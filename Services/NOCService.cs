using System.Text.Json;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class NOCService
    {
        private readonly NOCRepository _repository;
        private readonly PLFRepository _plfRepository;
        private readonly BranchServices _branchService;

        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly IConfiguration _configuration;
        private readonly TemplateService _templateService;
        private readonly HttpClient _httpClient;

        public NOCService(
            NOCRepository repository,
            PLFRepository plfRepository,
            EmailService emailService,
            SmsService smsService,
            IConfiguration configuration,
            TemplateService templateService,
            HttpClient httpClient
        )
        {
            _repository = repository;
            _plfRepository = plfRepository;
            _emailService = emailService;
            _smsService = smsService;
            _configuration = configuration;
            _templateService = templateService;
            _httpClient = httpClient;
        }

        public async Task<int> InsertClosedLansAsync(IEnumerable<NocClosedLanInsertDto> lans)
        {
            return await _repository.InsertClosedLansAsync(lans);
        }

        public async Task<IEnumerable<NocClosedLan>> GetEligibleLansAsync()
        {
            return await _repository.GetEligibleLansAsync();
        }

        public async Task<IEnumerable<NeolotexDisb>> GetLoansByCustIdAsync(string custId)
        {
            return await _plfRepository.GetLoansByCustIdAsync(custId);
        }

        public async Task<object> GetLinkedLoansByLanAsync(string lan)
        {
            // Step 1: Get all loans for the lan (borrower only)
            var loans = await _plfRepository.GetLinkedLoansByLanAsync(lan);

            // Step 2: Get all customers for the lan
            var customers = await _plfRepository.GetApplicantsByLanAsync(lan);

            var custList = customers
                .Where(x => !string.IsNullOrEmpty(x.CustomerId))
                .Select(x => new
                {
                    CustId = x.CustomerId,
                    CustName = x.CustomerName,
                    ApplicantType = x.ApplicantType,
                })
                .ToList();

            // Step 3: Attach customer list to each loan
            var result = loans.Select(loan => new
            {
                LoanAccountNo = loan.LoanAccountNo,
                LoanStatus = loan.LoanStatus,
                NocStatus = loan.NocStatus,
                DisbDate = loan.DisbursementDate,
                ClosureDate = loan.LoanClosureDate,
                Customers = custList,
            });

            return result;
        }

        public async Task<bool> InsertTransactionAsync(NocTransactionInsertDto dto)
        {
            await _repository.InsertTransactionAsync(dto);
            return true;
        }

        public async Task<NocTransaction?> GetByIdAsync(int id)
        {
            return await _repository.GetById(id);
        }

        public async Task<NocApplicantDetails?> GetApplicantByLanAsync(string lan)
        {
            return await _repository.GetApplicantByLanAsync(lan);
        }

        public async Task<(IEnumerable<UserLanDto>? Data, string? Error)> GetLansByUserAsync(
            GetLanByUserRequestDto dto
        )
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return (null, "UserId is required.");

            // Role should NOT block data fetch (only used in SP permission check)
            if (string.IsNullOrWhiteSpace(dto.Role))
                dto.Role = "NOCCOPS"; // optional default OR fetch from DB

            if (string.IsNullOrWhiteSpace(dto.Status))
                return (null, "Status is required.");

            return await _repository.GetLansByUserAsync(dto.UserId, dto.Role, dto.Status);
        }

        public async Task<(bool Success, string Message)> UpdateTransactionStatusAsync(
            UpdateNocStatusDto dto
        )
        {
            var result = await _repository.UpdateTransactionStatusAsync(dto);

            if (result != 1)
            {
                return result switch
                {
                    -1 => (false, "You are not authorized to update this status."),
                    -2 => (false, "Transaction not found."),
                    _ => (false, "Update failed."),
                };
            }

            // FETCH UPDATED TRANSACTION
            var transaction = await _repository.GetById(dto.Id);

            if (transaction != null)
            {
                await HandleStatusTriggersAsync(transaction, dto.Status);
            }

            return (true, "Status updated successfully.");
        }

        private async Task HandleStatusTriggersAsync(NocTransaction transaction, string status)
        {
            switch (status.Trim().ToUpper())
            {
                case "DISPATCHED":
                    await SendDispatchedNotifications(transaction);
                    break;

                case "RECEIVED WITH DISCREPANCY":
                    await SendDiscrepancyNotifications(transaction);
                    break;
            }
        }

        private async Task SendDispatchedNotifications(NocTransaction transaction)
        {
            try
            {
                //---------------------------------------------------
                // CUSTOMER SMS
                //---------------------------------------------------

                if (!string.IsNullOrWhiteSpace(transaction.CustMobile))
                {
                    var smsTemplate = _templateService.GetSmsTemplate("NocDispatchedSms");

                    var smsContent = _templateService.ReplacePlaceholders(
                        smsTemplate.Content,
                        BuildPlaceholders(transaction)
                    );

                    await _smsService.SendSms(
                        transaction.CustMobile,
                        smsContent,
                        smsTemplate.DltContentId
                    );
                }

                //---------------------------------------------------
                // BRANCH EMAILS
                //---------------------------------------------------

                var to = await GetNotificationRecipientsAsync(transaction.BranchCode);

                if (!to.Any())
                    return;

                var template = _templateService.GetEmailTemplate("NocDispatchedEmail");

                var body = _templateService.ReplacePlaceholders(
                    template.Body,
                    BuildPlaceholders(transaction)
                );

                var subject = _templateService.ReplacePlaceholders(
                    template.Subject,
                    BuildPlaceholders(transaction)
                );

                await _emailService.SendEmail(subject, body, to, new List<string>());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending dispatched notifications: {ex.Message}", ex);
            }
        }

        private async Task SendDiscrepancyNotifications(NocTransaction transaction)
        {
            try
            {
                var to = new List<string>();

                var nocEmail = _configuration["NocSettings:NocEmail"];

                var supervisorEmail = _configuration["NocSettings:SupervisorEmail"];

                if (!string.IsNullOrWhiteSpace(nocEmail))
                    to.Add(nocEmail);

                if (!string.IsNullOrWhiteSpace(supervisorEmail))
                    to.Add(supervisorEmail);

                to = to.Distinct().ToList();

                if (!to.Any())
                    return;

                var template = _templateService.GetEmailTemplate("NocDiscrepancyEmail");

                var body = _templateService.ReplacePlaceholders(
                    template.Body,
                    BuildPlaceholders(transaction)
                );

                var subject = _templateService.ReplacePlaceholders(
                    template.Subject,
                    BuildPlaceholders(transaction)
                );

                await _emailService.SendEmail(subject, body, to, new List<string>());
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending discrepancy notifications: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdatePropertyOwnerAsync(UpdatePropertyOwnerDto dto)
        {
            return await _repository.UpdatePropertyOwnerAsync(dto);
        }

        public async Task<List<NeolotexDisb>> GetApplicantsByLanAsync(string lan)
        {
            return await _plfRepository.GetApplicantsByLanAsync(lan);
        }

        public async Task<bool> UploadAcknowledgementAndKycAsync(
            UploadAcknowledgementKycDto dto,
            IWebHostEnvironment env
        )
        {
            string uploadRoot = Path.Combine(env.WebRootPath, "uploads", "noc");

            if (!Directory.Exists(uploadRoot))
                Directory.CreateDirectory(uploadRoot);

            string? acknowledgementPath = null;
            string? kycPath = null;

            if (dto.AcknowledgementFile != null)
            {
                var ackFileName =
                    $"ACK_{dto.Id}_{DateTime.Now.Ticks}{Path.GetExtension(dto.AcknowledgementFile.FileName)}";

                var ackFullPath = Path.Combine(uploadRoot, ackFileName);

                using (var stream = new FileStream(ackFullPath, FileMode.Create))
                {
                    await dto.AcknowledgementFile.CopyToAsync(stream);
                }

                acknowledgementPath = $"/uploads/noc/{ackFileName}";
            }

            if (dto.KycFile != null)
            {
                var kycFileName =
                    $"KYC_{dto.Id}_{DateTime.Now.Ticks}{Path.GetExtension(dto.KycFile.FileName)}";

                var kycFullPath = Path.Combine(uploadRoot, kycFileName);

                using (var stream = new FileStream(kycFullPath, FileMode.Create))
                {
                    await dto.KycFile.CopyToAsync(stream);
                }

                kycPath = $"/uploads/noc/{kycFileName}";
            }

            return await _repository.UploadAcknowledgementAndKycAsync(
                dto.Id,
                acknowledgementPath,
                kycPath,
                dto.UpdatedBy
            );
        }

        public async Task<(
            byte[]? FileBytes,
            string FileName,
            string Message
        )> GenerateAcknowledgementAsync(int id)
        {
            try
            {
                var transaction = await _repository.GetById(id);

                if (transaction == null)
                {
                    return (null, "", "Transaction not found.");
                }

                string lan = transaction.Lan;

                string folderPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "GeneratedFiles",
                    "Acknowledgements"
                );

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = $"{lan}_Acknowledgement.pdf";

                string filePath = Path.Combine(folderPath, fileName);

                if (!File.Exists(filePath))
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    {
                        Document document = new Document(PageSize.A4);

                        PdfWriter.GetInstance(document, fs);

                        document.Open();

                        document.Add(new Paragraph("ACKNOWLEDGEMENT"));
                        document.Add(new Paragraph(" "));
                        document.Add(new Paragraph($"LAN : {transaction.Lan}"));
                        document.Add(new Paragraph($"Customer : {transaction.CustName}"));
                        document.Add(new Paragraph($"Branch : {transaction.BranchName}"));
                        document.Add(new Paragraph($"Date : {DateTime.Now:dd-MMM-yyyy}"));

                        document.Close();
                    }
                }

                var bytes = await File.ReadAllBytesAsync(filePath);

                return (bytes, fileName, "Success");
            }
            catch (Exception ex)
            {
                return (null, "", ex.Message);
            }
        }

        public async Task<(
            bool Success,
            byte[]? FileBytes,
            string Message
        )> GenerateOneilReportAsync(List<int> ids, string updatedBy, string role)
        {
            //----------------------------------------------------
            // FETCH DATA
            //----------------------------------------------------
            var data = await _repository.GetOneilReportDataAsync(ids);

            if (data == null || !data.Any())
            {
                return (false, null, "No data found.");
            }

            //----------------------------------------------------
            // GENERATE EXCEL
            //----------------------------------------------------
            var fileBytes = ExcelHelper.GenerateExcel((data, "OneilReport"));

            //----------------------------------------------------
            // UPDATE STATUS AFTER SUCCESSFUL GENERATION
            //----------------------------------------------------
            foreach (var id in ids)
            {
                await _repository.UpdateTransactionStatusAsync(
                    new UpdateNocStatusDto
                    {
                        Id = id,
                        Status = "File Requested",
                        Remarks = "Oneil Report Downloaded",
                        CreatedBy = updatedBy,
                        Role = role,
                    }
                );
            }

            return (true, fileBytes, "Success");
        }

        private Dictionary<string, string> BuildPlaceholders(NocTransaction txn)
        {
            return new Dictionary<string, string>
            {
                { "LAN", txn.Lan ?? "" },
                { "CustName", txn.CustName ?? "" },
                { "CustomerName", txn.CustName ?? "" },
                { "BranchName", txn.BranchName ?? "" },
                { "Status", txn.Status ?? "" },
            };
        }

        private async Task<List<string>> GetNotificationRecipientsAsync(string branchCode)
        {
            var to = new List<string>();

            var branchEmails = await _repository.GetBranchEmailsAsync(branchCode);

            if (branchEmails != null)
            {
                if (!string.IsNullOrWhiteSpace(branchEmails.OPS_Email))
                    to.Add(branchEmails.OPS_Email);

                if (!string.IsNullOrWhiteSpace(branchEmails.BM_Email))
                    to.Add(branchEmails.BM_Email);

                if (!string.IsNullOrWhiteSpace(branchEmails.CM_Email))
                    to.Add(branchEmails.CM_Email);
            }

            return to.Distinct().ToList();
        }

        public async Task<IEnumerable<string>> GetAllowedStatusesAsync(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return Enumerable.Empty<string>();

            return await _repository.GetAllowedStatusesAsync(role);
        }

        public async Task<List<SignUser>> GetSignUsersAsync()
        {
            return await _repository.GetSignUsersAsync();
        }

        public async Task<object> CreateLeegalityRequestAsync(CreateLeegalityRequestDto dto)
        {
            var txn = await _repository.GetById(dto.TransactionId);

            if (txn == null)
                throw new Exception("Transaction not found");

            var profileId = _configuration["Leegality:ProfileId"];
            var token = _configuration["Leegality:AuthToken"];
            var baseUrl = _configuration["Leegality:BaseUrl"];

            var pdfBytes = await GenerateAcknowledgementAsync(dto.TransactionId);

            string base64File = Convert.ToBase64String(pdfBytes.FileBytes!);

            var payload = new
            {
                profileId = profileId,

                file = new { name = $"NOC_ACK_{txn.Lan}", file = base64File },

                invitees = new[]
                {
                    new
                    {
                        name = txn.PropertyOwnerName ?? txn.CustName,
                        phone = txn.PropertyOwnerMobile ?? txn.CustMobile,
                        email = "",
                        groupId = "1",
                        groupName = "Borrower",
                    },
                },

                irn = txn.Lan,
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v3.0/sign/request");

            request.Headers.Add("X-Auth-Token", token);

            request.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();

            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Status Code: {(int)response.StatusCode}");
            Console.WriteLine($"Response Body: {responseBody}");

            response.EnsureSuccessStatusCode();

            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(json);

            var documentId = doc
                .RootElement.GetProperty("data")
                .GetProperty("documentId")
                .GetString();

            var signUrl = doc
                .RootElement.GetProperty("data")
                .GetProperty("invitees")[0]
                .GetProperty("signUrl")
                .GetString();

            await _repository.SaveLeegalityDocumentAsync(dto.TransactionId, documentId!, signUrl!);

            return new { DocumentId = documentId, SignUrl = signUrl };
        }

        public async Task<string> RefreshLeegalityStatusAsync(RefreshLeegalityStatusDto dto)
        {
            var document = await _repository.GetLeegalityDocumentAsync(dto.TransactionId);

            if (document == null || string.IsNullOrWhiteSpace(document.DocumentId))
                throw new Exception("Leegality document not found");

            var token = _configuration["Leegality:AuthToken"];
            var baseUrl = _configuration["Leegality:BaseUrl"];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}/v3.2/sign/request?documentId={document.DocumentId}"
            );

            request.Headers.Add("X-Auth-Token", token);

            var response = await _httpClient.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(json);

            bool signed = doc
                .RootElement.GetProperty("data")
                .GetProperty("requests")[0]
                .GetProperty("signed")
                .GetBoolean();

            var status = signed ? "SIGNED" : "PENDING";

            await _repository.UpdateLeegalityStatusAsync(
                dto.TransactionId,
                status,
                signed ? DateTime.Now : null
            );

            return status;
        }

        public async Task<string> GetSignedDocumentUrlAsync(int transactionId)
        {
            var document = await _repository.GetLeegalityDocumentAsync(transactionId);

            if (document == null)
                throw new Exception("Document not found");

            var token = _configuration["Leegality:AuthToken"];
            var baseUrl = _configuration["Leegality:BaseUrl"];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{baseUrl}/v3.0/document/fetchDocument?documentId={document.DocumentId}&documentDownloadType=SIGNED"
            );

            request.Headers.Add("X-Auth-Token", token);

            var response = await _httpClient.SendAsync(request);

            var json = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("data").GetProperty("file").GetString()!;
        }

        public async Task<bool> UploadNocPdfAsync(UploadPdfDto dto)
        {
            using var ms = new MemoryStream();

            await dto.PdfFile.CopyToAsync(ms);

            string base64 = Convert.ToBase64String(ms.ToArray());

            return await _repository.UploadNocPdfAsync(dto.TransactionId, base64, dto.UpdatedBy);
        }

        public async Task<bool> UploadAckPdfAsync(UploadPdfDto dto)
        {
            using var ms = new MemoryStream();

            await dto.PdfFile.CopyToAsync(ms);

            string base64 = Convert.ToBase64String(ms.ToArray());

            return await _repository.UploadAckPdfAsync(dto.TransactionId, base64, dto.UpdatedBy);
        }

        public async Task<bool> SignNocPdfAsync(SignNocPdfDto dto)
        {
            var data = await _repository.GetNocPdfForSigningAsync(
                dto.TransactionId,
                dto.SignUserId
            );

            if (string.IsNullOrWhiteSpace(data.Pdf))
                throw new Exception("NOC PDF not found.");

            if (data.SignImage == null || data.SignImage.Length == 0)
                throw new Exception("Signature image not found.");

            byte[] pdfBytes = Convert.FromBase64String(data.Pdf);
            byte[] signBytes = data.SignImage;

            using var output = new MemoryStream();

            using (var reader = new PdfReader(pdfBytes))
            {
                using (var stamper = new PdfStamper(reader, output))
                {
                    var image = Image.GetInstance(signBytes);

                    image.ScaleToFit(120f, 60f);

                    int pageNumber = reader.NumberOfPages;

                    var pageSize = reader.GetPageSize(pageNumber);

                    float x = pageSize.Right - 140;
                    float y = 40;

                    image.SetAbsolutePosition(x, y);

                    stamper.GetOverContent(pageNumber).AddImage(image);
                }
            }

            string signedBase64 = Convert.ToBase64String(output.ToArray());

            return await _repository.SaveSignedNocAsync(
                dto.TransactionId,
                dto.SignUserId,
                signedBase64,
                dto.UpdatedBy
            );
        }

        public async Task<bool> SignAckPdfAsync(SignAckPdfDto dto)
        {
            var data = await _repository.GetAckPdfForSigningAsync(dto.TransactionId);

            byte[] pdfBytes = Convert.FromBase64String(data.Pdf);

            using var output = new MemoryStream();

            PdfReader reader = new PdfReader(pdfBytes);

            PdfStamper stamper = new PdfStamper(reader, output);

            var page = reader.NumberOfPages;

            var content = stamper.GetOverContent(page);

            ColumnText.ShowTextAligned(
                content,
                Element.ALIGN_LEFT,
                new Phrase("Property Owner:", FontFactory.GetFont(FontFactory.HELVETICA, 10)),
                420,
                55,
                0
            );

            ColumnText.ShowTextAligned(
                content,
                Element.ALIGN_LEFT,
                new Phrase(data.PropertyOwnerName, FontFactory.GetFont(FontFactory.HELVETICA, 10)),
                420,
                40,
                0
            );

            stamper.Close();

            string signedBase64 = Convert.ToBase64String(output.ToArray());

            return await _repository.SaveSignedAckAsync(
                dto.TransactionId,
                signedBase64,
                dto.UpdatedBy
            );
        }

        public async Task<byte[]> DownloadSignedNocPdfAsync(int transactionId)
        {
            var base64 = await _repository.GetSignedNocPdfAsync(transactionId);

            if (string.IsNullOrWhiteSpace(base64))
                throw new Exception("Signed NOC PDF not found.");

            return Convert.FromBase64String(base64);
        }

        public async Task<byte[]> DownloadSignedAckPdfAsync(int transactionId)
        {
            var base64 = await _repository.GetSignedAckPdfAsync(transactionId);

            if (string.IsNullOrWhiteSpace(base64))
                throw new Exception("Signed ACK PDF not found.");

            return Convert.FromBase64String(base64);
        }
    }
}