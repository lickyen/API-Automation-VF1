using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repository;
using System.Text;
using System.Text.Json;

namespace ProductivityApi.Services
{
    public class NOCService
    {
        private readonly NOCRepository _repository;
        private readonly PLFRepository _plfRepository;
        private readonly IndusArchivedRepository _indusArchivedRepository;
        private readonly BranchServices _branchService;
        private readonly PLFService _plfService;

        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly IConfiguration _configuration;
        private readonly TemplateService _templateService;
        private readonly HttpClient _httpClient;
        private readonly OtpService _otpService;
        private readonly FilePaths _filePaths;

        public NOCService(
            NOCRepository repository,
            PLFRepository plfRepository,
            IndusArchivedRepository indusArchivedRepository,
            EmailService emailService,
            SmsService smsService,
            IConfiguration configuration,
            TemplateService templateService,
            HttpClient httpClient,
            OtpService otpService,
            PLFService plfService,
            BranchServices branchService,
            AppSettings appSettings
        )
        {
            _repository = repository;
            _plfRepository = plfRepository;
            _indusArchivedRepository = indusArchivedRepository;
            _emailService = emailService;
            _smsService = smsService;
            _configuration = configuration;
            _templateService = templateService;
            _httpClient = httpClient;
            _plfService = plfService;
            _branchService = branchService;
            _otpService = otpService;
            _filePaths = appSettings.FilePaths;
        }

        public async Task<int> InsertClosedLansAsync(IEnumerable<NocClosedLanInsertDto> lans)
        {
            return await _repository.InsertClosedLansAsync(lans);
        }

        public async Task<IEnumerable<NocClosedLan>> GetClosedLansAsync(bool? isEligible)
        {
            return await _repository.GetClosedLansAsync(isEligible);
        }
        public async Task<object> GetLinkedLoansByLanAsync(string lan)
        {
            // Step 1: Get linked loans from PLF and INDUS
            var plfLoans = await _plfRepository.GetLinkedLoansByLanAsync(lan);
            var indusLoans = await _indusArchivedRepository.GetLinkedLoansByLanAsync(lan);

            if (!plfLoans.Any() && !indusLoans.Any())
                return Enumerable.Empty<object>();

            // Step 2: Get all linked LANs from PLF
            var plfLoanAccountNos = plfLoans
                .Select(x => x.LoanAccountNo)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToArray();

            // Step 3: Get all linked LANs from INDUS
            var indusLoanAccountNos = indusLoans
                .Select(x => x.LoanAccountNo)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToArray();

            // Step 4: Fetch PLF applicants
            var plfApplicants = plfLoanAccountNos.Any()
                ? await _plfRepository.GetApplicantsByLansAsync(plfLoanAccountNos)
                : [];

            // Step 5: Fetch INDUS applicants
            var indusApplicants = indusLoanAccountNos.Any()
                ? await _indusArchivedRepository.GetApplicantsByLansAsync(indusLoanAccountNos)
                : [];

            var plfApplicantsLookup = plfApplicants.ToLookup(x => x.LoanAccountNo);
            var indusApplicantsLookup = indusApplicants.ToLookup(x => x.LoanAccountNo);

            // Step 6: Map PLF loans
            var plfResult = plfLoans.Select(loan =>
            {
                var loanApplicants = plfApplicantsLookup[loan.LoanAccountNo].ToList();

                var borrower = loanApplicants
                    .FirstOrDefault(x => x.ApplicantType?.Trim() == "B");

                return new
                {
                    LoanAccountNo = loan.LoanAccountNo,
                    BranchCode = loan.BranchCode,
                    LoanStatus = loan.LoanStatus,
                    NocStatus = loan.NocStatus,
                    DisbDate = loan.DisbursementDate,
                    ClosureDate = loan.LoanClosureDate,

                    CustomerId = borrower?.CustomerId ?? loan.CustomerId,
                    CustomerName = borrower?.CustomerName ?? loan.CustomerName,

                    CollateralId = loan.CollateralId,
                    Source = "PLF",

                    Customers = loanApplicants
                        .Select(a => new
                        {
                            CustId = a.CustomerId,
                            CustName = a.CustomerName,
                            ApplicantType = a.ApplicantType
                        })
                        .ToList()
                };
            });
            // Step 7: Map INDUS loans
            var indusResult = indusLoans.Select(loan =>
            {
                var loanApplicants = indusApplicantsLookup[loan.LoanAccountNo].ToList();

                var borrower = loanApplicants
                    .FirstOrDefault(x => x.ApplicantType?.Trim() == "B");

                return new
                {
                    LoanAccountNo = loan.LoanAccountNo,
                    BranchCode = loan.BranchId,
                    LoanStatus = loan.LoanStatus,
                    NocStatus = loan.NocStatus,
                    DisbDate = loan.DisbursementDate,
                    ClosureDate = loan.ClosureDate,

                    CustomerId = borrower?.CustomerId ?? loan.CustomerId,
                    CustomerName = borrower?.CustomerName ?? loan.CustomerName,

                    CollateralId = loan.CollateralId,
                    Source = "INDUS",

                    Customers = loanApplicants
                        .Select(a => new
                        {
                            CustId = a.CustomerId,
                            CustName = a.CustomerName,
                            ApplicantType = a.ApplicantType
                        })
                        .ToList()
                };
            });
            // Step 8: Merge both sources
            return plfResult
                .Cast<object>()
                .Concat(indusResult.Cast<object>())
                .ToList();
        }
        //public async Task<bool> InsertTransactionAsync(NocTransactionInsertDto dto)
        //{
        //    await _repository.InsertTransactionAsync(dto);
        //    return true;
        //}

