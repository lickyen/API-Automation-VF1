using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class InvoiceService
    {
        private readonly InvoiceRepository _repository;
        private readonly PurchaseOrderRepository _Prepository;
        private readonly string _filePath;
        private readonly ILogger<InvoiceService> _logger;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".jpg", ".jpeg", ".png", ".docx" };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB per file
        public InvoiceService(InvoiceRepository repository, PurchaseOrderRepository prepository,
          ILogger<InvoiceService> logger, AppSettings appSettings)
        {
            _repository = repository;
            _Prepository = prepository;
            _logger = logger;
            _filePath = appSettings.FilePaths.POBasePath;
           
        }

        public async Task<InvoiceResponse> CreateInvoiceAgainstPOAsync(CreateInvoiceAgainstPORequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.VendorContactEmail) &&
    !System.Text.RegularExpressions.Regex.IsMatch(request.VendorContactEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new ArgumentException("VendorContactEmail is not a valid email address.");
            }

            if (!string.IsNullOrWhiteSpace(request.VendorContactMobile) &&
                !System.Text.RegularExpressions.Regex.IsMatch(request.VendorContactMobile, @"^\d{10}$"))
            {
                throw new ArgumentException("VendorContactMobile must be a 10-digit number.");
            }

            var po = await _repository.GetApprovedPOForInvoiceAsync(request.POId)
                ?? throw new ArgumentException($"PO {request.POId} does not exist or is not Approved.");

            var vendorAddress = await _Prepository.GetVendorAddressByIdAsync(request.VendorAddressId)
                ?? throw new ArgumentException($"VendorAddressId {request.VendorAddressId} does not exist.");

            if (vendorAddress.VendorId != po.VendorId)
                throw new ArgumentException("The selected VendorAddressId does not belong to this PO's vendor.");

            if (await _repository.POHasFinalInvoiceAsync(request.POId))
                throw new InvalidOperationException("This PO has already been marked as fully invoiced (final invoice submitted). No further invoices can be raised against it.");

            if (request.InvoiceAmount <= 0)
                throw new ArgumentException("InvoiceAmount must be greater than 0.");

            decimal alreadyInvoiced = await _repository.GetTotalInvoicedOnPOAsync(request.POId);
            decimal totalAfterThis = alreadyInvoiced + request.InvoiceAmount;

            if (totalAfterThis > po.TotalAmount)
            {
                throw new InvalidOperationException(
                    $"This invoice would exceed the PO's total amount. PO Total: {po.TotalAmount:N2}, Already Invoiced: {alreadyInvoiced:N2}, This Invoice: {request.InvoiceAmount:N2}.");
            }
            if (request.InvoiceCopy is null)
                throw new ArgumentException("InvoiceCopy is required.");
            string folder = Path.Combine(_filePath, "InvoiceDocuments", request.InvoiceNumber.Replace("/", "_"));
            Directory.CreateDirectory(folder);
            string invoiceCopyPath = SaveVendorDocument(request.InvoiceCopy, folder, "InvoiceCopy");

            var invoice = new Models.PoInvoice
            {
                InvoiceNumber = request.InvoiceNumber,
                InvoiceType = "AgainstPO",
                POId = request.POId,
                DepartmentCode = po.DepartmentCode,
                VendorId = po.VendorId,
                VendorAddressId = request.VendorAddressId,
                VendorContactName = request.VendorContactName,    
                VendorContactMobile = request.VendorContactMobile,   
                VendorContactEmail = request.VendorContactEmail,       
                InvoiceDate = request.InvoiceDate,
                InvoiceAmount = request.InvoiceAmount,
                IsFinalInvoice = request.IsFinalInvoice,
                InvoiceCopyPath = invoiceCopyPath,
                CreatedBy = request.CreatedBy
            };

            int invoiceId = await _repository.CreateInvoiceAsync(invoice);

            var roles = await BuildInvoiceApprovalChainAsync("AgainstPO", request.InvoiceAmount);
            var steps = roles.Select((role, i) => new Models.PoInvoiceApprovalHistory
            {
                StepNumber = i + 1,
                ApproverRole = role
            }).ToList();

            await _repository.AddInvoiceApprovalStepsAsync(invoiceId, steps);

            return await GetInvoiceByIdAsync(invoiceId);
        }

        public async Task<InvoiceResponse> GetInvoiceByIdAsync(int invoiceId)
        {
            var invoice = await _repository.GetInvoiceByIdAsync(invoiceId)
                ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

            var approvalHistory = (await _repository.GetInvoiceApprovalHistoryAsync(invoiceId)).ToList();
            var payments = (await _repository.GetInvoicePaymentsAsync(invoiceId)).ToList();

            PoPurchaseOrder? po = null;
            decimal? totalInvoicedOnPO = null;
            if (invoice.POId is not null)
            {
                po = await _Prepository.GetPurchaseOrderByIdAsync(invoice.POId.Value);
                totalInvoicedOnPO = await _repository.GetTotalInvoicedOnPOAsync(invoice.POId.Value);
            }

            var vendorAddress = await _Prepository.GetVendorAddressByIdAsync(invoice.VendorAddressId);
            var (vendor, _) = await _Prepository.GetVendorByIdAsync(invoice.VendorId);

            var pendingStep = approvalHistory.FirstOrDefault(h => h.StepNumber == invoice.CurrentApprovalStep);
            string? pendingRole = invoice.Status == "PendingApproval" ? pendingStep?.ApproverRole : null;

            return new InvoiceResponse
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceType = invoice.InvoiceType,
                DepartmentCode = invoice.DepartmentCode,
                POId = invoice.POId,
                PONumber = po?.PONumber,
                VendorId = invoice.VendorId,
                VendorName = vendor?.VendorName ?? string.Empty,
                VendorAddressId = invoice.VendorAddressId,
                VendorAddress = vendorAddress is not null ? $"{vendorAddress.AddressLine1}, {vendorAddress.City}" : string.Empty,
                VendorContactName = invoice.VendorContactName,
                VendorContactMobile = invoice.VendorContactMobile,
                VendorContactEmail = invoice.VendorContactEmail,
                InvoiceDate = invoice.InvoiceDate,
                InvoiceAmount = invoice.InvoiceAmount,
                POAmount = po?.TotalAmount,
                TotalInvoicedOnPO = totalInvoicedOnPO,
                RemainingOnPO = po is not null && totalInvoicedOnPO is not null ? po.TotalAmount - totalInvoicedOnPO : null,
                IsFinalInvoice = invoice.IsFinalInvoice,
                Status = invoice.Status,
                RejectionReason = invoice.RejectionReason,
                PendingWithRole = pendingRole,
                ApprovalTrail = approvalHistory.Select(h => new InvoiceApprovalStepResponse
                {
                    StepNumber = h.StepNumber,
                    ApproverRole = h.ApproverRole,
                    Action = h.Action,
                    Remarks = h.Remarks,
                    ActionedDt = h.ActionedDt
                }).ToList(),
                Payments = payments.Select(p => new InvoicePaymentResponse
                {
                    PaymentId = p.PaymentId,
                    PaymentStatus = p.PaymentStatus,
                    FailReason = p.FailReason,
                    UTRNumber = p.UTRNumber,
                    PaymentDate = p.PaymentDate,
                    AmountPaid = p.AmountPaid,
                    TDSAmount = p.TDSAmount
                }).ToList(),
                InvoiceCopy = LoadVendorDocument(invoice.InvoiceCopyPath),
            };
        }

        public async Task ActOnInvoiceApprovalAsync(InvoiceApprovalActionRequest request)
        {
            var invoice = await _repository.GetInvoiceByIdAsync(request.InvoiceId)
                ?? throw new KeyNotFoundException($"Invoice {request.InvoiceId} not found.");

            if (invoice.Status != "PendingApproval")
                throw new InvalidOperationException($"This invoice is not awaiting approval (current status: {invoice.Status}).");

            var currentStep = await _repository.GetPendingInvoiceApprovalStepAsync(invoice.InvoiceId, invoice.CurrentApprovalStep)
                ?? throw new InvalidOperationException("No pending approval step was found for this invoice.");

            var approver = await _Prepository.GetActiveUserByEmployeeCodeAsync(request.ApproverEmployeeCode)
                ?? throw new ArgumentException($"Approver '{request.ApproverEmployeeCode}' not found or inactive.");

            var approverRoles = (await _Prepository.GetActiveRoleNamesForUserAsync(approver.Id)).ToList();
            if (!approverRoles.Contains(currentStep.ApproverRole, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"'{request.ApproverEmployeeCode}' does not hold the '{currentStep.ApproverRole}' role required for this step.");

            await _repository.MarkInvoiceApprovalStepActionedAsync(
                currentStep.ApprovalId, request.ApproverEmployeeCode, request.IsApproved ? "Approved" : "Rejected", request.Remarks);

            if (!request.IsApproved)
            {
                await _repository.UpdateInvoiceStatusAsync(invoice.InvoiceId, "Rejected", 0, request.Remarks);
                return;
            }

            // Check if there's a next step — now relevant for multi-step WithoutPO chains
            var allSteps = await _repository.GetInvoiceApprovalHistoryAsync(invoice.InvoiceId);
            bool hasNextStep = allSteps.Any(s => s.StepNumber == invoice.CurrentApprovalStep + 1);

            if (hasNextStep)
            {
                await _repository.UpdateInvoiceStatusAsync(invoice.InvoiceId, "PendingApproval", invoice.CurrentApprovalStep + 1);
                // optionally: NotifyPendingInvoiceApproverAsync(...) here, mirroring the PO email notification
            }
            else
            {
                // Final step cleared — moves to Finance for payment, same as before
                await _repository.UpdateInvoiceStatusAsync(invoice.InvoiceId, "PendingPayment", invoice.CurrentApprovalStep);
            }
        }
        public async Task RecordInvoicePaymentAsync(RecordInvoicePaymentRequest request)
        {
            var invoice = await _repository.GetInvoiceByIdAsync(request.InvoiceId)
                ?? throw new KeyNotFoundException($"Invoice {request.InvoiceId} not found.");

            if (invoice.Status != "PendingPayment")
                throw new InvalidOperationException($"This invoice is not awaiting payment (current status: {invoice.Status}).");

            var user = await _Prepository.GetActiveUserByEmployeeCodeAsync(request.CreatedBy)
                ?? throw new ArgumentException($"User '{request.CreatedBy}' not found or inactive.");

            var roles = (await _Prepository.GetActiveRoleNamesForUserAsync(user.Id)).ToList();
            if (!roles.Contains("FinanceUser", StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"'{request.CreatedBy}' does not hold the 'FinanceUser' role required to record payments.");

            if (!new[] { "Success", "Failed", "Hold" }.Contains(request.PaymentStatus, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("PaymentStatus must be 'Success', 'Failed', or 'Hold'.");

            if (!string.Equals(request.PaymentStatus, "Success", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(request.FailReason))
                throw new ArgumentException("A reason is required (in FailReason) when PaymentStatus is 'Failed' or 'Hold'.");

            decimal alreadyPaid = await _repository.GetTotalSuccessfulInvoicePaymentsAsync(invoice.InvoiceId);
            if (string.Equals(request.PaymentStatus, "Success", StringComparison.OrdinalIgnoreCase) &&
                alreadyPaid + request.AmountPaid > invoice.InvoiceAmount)
            {
                throw new InvalidOperationException(
                    $"This payment would exceed the invoice amount. Invoice: {invoice.InvoiceAmount:N2}, already paid: {alreadyPaid:N2}, attempted: {request.AmountPaid:N2}.");
            }

            await _repository.AddInvoicePaymentAsync(new Models.PoInvoicePayment
            {
                InvoiceId = request.InvoiceId,
                PaymentStatus = request.PaymentStatus,
                FailReason = request.FailReason,
                UTRNumber = request.UTRNumber,
                PaymentDate = request.PaymentDate,
                AmountPaid = request.AmountPaid,
                TDSAmount = request.TDSAmount,
                CreatedBy = request.CreatedBy
            });

            if (string.Equals(request.PaymentStatus, "Success", StringComparison.OrdinalIgnoreCase) &&
                alreadyPaid + request.AmountPaid >= invoice.InvoiceAmount)
            {
                await _repository.UpdateInvoiceStatusAsync(invoice.InvoiceId, "Paid", invoice.CurrentApprovalStep);
            }
        }

        public async Task<InvoiceResponse> CreateInvoiceAgainstAgreementAsync(CreateInvoiceAgainstAgreementRequest request)
        {
            var agreement = await _repository.GetAgreementByIdAsync(request.AgreementId)
                ?? throw new ArgumentException($"Agreement {request.AgreementId} does not exist.");

            bool withinPeriod = DateTime.Today >= agreement.EffectiveDate.Date && DateTime.Today <= agreement.ExpiryDate.Date;

            if (!agreement.IsValid)
                throw new InvalidOperationException("This agreement has been marked invalid and cannot be used.");

            if (!withinPeriod)
                throw new InvalidOperationException($"This agreement is not within its effective period (Effective: {agreement.EffectiveDate:dd-MMM-yyyy}, Expiry: {agreement.ExpiryDate:dd-MMM-yyyy}).");

            var vendorAddress = await _Prepository.GetVendorAddressByIdAsync(request.VendorAddressId)
                ?? throw new ArgumentException($"VendorAddressId {request.VendorAddressId} does not exist.");

            if (vendorAddress.VendorId != agreement.VendorId)
                throw new ArgumentException("The selected VendorAddressId does not belong to this agreement's vendor.");

            if (!string.IsNullOrWhiteSpace(request.VendorContactEmail) &&
                !System.Text.RegularExpressions.Regex.IsMatch(request.VendorContactEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("VendorContactEmail is not a valid email address.");

            if (!string.IsNullOrWhiteSpace(request.VendorContactMobile) &&
                !System.Text.RegularExpressions.Regex.IsMatch(request.VendorContactMobile, @"^\d{10}$"))
                throw new ArgumentException("VendorContactMobile must be a 10-digit number.");

            if (request.InvoiceAmount <= 0)
                throw new ArgumentException("InvoiceAmount must be greater than 0.");

            var invoice = new Models.PoInvoice
            {
                InvoiceNumber = request.InvoiceNumber,
                InvoiceType = "AgainstAgreement",
                AgreementId = request.AgreementId,
                VendorId = agreement.VendorId,
                VendorAddressId = request.VendorAddressId,
                VendorContactName = request.VendorContactName,
                VendorContactMobile = request.VendorContactMobile,
                VendorContactEmail = request.VendorContactEmail,
                InvoiceDate = request.InvoiceDate,
                InvoiceAmount = request.InvoiceAmount,
                IsFinalInvoice = false,
                CreatedBy = request.CreatedBy
            };

            int invoiceId = await _repository.CreateInvoiceAsync(invoice);

            var roles = await BuildInvoiceApprovalChainAsync("AgainstAgreement", request.InvoiceAmount);
            var steps = roles.Select((role, i) => new Models.PoInvoiceApprovalHistory
            {
                StepNumber = i + 1,
                ApproverRole = role
            }).ToList();

            await _repository.AddInvoiceApprovalStepsAsync(invoiceId, steps);

            return await GetInvoiceByIdAsync(invoiceId);
        }

        //---- --------------Aggrement Related-------------
        public async Task<POAgreementResponse> CreateAgreementAsync(CreateAgreementRequest request)
        {
            var (vendor, _) = await _Prepository.GetVendorByIdAsync(request.VendorId);
            if (vendor is null)
                throw new ArgumentException($"VendorId {request.VendorId} does not exist.");

            if (await _repository.AgreementNumberExistsAsync(request.AgreementNumber))
                throw new ArgumentException($"AgreementNumber '{request.AgreementNumber}' already exists.");

            if (request.ExpiryDate <= request.EffectiveDate)
                throw new ArgumentException("ExpiryDate must be after EffectiveDate.");

            string? copyPath = null;
            if (request.AgreementCopy is not null)
            {
                string folder = Path.Combine(_filePath, "AgreementDocuments", request.AgreementNumber);
                Directory.CreateDirectory(folder);
                copyPath = SaveVendorDocument(request.AgreementCopy, folder, "AgreementCopy");
            }

            var agreement = new Models.PoAgreementMaster
            {
                AgreementNumber = request.AgreementNumber,
                VendorId = request.VendorId,
                EffectiveDate = request.EffectiveDate,
                ExpiryDate = request.ExpiryDate,
                AgreementCopyPath = copyPath,
                CreatedBy = request.CreatedBy
            };

            int agreementId = await _repository.CreateAgreementAsync(agreement);

            return await GetAgreementByIdAsync(agreementId);
        }

        public async Task<POAgreementResponse> GetAgreementByIdAsync(int agreementId)
        {
            var agreement = await _repository.GetAgreementByIdAsync(agreementId)
                ?? throw new KeyNotFoundException($"Agreement {agreementId} not found.");

            var (vendor, _) = await _Prepository.GetVendorByIdAsync(agreement.VendorId);

            return MapToAgreementResponse(agreement, vendor?.VendorName ?? string.Empty);
        }

        public async Task<POAgreementResponse> UpdateAgreementAsync(UpdateAgreementRequest request)
        {
            var existing = await _repository.GetAgreementByIdAsync(request.AgreementId)
                ?? throw new KeyNotFoundException($"Agreement {request.AgreementId} not found.");

            if (request.ExpiryDate <= request.EffectiveDate)
                throw new ArgumentException("ExpiryDate must be after EffectiveDate.");

            string? copyPath = existing.AgreementCopyPath;
            if (request.AgreementCopy is not null)
            {
                string folder = Path.Combine(_filePath, "AgreementDocuments", existing.AgreementNumber);
                Directory.CreateDirectory(folder);
                copyPath = SaveVendorDocument(request.AgreementCopy, folder, "AgreementCopy");
            }

            var agreement = new Models.PoAgreementMaster
            {
                AgreementId = request.AgreementId,
                EffectiveDate = request.EffectiveDate,
                ExpiryDate = request.ExpiryDate,
                AgreementCopyPath = copyPath,
                ModifiedBy = request.ModifiedBy
            };

            await _repository.UpdateAgreementAsync(agreement);

            return await GetAgreementByIdAsync(request.AgreementId);
        }

        public async Task UpdateAgreementValidityAsync(UpdateAgreementValidityRequest request)
        {
            _ = await _repository.GetAgreementByIdAsync(request.AgreementId)
                ?? throw new KeyNotFoundException($"Agreement {request.AgreementId} not found.");

            await _repository.UpdateAgreementValidityAsync(request.AgreementId, request.IsValid, request.ModifiedBy);
        }

        public async Task<IEnumerable<POAgreementResponse>> GetAgreementsByVendorAsync(long vendorId)
        {
            var rows = await _repository.GetAgreementsByVendorAsync(vendorId);
            return rows.Select(r => MapToAgreementResponse(
                new Models.PoAgreementMaster
                {
                    AgreementId = r.AgreementId,
                    AgreementNumber = r.AgreementNumber,
                    VendorId = r.VendorId,
                    EffectiveDate = r.EffectiveDate,
                    ExpiryDate = r.ExpiryDate,
                    IsValid = r.IsValid,
                    AgreementCopyPath = r.AgreementCopyPath
                }, (string)r.VendorName));
        }

        private static POAgreementResponse MapToAgreementResponse(Models.PoAgreementMaster a, string vendorName)
        {
            bool withinPeriod = DateTime.Today >= a.EffectiveDate.Date && DateTime.Today <= a.ExpiryDate.Date;

            return new POAgreementResponse
            {
                AgreementId = a.AgreementId,
                AgreementNumber = a.AgreementNumber,
                VendorId = a.VendorId,
                VendorName = vendorName,
                EffectiveDate = a.EffectiveDate,
                ExpiryDate = a.ExpiryDate,
                IsValid = a.IsValid,
                IsWithinEffectivePeriod = withinPeriod,
                IsUsable = a.IsValid && withinPeriod,
                AgreementCopy = LoadVendorDocument(a.AgreementCopyPath)
            };
        }

        private string SaveVendorDocument(VendorDocumentUpload doc, string folder, string label)
        {
            if (string.IsNullOrWhiteSpace(doc.FileName) || string.IsNullOrWhiteSpace(doc.Base64Content))
                throw new ArgumentException($"{label} document must include a FileName and Base64Content.");

            string extension = Path.GetExtension(doc.FileName);
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException($"{label} document type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}.");

            byte[] fileBytes;
            try
            {
                string cleanBase64 = doc.Base64Content.Contains(",")
                    ? doc.Base64Content.Substring(doc.Base64Content.IndexOf(",") + 1)
                    : doc.Base64Content;
                fileBytes = Convert.FromBase64String(cleanBase64);
            }
            catch (FormatException)
            {
                throw new ArgumentException($"{label} document contains invalid base64 content.");
            }

            if (fileBytes.LongLength > MaxFileSizeBytes)
                throw new ArgumentException($"{label} document exceeds the 5 MB size limit.");

            string fileName = $"{label}{extension}";
            string fullPath = Path.Combine(folder, fileName);
            File.WriteAllBytes(fullPath, fileBytes);

            return fullPath;
        }
        private static VendorDocumentResponse? LoadVendorDocument(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

            return new VendorDocumentResponse
            {
                FileName = Path.GetFileName(path),
                Base64Content = Convert.ToBase64String(File.ReadAllBytes(path))
            };
        }

        public async Task<InvoiceDashboardResponse> GetInvoiceDashboardAsync(string employeeCode)
        {
            var user = await _Prepository.GetActiveUserByEmployeeCodeAsync(employeeCode)
                ?? throw new ArgumentException($"User '{employeeCode}' not found or inactive.");

            var roles = (await _Prepository.GetActiveRoleNamesForUserAsync(user.Id)).ToList();

            if (roles.Count == 0)
                throw new InvalidOperationException($"User '{employeeCode}' has no active role mapped — cannot determine invoice access.");

            bool isDeptHead = roles.Contains("DepartmentHead", StringComparer.OrdinalIgnoreCase);
            bool isFinanceUser = roles.Contains("FinanceUser", StringComparer.OrdinalIgnoreCase);
            bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            bool hasAnyApprovalOrFinanceRole = isDeptHead || isFinanceUser || isAdmin;

            List<InvoiceListItemResponse> pendingWithMe = new();
            List<InvoiceListItemResponse> inProgress = new();
            List<InvoiceListItemResponse> completed = new();

            // ---------------- Preparer buckets — always included, every employee can be a Maker ----------------
            var rejectedRaw = await _repository.GetInvoicesRejectedByCreatorAsync(employeeCode);
            pendingWithMe.AddRange(MapToInvoiceListResponse(rejectedRaw));

            var myInProgressRaw = await _repository.GetInvoicesInProgressByCreatorAsync(employeeCode);
            inProgress.AddRange(MapToInvoiceListResponse(myInProgressRaw));

            var myPaidRaw = await _repository.GetInvoicesPaidByCreatorAsync(employeeCode);
            completed.AddRange(MapToInvoiceListResponse(myPaidRaw));

            // ---------------- DepartmentHead buckets — merged in on top of Preparer buckets ----------------
            if (isDeptHead)
            {
                var pendingApprovalRaw = await _repository.GetInvoicesPendingForDeptHeadAsync(user.DepartmentCode);
                List<InvoiceListItemResponse> pendingApprovalMapped = MapToInvoiceListResponse(pendingApprovalRaw);

                var existingPendingIds = pendingWithMe.Select(p => p.InvoiceId).ToHashSet();
                pendingWithMe.AddRange(pendingApprovalMapped.Where(x => !existingPendingIds.Contains(x.InvoiceId)));

                var actedRaw = await _repository.GetInvoicesActedByUserAsync(employeeCode);
                List<InvoiceListItemResponse> actedMapped = MapToInvoiceListResponse(actedRaw);

                var currentPendingIds = pendingWithMe.Select(p => p.InvoiceId).ToHashSet();
                var existingInProgressIds = inProgress.Select(i => i.InvoiceId).ToHashSet();
                var existingCompletedIds = completed.Select(c => c.InvoiceId).ToHashSet();

                foreach (var item in actedMapped)
                {
                    if (currentPendingIds.Contains(item.InvoiceId)) continue;

                    if (item.Status is "PendingPayment" or "PendingApproval")
                    {
                        if (!existingInProgressIds.Contains(item.InvoiceId)) inProgress.Add(item);
                    }
                    else
                    {
                        if (!existingCompletedIds.Contains(item.InvoiceId)) completed.Add(item);
                    }
                }
            }

            // ---------------- FinanceUser / Admin buckets — merged in on top ----------------
            if (isFinanceUser || isAdmin)
            {
                var pendingPaymentRaw = await _repository.GetInvoicesPendingPaymentAllAsync();
                List<InvoiceListItemResponse> pendingPaymentMapped = MapToInvoiceListResponse(pendingPaymentRaw);

                var existingPendingIds = pendingWithMe.Select(p => p.InvoiceId).ToHashSet();
                pendingWithMe.AddRange(pendingPaymentMapped.Where(x => !existingPendingIds.Contains(x.InvoiceId)));

                var paidByMeRaw = await _repository.GetInvoicesPaidByUserAsync(employeeCode);
                List<InvoiceListItemResponse> paidByMeMapped = MapToInvoiceListResponse(paidByMeRaw);

                var existingCompletedIds = completed.Select(c => c.InvoiceId).ToHashSet();
                completed.AddRange(paidByMeMapped.Where(x => !existingCompletedIds.Contains(x.InvoiceId)));
            }

            return new InvoiceDashboardResponse
            {
                PendingWithMe = pendingWithMe,
                InProgress = inProgress,
                Completed = completed
            };
        }

        private static InvoiceListItemResponse MapSingle(dynamic row) => new()
        {
            InvoiceId = row.InvoiceId,
            InvoiceNumber = row.InvoiceNumber,
            InvoiceType = row.InvoiceType,
            POId = row.POId,
            PONumber = row.PONumber,
            VendorName = row.VendorName,
            InvoiceAmount = row.InvoiceAmount,
            Status = row.Status,
            CreatedBy = row.CreatedBy,
            CreatedDt = row.CreatedDt
        };

        private static List<InvoiceListItemResponse> MapToInvoiceListResponse(IEnumerable<dynamic> rows)
            => rows.Select(r => (InvoiceListItemResponse)MapSingle(r)).ToList();

        private async Task<List<string>> BuildInvoiceApprovalChainAsync(string invoiceType, decimal invoiceAmount)
        {
            if (string.Equals(invoiceType, "WithoutPO", StringComparison.OrdinalIgnoreCase))
            {
                // Same amount-tiered chain as PO, using the same configurable thresholds
                var settings = await _Prepository.GetSettingsAsync(
                    new[] { "ApprovalTier1Threshold", "ApprovalTier2Threshold" });

                decimal tier1Threshold = settings.TryGetValue("ApprovalTier1Threshold", out var t1) && decimal.TryParse(t1, out var t1Val)
                    ? t1Val : 100000m;

                decimal tier2Threshold = settings.TryGetValue("ApprovalTier2Threshold", out var t2) && decimal.TryParse(t2, out var t2Val)
                    ? t2Val : 3000000m;

                if (invoiceAmount < tier1Threshold)
                    return new List<string> { "DepartmentHead", "FinanceHead" };

                if (invoiceAmount <= tier2Threshold)
                    return new List<string> { "DepartmentHead", "FinanceHead", "CFO" };

                return new List<string> { "DepartmentHead", "FinanceHead", "CFO", "ManagingDirector" };
            }

            // AgainstPO and AgainstAgreement — single step, DeptHead only, then straight to Finance for payment
            return new List<string> { "DepartmentHead" };
        }

        public async Task<InvoiceResponse> CreateInvoiceWithoutPOAsync(CreateInvoiceWithoutPORequest request)
        {
            if (request.Items is null || request.Items.Count == 0)
                throw new ArgumentException("An invoice must have at least one line item.");

            if (!await _Prepository.VendorExistsAsync(request.VendorId))
                throw new ArgumentException($"VendorId {request.VendorId} does not exist or is not Approved.");

            var vendorAddress = await _Prepository.GetVendorAddressByIdAsync(request.VendorAddressId)
                ?? throw new ArgumentException($"VendorAddressId {request.VendorAddressId} does not exist.");

            if (vendorAddress.VendorId != request.VendorId)
                throw new ArgumentException("The selected VendorAddressId does not belong to this vendor.");

            if (!await _Prepository.DepartmentExistsAsync(request.DepartmentCode))
                throw new ArgumentException($"DepartmentCode '{request.DepartmentCode}' does not exist or is inactive.");

            if (!await _Prepository.SubClassificationExistsAsync(request.DepartmentCode, request.Classification, request.SubClassification))
                throw new ArgumentException($"SubClassification '{request.SubClassification}' is not valid for department '{request.DepartmentCode}' / classification '{request.Classification}'.");

            foreach (var line in request.Items)
            {
                if (!await _Prepository.BudgetCategoryExistsAsync(line.BudgetCategoryId, request.DepartmentCode))
                    throw new ArgumentException($"BudgetCategoryId {line.BudgetCategoryId} does not exist for department '{request.DepartmentCode}' (item: '{line.ItemDescription}').");
            }

            if (request.DeliveryLocationIds is null || request.DeliveryLocationIds.Count == 0)
                throw new ArgumentException("At least one DeliveryLocationId is required.");

            if (!await _Prepository.AllLocationsExistAsync(request.DeliveryLocationIds))
                throw new ArgumentException("One or more DeliveryLocationIds do not exist or are inactive.");

            var selectedTerms = new List<Models.PoTermsConditionsMaster>();
            if (request.TermMasterIds is { Count: > 0 })
            {
                var allTerms = (await _Prepository.GetTermsMasterAsync()).ToList();
                var validIds = allTerms.Select(t => t.TermMasterId).ToHashSet();
                var invalidIds = request.TermMasterIds.Where(id => !validIds.Contains(id)).ToList();

                if (invalidIds.Count > 0)
                    throw new ArgumentException($"The following TermMasterId(s) do not exist or are inactive: {string.Join(", ", invalidIds)}.");

                selectedTerms = allTerms.Where(t => request.TermMasterIds.Contains(t.TermMasterId)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.VendorContactEmail) &&
                !System.Text.RegularExpressions.Regex.IsMatch(request.VendorContactEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("VendorContactEmail is not a valid email address.");

            if (!string.IsNullOrWhiteSpace(request.VendorContactMobile) &&
                !System.Text.RegularExpressions.Regex.IsMatch(request.VendorContactMobile, @"^\d{10}$"))
                throw new ArgumentException("VendorContactMobile must be a 10-digit number.");

            if (request.InvoiceCopy is null)
                throw new ArgumentException("InvoiceCopy is required.");

            // ---------------- Items + GST calc (same pattern as PO) ----------------
            var items = new List<Models.PoInvoiceItem>();
            decimal subTotal = 0m, gstTotal = 0m;

            foreach (var line in request.Items)
            {
                decimal amount = line.Quantity * line.UnitPrice;
                decimal gstAmount = Math.Round(amount * line.GSTRate / 100m, 2);

                items.Add(new Models.PoInvoiceItem
                {
                    ItemDescription = line.ItemDescription,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Amount = amount,
                    GSTRate = line.GSTRate,
                    GSTAmount = gstAmount,
                    BudgetCategoryId = line.BudgetCategoryId
                });

                subTotal += amount;
                gstTotal += gstAmount;
            }

            decimal totalAmount = subTotal + gstTotal;

            // ---------------- Budget check (same pattern as PO) ----------------
            var categoryTotals = items.GroupBy(i => i.BudgetCategoryId).ToDictionary(g => g.Key, g => g.Sum(i => i.Amount + i.GSTAmount));
            var allocatedByCategory = await _Prepository.GetBudgetAmountsAsync(categoryTotals.Keys, GetCurrentBudgetYear());
            var consumedByCategory = await _Prepository.GetConsumedBudgetsAsync(categoryTotals.Keys, GetCurrentBudgetYear());

            var exceededCategories = new HashSet<int>();
            foreach (var (categoryId, thisInvoiceAmount) in categoryTotals)
            {
                bool hasAllocation = allocatedByCategory.TryGetValue(categoryId, out var allocated);
                decimal consumed = consumedByCategory.TryGetValue(categoryId, out var c) ? c : 0m;
                if (!hasAllocation || (consumed + thisInvoiceAmount) > allocated)
                    exceededCategories.Add(categoryId);
            }

            foreach (var item in items)
                item.IsBudgetExceeded = exceededCategories.Contains(item.BudgetCategoryId);

            bool budgetExceeded = items.Any(i => i.IsBudgetExceeded);

            // ---------------- File save ----------------
            string folder = Path.Combine(_filePath, "InvoiceDocuments", request.InvoiceNumber.Replace("/", "_"));
            Directory.CreateDirectory(folder);
            string invoiceCopyPath = SaveVendorDocument(request.InvoiceCopy, folder, "InvoiceCopy");

            // ---------------- Header insert ----------------
            var invoice = new Models.PoInvoice
            {
                InvoiceNumber = request.InvoiceNumber,
                InvoiceType = "WithoutPO",
                DepartmentCode = request.DepartmentCode,
                Classification = request.Classification,
                SubClassification = request.SubClassification,
                VendorId = request.VendorId,
                VendorAddressId = request.VendorAddressId,
                VendorContactName = request.VendorContactName,
                VendorContactMobile = request.VendorContactMobile,
                VendorContactEmail = request.VendorContactEmail,
                InvoiceDate = request.InvoiceDate,
                InvoiceAmount = totalAmount,     
                SubTotal = subTotal,
                GSTAmount = gstTotal,
                TotalAmount = totalAmount,
                IsBudgetExceeded = budgetExceeded,
                IsFinalInvoice = false,
                InvoiceCopyPath = invoiceCopyPath,
                CreatedBy = request.CreatedBy
            };

            int invoiceId = await _repository.CreateInvoiceAsync(invoice);

            var itemsList = items.ToList();
            await _repository.AddInvoiceItemsAsync(invoiceId, itemsList);

            if (selectedTerms.Count > 0)
                await _repository.AddInvoiceTermsAsync(invoiceId, request.TermMasterIds, request.CreatedBy);

            await _repository.AddInvoiceDeliveryLocationsAsync(invoiceId, request.DeliveryLocationIds, request.CreatedBy);

            // ---------------- Approval chain — amount-tiered, same as PO ----------------
            var roles = await BuildInvoiceApprovalChainAsync("WithoutPO", totalAmount);
            var steps = roles.Select((role, i) => new Models.PoInvoiceApprovalHistory
            {
                StepNumber = i + 1,
                ApproverRole = role
            }).ToList();

            await _repository.AddInvoiceApprovalStepsAsync(invoiceId, steps);

            return await GetInvoiceByIdAsync(invoiceId);
        }

        private static string GetCurrentBudgetYear()
        {
            var today = DateTime.UtcNow;
            int startYear = today.Month >= 4 ? today.Year : today.Year - 1;
            return $"FY{startYear}-{(startYear + 1).ToString().Substring(2)}";
        }

    }
}
