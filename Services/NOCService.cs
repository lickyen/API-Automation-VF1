using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repository;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ProductivityApi.Services
{
    public class NOCService
    {
        private readonly NOCRepository _repository;
        private readonly PLFRepository _plfRepository;

        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly IConfiguration _configuration;
        private readonly TemplateService _templateService;

        public NOCService(
            NOCRepository repository,
            PLFRepository plfRepository,
            EmailService emailService,
            SmsService smsService,
            IConfiguration configuration,
            TemplateService templateService)
        {
            _repository = repository;
            _plfRepository = plfRepository;
            _emailService = emailService;
            _smsService = smsService;
            _configuration = configuration;
            _templateService = templateService;
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
                    ApplicantType = x.ApplicantType
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
                Customers = custList
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

        public async Task<(IEnumerable<UserLanDto>? Data, string? Error)> GetLansByUserAsync(GetLanByUserRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return (null, "UserId is required.");

            // Role should NOT block data fetch (only used in SP permission check)
            if (string.IsNullOrWhiteSpace(dto.Role))
                dto.Role = "NOCCOPS"; // optional default OR fetch from DB

            if (string.IsNullOrWhiteSpace(dto.Status))
                return (null, "Status is required.");

            return await _repository.GetLansByUserAsync(
                dto.UserId,
                dto.Role,
                dto.Status
            );
        }
        public async Task<(bool Success, string Message)> UpdateTransactionStatusAsync(UpdateNocStatusDto dto)
        {
            var result = await _repository.UpdateTransactionStatusAsync(dto);

            if (result != 1)
            {
                return result switch
                {
                    -1 => (false, "You are not authorized to update this status."),
                    -2 => (false, "Transaction not found."),
                    _ => (false, "Update failed.")
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

        private async Task HandleStatusTriggersAsync(
            NocTransaction transaction,
            string status)
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

        private async Task SendDispatchedNotifications(
            NocTransaction transaction)
        {
            try
            {
                //---------------------------------------------------
                // CUSTOMER SMS
                //---------------------------------------------------

                if (!string.IsNullOrWhiteSpace(transaction.CustMobile))
                {
                    var smsTemplate =
                        _templateService.GetSmsTemplate(
                            "NocDispatchedSms");

                    var smsContent =
                        _templateService.ReplacePlaceholders(
                            smsTemplate.Content,
                            BuildPlaceholders(transaction));

                    await _smsService.SendSms(
                        transaction.CustMobile,
                        smsContent,
                        smsTemplate.DltContentId);
                }

                //---------------------------------------------------
                // BRANCH EMAILS
                //---------------------------------------------------

                var to =
                    await GetNotificationRecipientsAsync(
                        transaction.BranchCode);

                if (!to.Any())
                    return;

                var template =
                    _templateService.GetEmailTemplate(
                        "NocDispatchedEmail");

                var body =
                    _templateService.ReplacePlaceholders(
                        template.Body,
                        BuildPlaceholders(transaction));

                var subject =
                    _templateService.ReplacePlaceholders(
                        template.Subject,
                        BuildPlaceholders(transaction));

                await _emailService.SendEmail(
                    subject,
                    body,
                    to,
                    new List<string>());
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error sending dispatched notifications: {ex.Message}",
                    ex);
            }
        }

        private async Task SendDiscrepancyNotifications(
            NocTransaction transaction)
        {
            try
            {
                var to = new List<string>();

                var nocEmail =
                    _configuration["NocSettings:NocEmail"];

                var supervisorEmail =
                    _configuration["NocSettings:SupervisorEmail"];

                if (!string.IsNullOrWhiteSpace(nocEmail))
                    to.Add(nocEmail);

                if (!string.IsNullOrWhiteSpace(supervisorEmail))
                    to.Add(supervisorEmail);

                to = to
                    .Distinct()
                    .ToList();

                if (!to.Any())
                    return;

                var template =
                    _templateService.GetEmailTemplate(
                        "NocDiscrepancyEmail");

                var body =
                    _templateService.ReplacePlaceholders(
                        template.Body,
                        BuildPlaceholders(transaction));

                var subject =
                    _templateService.ReplacePlaceholders(
                        template.Subject,
                        BuildPlaceholders(transaction));

                await _emailService.SendEmail(
                    subject,
                    body,
                    to,
                    new List<string>());
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error sending discrepancy notifications: {ex.Message}",
                    ex);
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
            IWebHostEnvironment env)
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

        public async Task<(byte[]? FileBytes, string FileName, string Message)> GenerateNocAsync(int id, string updatedBy, string role)
        {
            try
            {
                var transaction = await _repository.GetById(id);

                if (transaction == null)
                    return (null, "", "Transaction not found.");

                //----------------------------------------------------
                // 1. RBAC CHECK BEFORE ANY ACTION
                //----------------------------------------------------
                var hasEditAccess = await _repository.ValidateRoleStatusPermissionAsync(
                    role,
                    "Document Collected",
                    "EDIT"
                );

                if (!hasEditAccess)
                    return (null, "", "You are not authorized to generate NOC.");

                //----------------------------------------------------
                // 2. File generation
                //----------------------------------------------------
                string folderPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "GeneratedFiles",
                    "NOC");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = $"{transaction.Lan}_{id}_NOC.pdf";
                string filePath = Path.Combine(folderPath, fileName);

                if (!File.Exists(filePath))
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    {
                        Document document = new Document(PageSize.A4);
                        PdfWriter.GetInstance(document, fs);

                        document.Open();

                        document.Add(new Paragraph("NO OBJECTION CERTIFICATE"));
                        document.Add(new Paragraph(" "));
                        document.Add(new Paragraph($"LAN : {transaction.Lan}"));
                        document.Add(new Paragraph($"Customer : {transaction.CustName}"));
                        document.Add(new Paragraph($"Date : {DateTime.Now:dd-MMM-yyyy}"));

                        document.Close();
                    }
                }

                //----------------------------------------------------
                // 3. Update status ONLY AFTER SUCCESS
                //----------------------------------------------------
                var statusUpdated = await _repository.UpdateTransactionStatusAsync(new UpdateNocStatusDto
                {
                    Id = id,
                    Status = "Document Collected",
                    Remarks = "NOC Downloaded",
                    CreatedBy = updatedBy,
                    Role = role // IMPORTANT
                });

                if (statusUpdated != 1)
                    return (null, "", "Failed to update status.");

                var bytes = await File.ReadAllBytesAsync(filePath);

                return (bytes, fileName, "Success");
            }
            catch (Exception ex)
            {
                return (null, "", ex.Message);
            }
        }

        public async Task<(byte[]? FileBytes, string FileName, string Message)> GenerateAcknowledgementAsync(int id)
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
                    "Acknowledgements");

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

        public async Task<(byte[]? FileBytes, string FileName, string Message)> GenerateLanPdfAsync(int id)
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
                    "LanPdf");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = $"{lan}_OneNeil.pdf";

                string filePath = Path.Combine(folderPath, fileName);

                if (!File.Exists(filePath))
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Create))
                    {
                        Document document = new Document(PageSize.A4);

                        PdfWriter.GetInstance(document, fs);

                        document.Open();

                        var titleFont = FontFactory.GetFont(
                            FontFactory.HELVETICA_BOLD,
                            18);

                        var normalFont = FontFactory.GetFont(
                            FontFactory.HELVETICA,
                            14);

                        Paragraph title = new Paragraph(
                            "OneNeil",
                            titleFont);

                        title.Alignment = Element.ALIGN_CENTER;

                        document.Add(title);

                        document.Add(new Paragraph(" "));

                        Paragraph lanText = new Paragraph(
                            $"LAN Number : {lan}",
                            normalFont);

                        lanText.Alignment = Element.ALIGN_CENTER;

                        document.Add(lanText);

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

        private Dictionary<string, string> BuildPlaceholders(
            NocTransaction txn)
        {
            return new Dictionary<string, string>
            {
                { "LAN", txn.Lan ?? "" },
                { "CustName", txn.CustName ?? "" },
                { "CustomerName", txn.CustName ?? "" },
                { "BranchName", txn.BranchName ?? "" },
                { "Status", txn.Status ?? "" }
            };
        }

        private async Task<List<string>> GetNotificationRecipientsAsync(
            string branchCode)
        {
            var to = new List<string>();

            var branchEmails =
                await _repository.GetBranchEmailsAsync(branchCode);

            if (branchEmails != null)
            {
                if (!string.IsNullOrWhiteSpace(branchEmails.OPS_Email))
                    to.Add(branchEmails.OPS_Email);

                if (!string.IsNullOrWhiteSpace(branchEmails.BM_Email))
                    to.Add(branchEmails.BM_Email);

                if (!string.IsNullOrWhiteSpace(branchEmails.CM_Email))
                    to.Add(branchEmails.CM_Email);
            }

            return to
                .Distinct()
                .ToList();
        }

        public async Task<IEnumerable<string>> GetAllowedStatusesAsync(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return Enumerable.Empty<string>();

            return await _repository.GetAllowedStatusesAsync(role);
        }
    }
}