        public async Task<(bool Success, string Message)> InsertTransactionAsync(NocTransactionInsertDto dto)
        {
            var customer = await _plfService.GetCustomerDetails(dto.CustId);

            if (customer?.ReturnStatus == null)
                return (false, "Failed to retrieve customer details");

            if (customer.ReturnStatus.returnCode != "0000")
                return (false, customer.ReturnStatus.returnText);

            var mobile = customer.Phones?
                .OrderByDescending(p => p.priority)
                .Select(p => p.phoneNumber)
                .FirstOrDefault();

            var branch = _branchService.GetBranchByID(dto.BranchCode);

            if (branch == null)
                return (false, "Branch not found");

            var transaction = new NocTransactionInsertModel
            {
                Lan = dto.Lan,
                CustId = dto.CustId,
                CustName = dto.CustName,
                CollId = dto.CollId,
                CustMobile = mobile,

                BranchCode = dto.BranchCode,
                BranchName = branch.BranchName,
                Region = branch.Region,
                Zone = branch.Zone,
                MegaZone = branch.MegaZone,

                DisbDate = dto.DisbDate,
                LoanStatus = dto.LoanStatus,
                ClosureDate = dto.ClosureDate,
                NocStatus = dto.NocStatus,
                CreatedBy = dto.CreatedBy,
                GenerateNOC = dto.GenerateNOC
            };

            await _repository.InsertTransactionAsync(transaction);

            return (true, "Transaction inserted successfully.");
        }
        public async Task<(IEnumerable<NocDashboardDto>? Data, string? Error)>
        GetDashboardByUserAsync(NocDashboardRequestDto dto)
        {
            return await _repository.GetDashboardByUserAsync(dto);
        }

        public async Task<NocTransaction?> GetByIdAsync(int id)
        {
            return await _repository.GetById(id);
        }

        public async Task<(IEnumerable<UserLanDto>? Data, string? Error)> GetLansByUserAsync(GetLanByUserRequestDto dto)
        {
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
            if (status.Trim().Equals("DISPATCHED", StringComparison.OrdinalIgnoreCase))
            {
                await SendDispatchedNotifications(transaction);
            }

            if (
                status.Trim().Equals("DOCUMENT COLLECTED", StringComparison.OrdinalIgnoreCase)
                || status.Trim().Equals("Document Collected", StringComparison.OrdinalIgnoreCase)
            )
            {
                // Send OTP confirmation SMS to customer after collection
                try
                {
                    if (!string.IsNullOrWhiteSpace(transaction.CustMobile))
                    {
                        var smsTemplate = _templateService.GetSmsTemplate("NocOtpConfirmationSms");

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
                }
                catch (Exception ex)
                {
                    // swallow but log
                    Console.WriteLine(
                        $"Error sending collection OTP confirmation SMS: {ex.Message}"
                    );
                }
            }
        }

        private async Task SendDispatchedNotifications(NocTransaction transaction)
        {
            try
            {
                //---------------------------------------------------
                // CUSTOMER SMS - detailed dispatched message
                //---------------------------------------------------

                if (!string.IsNullOrWhiteSpace(transaction.CustMobile))
                {
                    var smsTemplate = _templateService.GetSmsTemplate("NocDispatchedCustomerSms");

                    var smsContent = _templateService.ReplacePlaceholders(
                        smsTemplate.Content,
                        BuildPlaceholders(transaction)
                    );

                    await _smsService.SendSms(
                        transaction.CustMobile,
                        smsContent,
                        smsTemplate.DltContentId
                    );

                    // Also send OTP required sms to customer (informational)
                    var otpTemplate = _templateService.GetSmsTemplate("NocOtpRequiredSms");

                    var otpContent = _templateService.ReplacePlaceholders(
                        otpTemplate.Content,
                        BuildPlaceholders(transaction)
                    );

                    await _smsService.SendSms(
                        transaction.CustMobile,
                        otpContent,
                        otpTemplate.DltContentId
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

        private async Task SendDiscrepancyNotifications(List<NocTransaction> transactions)
        {
            try
            {
                if (transactions == null || !transactions.Any())
                    return;

                var nocEmail = _configuration["NocSettings:NocEmail"];
                var supervisorEmail = _configuration["NocSettings:SupervisorEmail"];

                var to = new List<string>();
                var cc = new List<string>();

                if (!string.IsNullOrWhiteSpace(nocEmail))
                    to.Add(nocEmail);

                if (!string.IsNullOrWhiteSpace(supervisorEmail))
                    cc.Add(supervisorEmail);

                to = to.Distinct().ToList();
                cc = cc.Distinct().ToList();

                if (!to.Any() && !cc.Any())
                    return;

                var template = _templateService.GetEmailTemplate("NocDiscrepancyEmail");

                var placeholders = BuildPlaceholders(transactions);

                var body = _templateService.ReplacePlaceholders(
                    template.Body,
                    placeholders
                );

                await _emailService.SendEmail(
                    template.Subject,
                    body,
                    to,
                    cc
                );
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error sending discrepancy notifications: {ex.Message}",
                    ex
                );
            }
        }
        public async Task<bool> UpdatePropertyOwnerAsync(UpdatePropertyOwnerDto dto)
        {
            return await _repository.UpdatePropertyOwnerAsync(dto);
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
                { "POD", txn.POD ?? "" },
                { "CourierPartner", txn.CourierPartner ?? "" },
                { "CourierVendor", txn.CourierVendor ?? "" },
                { "ClosureDate", txn.ClosureDate?.ToString("dd-MMM-yyyy") ?? "" },
                { "TodayDate", DateTime.Now.ToString("dd-MMM-yyyy") },
            };
        }

        private string BuildDiscrepancyTableHtml(List<NocTransaction> transactions)
        {
            var table = new StringBuilder();

            table.Append(
                "<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width:100%;'>"
            );

            table.Append("<tr style='background-color:#f2f2f2;'>");
            table.Append("<th>LAN</th>");
            table.Append("<th>Customer Name</th>");
            table.Append("<th>Branch</th>");
            table.Append("<th>Status</th>");
            table.Append("</tr>");

            foreach (var txn in transactions)
            {
                table.Append("<tr>");
                table.Append($"<td style='text-align:center;'>{txn.Lan}</td>");
                table.Append($"<td>{txn.CustName}</td>");
                table.Append($"<td>{txn.BranchName}</td>");
                table.Append($"<td style='text-align:center;'>{txn.Status}</td>");
                table.Append("</tr>");
            }

            table.Append("</table>");

            return table.ToString();
        }

        private Dictionary<string, string> BuildPlaceholders(List<NocTransaction> transactions)
        {
            return new Dictionary<string, string>
            {
                { "Table", BuildDiscrepancyTableHtml(transactions) },
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

        //public async Task<bool> UploadNOCandAckAsync(FetchNocAckDto dto)
        //{
        //    var request = new AgreementRequest
        //    {
        //        finReference = dto.LanNumber,
        //        agreementType = "NOC"
        //    };

        //    var response = _plfService.GetAgreements(request);

        //    if (response == null || response.Result?.returnStatus?.returnCode != "0000")
        //        throw new Exception(response?.Result?.returnStatus?.returnText ?? "Unable to fetch agreement document.");

        //    var agreement = response.Result?.agreementsList?.FirstOrDefault();

        //    if (agreement == null || string.IsNullOrWhiteSpace(agreement.docContent))
        //        throw new Exception("Document content not found.");

        //    byte[] uploadedPdf = Convert.FromBase64String(
        //        agreement.docContent.Replace("\r", "").Replace("\n", "")
        //    );

        //    using var reader = new PdfReader(uploadedPdf);

        //    if (reader.NumberOfPages < 2)
        //        throw new Exception("Agreement PDF must contain at least 2 pages.");

        //    byte[] nocBytes = ExtractPage(reader, 1);
        //    byte[] ackBytes = ExtractPage(reader, 2);

        //    string nocFileName = $"{dto.TransactionId}_noc.pdf";
        //    string ackFileName = $"{dto.TransactionId}_ack.pdf";

        //    var nocSaved = await FileHelper.SaveBytesAsync(_filePaths.NOCFilePath, nocFileName, nocBytes);
        //    if (!nocSaved.Success)
        //        throw new Exception($"Failed to save NOC PDF: {nocSaved.Error}");

        //    var ackSaved = await FileHelper.SaveBytesAsync(_filePaths.NOCFilePath, ackFileName, ackBytes);
        //    if (!ackSaved.Success)
        //        throw new Exception($"Failed to save ACK PDF: {ackSaved.Error}");

        //    return await _repository.UploadNOCAndAckPdfAsync(
        //        dto.TransactionId,
        //        nocFileName,
        //        ackFileName,
        //        dto.UpdatedBy
        //    );
        //}

        public async Task<(bool Success, string Message)> UploadNOCandAckAsync(FetchNocAckDto dto)
        {
            try
            {
                var request = new AgreementRequest
                {
                    finReference = dto.LanNumber,
                    agreementType = "NOC"
                };

                var response = await _plfService.GetAgreements(request);

                if (response?.returnStatus?.returnCode != "0000")
                    return (
                        false,
                        response?.returnStatus?.returnText ?? "Unable to fetch NOC."
                    );

                var agreement = response.agreementsList?.FirstOrDefault();

                if (agreement == null || string.IsNullOrWhiteSpace(agreement.docContent))
                    return (false, "Document content not found.");

                byte[] uploadedPdf = Convert.FromBase64String(
                    agreement.docContent.Replace("\r", "").Replace("\n", "")
                );

                using var reader = new PdfReader(uploadedPdf);

                if (reader.NumberOfPages < 2)
                    return (false, "PDF must contain at least 2 pages.");

                byte[] nocBytes = ExtractPage(reader, 1);
                byte[] ackBytes = ExtractPage(reader, 2);

                string nocFileName = $"{dto.TransactionId}_noc.pdf";
                string ackFileName = $"{dto.TransactionId}_ack.pdf";

                var nocSaved = await FileHelper.SaveBytesAsync(
                    _filePaths.NOCFilePath,
                    nocFileName,
                    nocBytes
                );

                if (!nocSaved.Success)
                    return (false, $"Failed to save NOC PDF: {nocSaved.Error}");

                var ackSaved = await FileHelper.SaveBytesAsync(
                    _filePaths.NOCFilePath,
                    ackFileName,
                    ackBytes
                );

                if (!ackSaved.Success)
                    return (false, $"Failed to save ACK PDF: {ackSaved.Error}");

                var result = await _repository.UploadNOCAndAckPdfAsync(
                    dto.TransactionId,
                    nocFileName,
                    ackFileName,
                    dto.UpdatedBy
                );

                return result
                    ? (true, "NOC uploaded successfully.")
                    : (false, "Failed to save NOC details.");
            }
            catch (FormatException)
            {
                return (false, "Invalid document content.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        private byte[] ExtractPage(PdfReader reader, int pageNumber)
        {
            using var ms = new MemoryStream();

            using (var document = new Document())
            {
                using (var copy = new PdfCopy(document, ms))
                {
                    document.Open();

                    copy.AddPage(copy.GetImportedPage(reader, pageNumber));

                    document.Close();
                }
            }

            return ms.ToArray();
        }

        public async Task<bool> SignNocPdfAsync(SignNocPdfDto dto)
        {
            var data = await _repository.GetNocPdfForSigningAsync(
                dto.TransactionId,
                dto.SignUserId
            );

            if (string.IsNullOrWhiteSpace(data.Pdf))
                throw new Exception("NOC PDF path not found.");

            if (string.IsNullOrWhiteSpace(data.SignImage))
                throw new Exception("Signature image path not found.");

            byte[]? pdfBytes = await FileHelper.ReadFileAsync(_filePaths.NOCFilePath, data.Pdf);
            if (pdfBytes == null)
                throw new Exception($"NOC PDF file not found: {data.Pdf}");

            string signImagePath = Path.Combine(AppContext.BaseDirectory, "templates", "noc", data.SignImage);

            if (!File.Exists(signImagePath))
                throw new Exception($"Signature image not found: {signImagePath}");

            byte[] signBytes = await File.ReadAllBytesAsync(signImagePath);

            using var output = new MemoryStream();
            using (var reader = new PdfReader(pdfBytes))
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

            string fileName = $"{dto.TransactionId}_noc_signed.pdf";
            var saved = await FileHelper.SaveBytesAsync(_filePaths.NOCFilePath, fileName, output.ToArray());
            if (!saved.Success)
                throw new Exception($"Failed to save signed NOC PDF: {saved.Error}");

            return await _repository.SaveSignedNocAsync(
                dto.TransactionId,
                dto.SignUserId,
                fileName,
                dto.UpdatedBy
            );
        }
        public async Task<bool> SignAckPdfAsync(SignAckPdfDto dto)
        {
            var data = await _repository.GetAckPdfForSigningAsync(dto.TransactionId);

            if (string.IsNullOrWhiteSpace(data.Pdf))
                throw new Exception("ACK PDF path not found.");

            byte[]? pdfBytes = await FileHelper.ReadFileAsync(_filePaths.NOCFilePath, data.Pdf);
            if (pdfBytes == null)
                throw new Exception($"ACK PDF file not found: {data.Pdf}");

            using var output = new MemoryStream();

            var reader = new PdfReader(pdfBytes);
            var stamper = new PdfStamper(reader, output);
            var content = stamper.GetOverContent(reader.NumberOfPages);

            ColumnText.ShowTextAligned(
                content,
                Element.ALIGN_LEFT,
                new Phrase("Property Owner:", FontFactory.GetFont(FontFactory.HELVETICA, 10)),
                420, 255, 0
            );

            ColumnText.ShowTextAligned(
                content,
                Element.ALIGN_LEFT,
                new Phrase(data.PropertyOwnerName, FontFactory.GetFont(FontFactory.HELVETICA, 10)),
                420, 240, 0
            );

            stamper.Close();

            string fileName = $"{dto.TransactionId}_ack_signed.pdf";
            var saved = await FileHelper.SaveBytesAsync(_filePaths.NOCFilePath, fileName, output.ToArray());
            if (!saved.Success)
                throw new Exception($"Failed to save signed ACK PDF: {saved.Error}");

            return await _repository.SaveSignedAckAsync(
                dto.TransactionId,
                fileName,
                dto.UpdatedBy
            );
        }
        public async Task<byte[]> DownloadSignedNocPdfAsync(int transactionId)
        {
            var fileName = await _repository.GetSignedNocPdfAsync(transactionId);

            if (string.IsNullOrWhiteSpace(fileName))
                throw new Exception("Signed NOC PDF not found.");

            var bytes = await FileHelper.ReadFileAsync(_filePaths.NOCFilePath, fileName);
            if (bytes == null)
                throw new Exception($"Signed NOC PDF file not found: {fileName}");

            return bytes;
        }

        public async Task<byte[]> DownloadSignedAckPdfAsync(int transactionId)
        {
            var fileName = await _repository.GetSignedAckPdfAsync(transactionId);

            if (string.IsNullOrWhiteSpace(fileName))
                throw new Exception("Signed ACK PDF not found.");

            var bytes = await FileHelper.ReadFileAsync(_filePaths.NOCFilePath, fileName);
            if (bytes == null)
                throw new Exception($"Signed ACK PDF file not found: {fileName}");

            return bytes;
        }

        public async Task<(bool Success, string Message)> UpdateOwnerMobileNumberAsync(
            UpdateOwnerMobileNumberDto dto
        )
        {
            //----------------------------------------------------
            // UPDATE MOBILE NUMBER
            //----------------------------------------------------
            var updated = await _repository.UpdateOwnerMobileNumberAsync(dto);

            if (!updated)
            {
                return (false, "Mobile number update failed.");
            }

            //----------------------------------------------------
            // UPDATE STATUS THROUGH WORKFLOW
            //----------------------------------------------------
            var statusResult = await _repository.UpdateTransactionStatusAsync(
                new UpdateNocStatusDto
                {
                    Id = dto.Id,
                    Status = "Owner Mobile Update Pending",
                    //Status = "Mobile Update Pending",
                    Remarks = "Property owner mobile number updated",
                    CreatedBy = dto.UpdatedBy,
                    Role = dto.Role,
                }
            );

            if (statusResult != 1)
            {
                return (false, "Mobile updated but status update failed.");
            }

            return (true, "Mobile number updated successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateCustMobileNumberAsync(
            UpdateCustMobileNumberDto dto
        )
        {
            //----------------------------------------------------
            // UPDATE MOBILE NUMBER
            //----------------------------------------------------
            var updated = await _repository.UpdateCustMobileNumberAsync(dto);

            if (!updated)
            {
                return (false, "Mobile number update failed.");
            }

            //----------------------------------------------------
            // UPDATE STATUS THROUGH WORKFLOW
            //----------------------------------------------------
            var statusResult = await _repository.UpdateTransactionStatusAsync(
                new UpdateNocStatusDto
                {
                    Id = dto.Id,
                    Status = "Customer Mobile Update Pending",
                    //Status = "Mobile Update Pending",
                    Remarks = "Customer mobile number updated",
                    CreatedBy = dto.UpdatedBy,
                    Role = dto.Role,
                }
            );

            if (statusResult != 1)
            {
                return (false, "Mobile updated but status update failed.");
            }

            return (true, "Mobile number updated successfully.");
        }

        public async Task<(bool Success, string Message)> UploadKycPdfAsync(UploadKycPdfDto dto)
        {
            if (dto.PdfFile == null || dto.PdfFile.Length == 0)
                return (false, "PDF file is required.");

            using var ms = new MemoryStream();
            await dto.PdfFile.CopyToAsync(ms);
            byte[] pdfBytes = ms.ToArray();

            string fileName = $"{dto.TransactionId}_kyc.pdf";
            var saved = await FileHelper.SaveBytesAsync(_filePaths.NOCFilePath, fileName, pdfBytes);
            if (!saved.Success)
                return (false, $"Failed to save KYC PDF: {saved.Error}");

            var dbSaved = await _repository.UpdateKycPdfAsync(
                dto.TransactionId,
                fileName,
                dto.UpdatedBy
            );

            if (!dbSaved)
                return (false, "Failed to save KYC PDF record.");

            var statusResult = await _repository.UpdateTransactionStatusAsync(
                new UpdateNocStatusDto
                {
                    Id = dto.TransactionId,
                    Status = "KYC Verification Pending",
                    Remarks = "KYC document uploaded",
                    CreatedBy = dto.UpdatedBy,
                    Role = dto.Role,
                }
            );

            if (statusResult != 1)
                return (false, "KYC uploaded but status update failed.");

            try
            {
                var txn = await _repository.GetById(dto.TransactionId);

                if (txn != null)
                {
                    var template = _templateService.GetEmailTemplate("NocKycVerificationEmail");

                    var placeholders = new Dictionary<string, string>
            {
                { "BranchName", txn.BranchName ?? "" },
                { "LAN",        txn.Lan        ?? "" },
                { "UploadedBy", dto.UpdatedBy  ?? "" },
                { "UploadDate", DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") },
            };

                    var body = _templateService.ReplacePlaceholders(template.Body, placeholders);
                    var to = new List<string>();

                    var nocEmail = _configuration["NocSettings:NocEmail"];
                    if (!string.IsNullOrWhiteSpace(nocEmail))
                        to.Add(nocEmail);

                    //var branchRecipients = await GetNotificationRecipientsAsync(txn.BranchCode);
                    //to.AddRange(branchRecipients);

                    //to = to.Distinct().ToList();

                    if (to.Any())
                        await _emailService.SendEmail(template.Subject, body, to, new List<string>());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending KYC verification email: {ex.Message}");
            }

            return (true, "KYC uploaded successfully.");
        }

        public async Task<(bool Success, string Message)> UploadAckPdfAsync(UploadAckPdfDto dto)
        {
            if (dto.PdfFile == null || dto.PdfFile.Length == 0)
                return (false, "PDF file is required.");

            using var ms = new MemoryStream();
            await dto.PdfFile.CopyToAsync(ms);
            byte[] pdfBytes = ms.ToArray();

            string fileName = $"{dto.TransactionId}_ack_signed.pdf";
            var saved = await FileHelper.SaveBytesAsync(_filePaths.NOCFilePath, fileName, pdfBytes);
            if (!saved.Success)
                return (false, $"Failed to save ACK PDF: {saved.Error}");

            var dbSaved = await _repository.UpdateAckPdfAsync(
                dto.TransactionId,
                fileName,
                dto.UpdatedBy
            );

            if (!dbSaved)
                return (false, "Failed to save Acknowledgement PDF.");

            return (true, "Acknowledgement uploaded successfully.");
        }

        public async Task<byte[]?> DownloadKycPdfAsync(int transactionId)
        {
            var fileName = await _repository.GetKycPdfAsync(transactionId);

            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return await FileHelper.ReadFileAsync(_filePaths.NOCFilePath, fileName);
        }
        public async Task<(bool Success, string Message)> UpdateTransactionStatusesAsync(
            BulkUpdateNocStatusDto dto
        )
        {
            var updatedTransactions = new List<NocTransaction>();
            var failedIds = new List<int>();

            foreach (var id in dto.Ids)
            {
                try
                {
                    var result = await _repository.UpdateTransactionStatusAsync(
                        new UpdateNocStatusDto
                        {
                            Id = id,
                            Status = dto.Status,
                            Remarks = dto.Remarks,
                            CreatedBy = dto.CreatedBy,
                            Role = dto.Role,
                        }
                    );

                    if (result == 1)
                    {
                        var txn = await _repository.GetById(id);

                        if (txn != null)
                            updatedTransactions.Add(txn);
                    }
                    else
                    {
                        failedIds.Add(id);
                    }
                }
                catch (Exception ex)
                {
                    failedIds.Add(id);

                    // IMPORTANT: log this
                    Console.WriteLine($"Failed for ID {id}: {ex.Message}");
                }
            }

            if (
                dto.Status.Equals("Received with Discrepancy", StringComparison.OrdinalIgnoreCase)
                && updatedTransactions.Any()
            )
            {
                await SendDiscrepancyNotifications(updatedTransactions);
            }

            if (failedIds.Any())
            {
                return (false, $"Partial failure. Failed IDs: {string.Join(",", failedIds)}");
            }

            return (true, "Status updated successfully.");
        }

        public async Task SendCustomerOtpAsync(int transactionId)
        {
            var transaction = await _repository.GetById(transactionId);

            if (transaction == null)
                throw new Exception("Transaction not found");

            string? mobile = !string.IsNullOrWhiteSpace(transaction.UpdatedCustMobile)
                ? transaction.UpdatedCustMobile
                : transaction.CustMobile;

            if (string.IsNullOrWhiteSpace(mobile))
                throw new Exception("Customer mobile number not available");

            string referenceId = $"NOC_{transactionId}_CUSTOMER";

            await _otpService.SendOtpToMobile(referenceId, mobile);

            await _repository.UpdateOtpStatus(transactionId, "NOC", "Sent");
        }

        public async Task SendOwnerOtpAsync(int transactionId)
        {
            var transaction = await _repository.GetById(transactionId);

            if (transaction == null)
                throw new Exception("Transaction not found");

            string? mobile = !string.IsNullOrWhiteSpace(transaction.UpdatedOwnerMobile)
                ? transaction.UpdatedOwnerMobile
                : transaction.PropertyOwnerMobile;

            if (string.IsNullOrWhiteSpace(mobile))
                throw new Exception("Property owner mobile number not available");

            string referenceId = $"NOC_{transactionId}_OWNER";

            await _otpService.SendOtpToMobile(referenceId, mobile);

            await _repository.UpdateOtpStatus(transactionId, "KYC", "Sent");
        }

        public async Task<bool> VerifyCustomerOtpAsync(int transactionId, string otp)
        {
            // #if DEBUG
            //             bool result = otp == "123456";
            // #else
            string referenceId = $"NOC_{transactionId}_CUSTOMER";
            bool result = await _otpService.VerifyOtp(referenceId, otp);
            // #endif

            if (result)
            {
                await _repository.UpdateOtpStatus(transactionId, "NOC", "Verified");
            }

            return result;
        }

        public async Task<bool> VerifyOwnerOtpAsync(int transactionId, string otp)
        {
            // #if DEBUG
            //             bool result = otp == "123456";
            // #else
            string referenceId = $"NOC_{transactionId}_OWNER";
            bool result = await _otpService.VerifyOtp(referenceId, otp);
            // #endif

            if (result)
            {
                await _repository.UpdateOtpStatus(transactionId, "KYC", "Verified");
            }

            return result;
        }

        public async Task<(bool Success, string Message)> SentForDispatchAsync(
            SentForDispatchDto dto
        )
        {
            //--------------------------------------------------
            // UPDATE POD
            //--------------------------------------------------
            var podUpdated = await _repository.UpdatePodAsync(
                dto.TransactionId,
                dto.POD,
                dto.CourierVendor,
                dto.UpdatedBy
            );

            if (!podUpdated)
            {
                return (false, "Failed to update POD.");
            }

            //--------------------------------------------------
            // UPDATE STATUS TO DISPATCHED
            //--------------------------------------------------
            var statusResult = await UpdateTransactionStatusAsync(
                new UpdateNocStatusDto
                {
                    Id = dto.TransactionId,
                    Status = "Dispatched",
                    Remarks = $"POD Updated : {dto.POD}",
                    CreatedBy = dto.UpdatedBy,
                    Role = dto.Role,
                }
            );

            return statusResult;
        }

        public async Task<(bool Success, string Message)> ReturnToHoAsync(ReturnToHoDto dto)
        {
            //--------------------------------------------------
            // UPDATE POD
            //--------------------------------------------------
            var podUpdated = await _repository.UpdatePodAsync(
                dto.TransactionId,
                dto.POD,
                dto.CourierVendor,
                dto.UpdatedBy
            );

            if (!podUpdated)
            {
                return (false, "Failed to update POD.");
            }

            //--------------------------------------------------
            // UPDATE STATUS TO RETURNED TO HO
            //--------------------------------------------------
            var statusResult = await UpdateTransactionStatusAsync(
                new UpdateNocStatusDto
                {
                    Id = dto.TransactionId,
                    Status = "Returned to HO",
                    Remarks = $"Returned to HO. POD : {dto.POD}",
                    CreatedBy = dto.UpdatedBy,
                    Role = dto.Role,
                }
            );

            return statusResult;
        }

        internal async Task<byte[]> GetCibilReportExcelAsync(DateTime? fromDate, DateTime? toDate, string? type)
        {
            throw new NotImplementedException();
        }

        public async Task<(bool Success, byte[]? Data, string Message)> GetTransactionReportAsync(
            NocReportRequest request)
        {
            if (request.FromDate.HasValue &&
                request.ToDate.HasValue &&
                request.FromDate > request.ToDate)
            {
                return (false, null, "From date cannot be greater than To date.");
            }

            var data = await _repository.GetTransactionReportAsync(
                request.FromDate,
                request.ToDate);

            var excel = ExcelHelper.GenerateExcel(data, "NOC Report");

            if (excel == null)
                return (false, null, "No records found.");

            using var stream = new MemoryStream(excel);
            using var workbook = new XLWorkbook(stream);

            var ws = workbook.Worksheet("NOC Report");

            var tatColumn = ws.Row(1)
                .CellsUsed()
                .FirstOrDefault(x => x.Value.ToString()
                    .Equals("TAT", StringComparison.OrdinalIgnoreCase));

            if (tatColumn != null)
            {
                for (int row = 2; row <= ws.LastRowUsed().RowNumber(); row++)
                {
                    var cell = ws.Cell(row, tatColumn.Address.ColumnNumber);

                    if (!cell.IsEmpty() && int.TryParse(cell.Value.ToString(), out int tat))
                    {
                        if (tat <= 10)
                            cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        else if (tat <= 16)
                            cell.Style.Fill.BackgroundColor = XLColor.LightGoldenrodYellow;
                        else
                            cell.Style.Fill.BackgroundColor = XLColor.LightPink;
                    }
                }
            }

            using var output = new MemoryStream();
            workbook.SaveAs(output);

            return (true, output.ToArray(), "Report generated successfully.");
        }
    }
}
