using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Text;
using System.Text.RegularExpressions;

namespace ProductivityApi.Services
{
    public sealed class PurchaseOrderService
    {


        private readonly PurchaseOrderRepository _repository;
        private readonly string _filePath;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".jpg", ".jpeg", ".png", ".docx" };

        private readonly string _templatePath;
        private readonly string _logoPath;


        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB per file

        private readonly ILogger<PurchaseOrderService> _logger;

        public PurchaseOrderService(PurchaseOrderRepository repository,
            ILogger<PurchaseOrderService> logger, AppSettings appSettings)
        {
            _repository = repository;
            _logger = logger;
            _filePath = appSettings.FilePaths.POBasePath;
            _templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "PO", "Purchaseordertemplate.html");
            _logoPath = Path.Combine(AppContext.BaseDirectory, "Templates", "PO", "assets", "vistaar Logo.png");
        }
        public async Task<POUserAccessResponse> CheckAccessAsync(string employeeCode)
        {
            var user = await _repository.GetActiveUserByEmployeeCodeAsync(employeeCode);

            if (user is null)
            {
                return new POUserAccessResponse
                {
                    HasAccess = false,
                    EmployeeCode = employeeCode,
                    Message = "User not found or inactive in PO module."
                };
            }
            var roles = (await _repository.GetActiveRoleNamesForUserAsync(user.Id)).ToList();

            return new POUserAccessResponse
            {
                HasAccess = roles.Count > 0,
                EmployeeCode = user.EmployeeCode,
                Name = user.Name,
                DepartmentCode = user.DepartmentCode,
                IsDepartmentHead = user.IsDepartmentHead,
                Roles = roles,
                Message = roles.Count > 0 ? null : "User exists but has no active role mapped."
            };
        }

        public async Task<IEnumerable<POVendorListItemResponse>> GetAllVendorsAsync(string? status = null)
        {
            var vendors = await _repository.GetAllVendorsAsync(status);

            return vendors.Select(v => new POVendorListItemResponse
            {
                VendorId = v.VendorId,
                VendorCode = v.VendorCode,
                VendorName = v.VendorName,
                MSMEVendor = v.MSMEVendor,
                Status = v.Status
            });
        }
        public async Task<POVendorDetailResponse> GetVendorByIdAsync(long vendorId)
        {
            var (vendor, addresses) = await _repository.GetVendorByIdAsync(vendorId);

            if (vendor is null)
                throw new KeyNotFoundException($"Vendor {vendorId} not found.");

            return new POVendorDetailResponse
            {
                VendorId = vendor.VendorId,
                VendorCode = vendor.VendorCode,
                VendorName = vendor.VendorName,
                BankHolderName = vendor.BankHolderName,
                BankAccountNo = vendor.BankAccountNo,
                BankName = vendor.BankName,
                IFSCCode = vendor.IFSCCode,
                MSMEVendor = vendor.MSMEVendor,
                UDYAMRegnNo = vendor.UDYAMRegnNo,
                VendorPAN = vendor.VendorPAN,
                Status = vendor.Status,
                Addresses = addresses.Select(a => new POVendorAddressResponse
                {
                    VendorAddressId = a.VendorAddressId,
                    AddressLine1 = a.AddressLine1,
                    City = a.City,
                    StateName = a.StateName,
                    StateCode = a.StateCode,
                    PinCode = a.PinCode,
                    GSTIN = a.GSTIN,
                    IsPrimaryAddress = a.IsPrimaryAddress,
                    Status = a.Status
                }).ToList(),
                PANDocument = LoadVendorDocument(vendor.PANDocumentPath),
                MSMEDocument = LoadVendorDocument(vendor.MSMEDocumentPath),
                BankAccountProofDocument = LoadVendorDocument(vendor.BankAccountProofPath)
            };

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
        // ---------------- Departments / Budget ----------------

        public async Task<IEnumerable<PODepartmentResponse>> GetDepartmentsAsync()
        {
            var departments = await _repository.GetDepartmentsAsync();

            return departments.Select(d => new PODepartmentResponse
            {
                Id = d.Id,
                Department = d.Department,
                DepartmentCode = d.DepartmentCode
            });
        }

        public async Task<IEnumerable<PODepartmentBudgetResponse>> GetBudgetAsync(string? budgetYear = null)
        {
            var budgets = await _repository.GetBudgetAsync(budgetYear);
            return budgets.Select(r => new PODepartmentBudgetResponse
            {
                Id = (int)r.Id,
                DepartmentCode = (string)r.DepartmentCode,
                Classification = (string)r.Classification,
                SubClassification = (string)r.SubClassification,
                ItemCategory = (string)r.ItemCategory,
                BudgetYear = (string)r.BudgetYear,
                Budget = (decimal)r.Budget,
                IsActive = (bool)r.IsActive
            });
        }

        public async Task<IEnumerable<PODepartmentBudgetResponse>> GetDepartmentWiseBudgetAsync(
            string? department, string? budgetYear, string? classification)
        {
            var budgets = await _repository.GetDepartmentWiseBudgetAsync(department, budgetYear, classification);
            return budgets.Select(MapToBudgetResponse);
        }
        public async Task<IEnumerable<PurchaseOrderTermResponse>> GetTermsMasterAsync(string? termCategory)
        {
            var terms = await _repository.GetTermsMasterAsync(termCategory);
            return terms.Select(t => new PurchaseOrderTermResponse
            {
                TermMasterId = t.TermMasterId,
                TermType = t.TermType,
                TermCategory = t.TermCategory,
                TermsAndConditionDetails = t.TermsAndConditionDetails
            });
        }

        private static PODepartmentBudgetResponse MapToBudgetResponse(Models.PoDeptWiseBudget b) => new()
        {
            Id = b.Id,
            DepartmentCode = b.DepartmentCode,
            Classification = b.Classification,
            SubClassification = b.SubClassification,
            ItemCategory = b.ItemCategory,
            BudgetYear = b.BudgetYear,
            Budget = b.Budget,
            IsActive = b.IsActive
        };



        public async Task<IEnumerable<PODepartmentBudgetResponse>> GetBudgetLinesAsync(string departmentCode, string classification, string budgetYear)
        {
            var lines = await _repository.GetBudgetLinesAsync(departmentCode, classification, budgetYear);
            return lines.Select(b => new PODepartmentBudgetResponse
            {
                Id = b.Id,
                DepartmentCode = b.DepartmentCode,
                Classification = b.Classification,
                SubClassification = b.SubClassification,
                ItemCategory = b.ItemCategory,
                BudgetYear = b.BudgetYear,
                Budget = b.Budget,
                IsActive = b.IsActive
            });
        }
        
        public async Task<PurchaseOrderResponse> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request)
        {
            if (request.Items is null || request.Items.Count == 0)
                throw new ArgumentException("A Purchase Order must have at least one line item.");
            // ==================== LOAD CONFIGURABLE THRESHOLDS ====================

            var settings = await _repository.GetSettingsAsync(
                new[] { "QuotationThresholdAmount", "ApprovalTier1Threshold", "ApprovalTier2Threshold" });

            decimal quotationThreshold = settings.TryGetValue("QuotationThresholdAmount", out var qt) && decimal.TryParse(qt, out var qtVal)
                ? qtVal : 25000m;   // fallback default if setting missing

            decimal tier1Threshold = settings.TryGetValue("ApprovalTier1Threshold", out var t1) && decimal.TryParse(t1, out var t1Val)
                ? t1Val : 100000m;

            decimal tier2Threshold = settings.TryGetValue("ApprovalTier2Threshold", out var t2) && decimal.TryParse(t2, out var t2Val)
                ? t2Val : 3000000m;


            // ==================== PRE-VALIDATION ====================
            if (string.IsNullOrWhiteSpace(request.SubClassification))
                throw new ArgumentException("SubClassification is required.");

            if (!await _repository.SubClassificationExistsAsync(request.DepartmentCode, request.Classification, request.SubClassification))
                throw new ArgumentException($"SubClassification '{request.SubClassification}' is not valid for department '{request.DepartmentCode}' / classification '{request.Classification}'.");

            if (!await _repository.VendorExistsAsync(request.VendorId))
                throw new ArgumentException($"VendorId {request.VendorId} does not exist or is not Approved.");

            if (!await _repository.VendorAddressExistsAsync(request.VendorAddressId, request.VendorId))
                throw new ArgumentException($"VendorAddressId {request.VendorAddressId} does not exist for VendorId {request.VendorId}.");

            if (!await _repository.DepartmentExistsAsync(request.DepartmentCode))
                throw new ArgumentException($"DepartmentCode '{request.DepartmentCode}' does not exist or is inactive.");

            foreach (var line in request.Items)
            {
                if (!await _repository.BudgetCategoryExistsAsync(line.BudgetCategoryId, request.DepartmentCode))
                    throw new ArgumentException($"BudgetCategoryId {line.BudgetCategoryId} does not exist for department '{request.DepartmentCode}' (item: '{line.ItemDescription}').");
            }

            if (!string.Equals(request.POType, "Standard", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.POType, "Blanket", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("POType must be either 'Standard' or 'Blanket'.");
            }

            if (!string.Equals(request.TermsType, "Normal", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.TermsType, "Advance", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("TermsType must be either 'Normal' or 'Advance'.");
            }

            if (string.Equals(request.TermsType, "Advance", StringComparison.OrdinalIgnoreCase))
            {
                if (request.AdvancePercentage is null || request.AdvancePercentage <= 0 || request.AdvancePercentage > 100)
                    throw new ArgumentException("AdvancePercentage is required and must be between 0 and 100 when TermsType is 'Advance'.");
            }
            else if (request.AdvancePercentage is not null)
            {
                throw new ArgumentException("AdvancePercentage should not be provided when TermsType is 'Normal'.");
            }

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

            if (request.DeliveryLocationIds is null || request.DeliveryLocationIds.Count == 0)
                throw new ArgumentException("At least one DeliveryLocationId is required.");

            if (!await _repository.AllLocationsExistAsync(request.DeliveryLocationIds))
                throw new ArgumentException("One or more DeliveryLocationIds do not exist or are inactive.");

            var selectedTerms = new List<Models.PoTermsConditionsMaster>();
            if (request.TermMasterIds is { Count: > 0 })
            {
                var allTerms = (await _repository.GetTermsMasterAsync()).ToList();
                var validIds = allTerms.Select(t => t.TermMasterId).ToHashSet();
                var invalidIds = request.TermMasterIds.Where(id => !validIds.Contains(id)).ToList();

                if (invalidIds.Count > 0)
                    throw new ArgumentException($"The following TermMasterId(s) do not exist or are inactive: {string.Join(", ", invalidIds)}.");

                selectedTerms = allTerms.Where(t => request.TermMasterIds.Contains(t.TermMasterId)).ToList();
            }

            // ==================== BUILD ITEMS (GST calc — rate only, no split) ====================

            var items = new List<Models.PoPurchaseOrderItem>();
            decimal subTotal = 0m, gstTotal = 0m;

            foreach (var line in request.Items)
            {
                decimal amount = line.Quantity * line.UnitPrice;
                decimal gstAmount = Math.Round(amount * line.GSTRate / 100m, 2);

                items.Add(new Models.PoPurchaseOrderItem
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

            // ==================== QUOTATION CHECK ====================

            bool quotationRequired = totalAmount > quotationThreshold;
            if (quotationRequired && (request.Quotations is null || request.Quotations.Count == 0))
                throw new InvalidOperationException($"Comparative quotations are mandatory for POs above Rs. {quotationThreshold:N0}.");


            // ==================== BUDGET CHECK — per item's category, rolled up to header ====================

            var categoryTotals = items
                .GroupBy(i => i.BudgetCategoryId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount + i.GSTAmount));

            var allocatedByCategory = await _repository.GetBudgetAmountsAsync(categoryTotals.Keys, request.BudgetYear);
            var consumedByCategory = await _repository.GetConsumedBudgetsAsync(categoryTotals.Keys, request.BudgetYear);

            var exceededCategories = new HashSet<int>();
            foreach (var (categoryId, thisPoAmount) in categoryTotals)
            {
                bool hasAllocation = allocatedByCategory.TryGetValue(categoryId, out var allocated);
                decimal consumed = consumedByCategory.TryGetValue(categoryId, out var c) ? c : 0m;

                if (!hasAllocation || (consumed + thisPoAmount) > allocated)
                    exceededCategories.Add(categoryId);
            }

            foreach (var item in items)
                item.IsBudgetExceeded = exceededCategories.Contains(item.BudgetCategoryId);

            bool budgetExceeded = items.Any(i => i.IsBudgetExceeded);

            // ==================== PO NUMBER ====================

            var lastPoNumber = await _repository.GetLastPoNumberAsync(request.BudgetYear);
            int nextSeq = 1;
            if (!string.IsNullOrEmpty(lastPoNumber))
            {
                string prefix = $"PO-{request.BudgetYear}-";
                if (lastPoNumber.StartsWith(prefix) && int.TryParse(lastPoNumber.Substring(prefix.Length), out var seq))
                    nextSeq = seq + 1;
            }
            string poNumber = $"PO-{request.BudgetYear}-{nextSeq:D5}";

            // ==================== BUILD & INSERT HEADER ====================

            var po = new Models.PoPurchaseOrder
            {
                PONumber = poNumber,
                VendorId = request.VendorId,
                VendorAddressId = request.VendorAddressId,
                DepartmentCode = request.DepartmentCode,
                Classification = request.Classification,
                SubClassification = request.SubClassification,
                BudgetYear = request.BudgetYear,
                POType = request.POType,
                TermsType = request.TermsType,
                AdvancePercentage = request.AdvancePercentage,
                VendorContactName = request.VendorContactName,
                VendorContactMobile = request.VendorContactMobile,
                VendorContactEmail = request.VendorContactEmail,
                SubTotal = subTotal,
                GSTAmount = gstTotal,
                TotalAmount = totalAmount,
                IsBudgetExceeded = budgetExceeded,
                QuotationRequired = quotationRequired,
                Status = request.SaveAsDraft ? "Draft" : "PendingApproval",
                CurrentApprovalStep = request.SaveAsDraft ? 0 : 1,
                CreatedBy = request.CreatedBy
            };

            int poId = await _repository.CreatePurchaseOrderAsync(po);

            // ==================== CHILD INSERTS ====================

            var itemsList = items.ToList();
            await _repository.AddPurchaseOrderItemsAsync(poId, itemsList);

            var savedQuotationPaths = SaveQuotationFiles(request.Quotations, request.BudgetYear, poId);
            await _repository.AddQuotationsAsync(poId, savedQuotationPaths);

            if (selectedTerms.Count > 0)
                await _repository.AddPurchaseOrderTermsAsync(poId, request.TermMasterIds, request.CreatedBy);

            var cleanedAdditionalTerms = (request.AdditionalTerms ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .ToList();

            if (cleanedAdditionalTerms.Count > 0)
                await _repository.AddAdditionalTermsAsync(poId, cleanedAdditionalTerms, request.CreatedBy);

            await _repository.AddPurchaseOrderDeliveryLocationsAsync(poId, request.DeliveryLocationIds, request.CreatedBy);

            if (!request.SaveAsDraft)
            {
                var roles = BuildApprovalChain(totalAmount, budgetExceeded, tier1Threshold, tier2Threshold);
                var steps = roles.Select((role, i) => new Models.PoPurchaseOrderApprovalHistory
                {
                    StepNumber = i + 1,
                    ApproverRole = role
                }).ToList();

                await _repository.AddApprovalStepsAsync(poId, steps);
            }

            // ==================== BUILD RESPONSE ====================

            var vendorDetails = await _repository.GetVendorDetailsForPOAsync(poId);
            var deliveryLocationDetails = await _repository.GetPurchaseOrderDeliveryLocationsAsync(poId);

            var response = new PurchaseOrderResponse
            {
                POId = poId,
                PONumber = poNumber,
                DepartmentCode = request.DepartmentCode,
                POType = request.POType,
                VendorName = vendorDetails?.VendorName ?? string.Empty,
                VendorCode = vendorDetails?.VendorCode ?? string.Empty,
                VendorGSTIN = vendorDetails?.GSTIN ?? string.Empty,
                VendorAddress = vendorDetails is not null ? $"{vendorDetails.AddressLine1}, {vendorDetails.City}" : string.Empty,
                VendorState = vendorDetails?.StateName ?? string.Empty,
                VendorContactName = request.VendorContactName,
                VendorContactMobile = request.VendorContactMobile,
                VendorContactEmail = request.VendorContactEmail,
                Classification = request.Classification,
                SubClassification = request.SubClassification,
                BudgetYear = request.BudgetYear,
                TermsType = request.TermsType,
                AdvancePercentage = request.AdvancePercentage,
                SubTotal = subTotal,
                GSTAmount = gstTotal,
                TotalAmount = totalAmount,
                IsBudgetExceeded = budgetExceeded,
                QuotationRequired = quotationRequired,
                Status = po.Status,
                PendingWithRole = request.SaveAsDraft ? null : "DepartmentHead",
                PendingWithUsers = new List<PendingApproverResponse>(),   // resolved on GetById; empty on create response
                Items = itemsList.Select(i => new PurchaseOrderItemResponse
                {
                    ItemDescription = i.ItemDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount,
                    GSTRate = i.GSTRate,
                    GSTAmount = i.GSTAmount,
                    BudgetCategoryId = i.BudgetCategoryId,
                    IsBudgetExceeded = i.IsBudgetExceeded
                }).ToList(),
                Terms = selectedTerms.Select(t => new PurchaseOrderTermResponse
                {
                    TermMasterId = t.TermMasterId,
                    TermType = t.TermType,
                    TermCategory = t.TermCategory,
                    TermsAndConditionDetails = t.TermsAndConditionDetails
                }).ToList(),
                AdditionalTerms = cleanedAdditionalTerms.Select(text => new PurchaseOrderAdditionalTermResponse
                {
                    POAdditionalTermId = 0,
                    TermText = text
                }).ToList(),
                Quotations = LoadQuotationFiles(savedQuotationPaths),
                ApprovalTrail = request.SaveAsDraft
        ? new List<ApprovalStepSummary>()
        : BuildApprovalChain(totalAmount, budgetExceeded, tier1Threshold, tier2Threshold).Select((role, i) => new ApprovalStepSummary
        {
            StepNumber = i + 1,
            ApproverRole = role,
            Action = null,
            Remarks = null,
            ActionedDt = null
        }).ToList(),
                DeliveryLocations = deliveryLocationDetails.Select(l => new DeliveryLocationResponse
                {
                    LocationId = l.LocationId,
                    BranchCode = l.BranchCode,
                    BranchName = l.BranchName,
                    Address = l.Address,
                    Region = l.Region,
                    Zone = l.Zone,
                    MegaZone = l.MegaZone
                }).ToList()
            };
            // Generate and save the PDF now that the PO fully exists in the DB
            await GenerateAndSavePurchaseOrderPdfAsync(poId);
            return response;
        }

        // ---------------- helpers ----------------

        private List<string> BuildApprovalChain(decimal totalAmount, bool budgetExceeded, decimal tier1Threshold, decimal tier2Threshold)
        {
            List<string> chain;

            if (totalAmount < tier1Threshold)
                chain = new List<string> { "DepartmentHead", "FinancialController" };
            else if (totalAmount <= tier2Threshold)
                chain = new List<string> { "DepartmentHead", "FinancialController", "FinanceHead" };
            else
                chain = new List<string> { "DepartmentHead", "FinancialController", "FinanceHead", "ManagingDirector" };

            if (budgetExceeded && !chain.Contains("FinanceHead"))
                chain.Add("FinanceHead");

            return chain;
        }

        private List<string> SaveQuotationFiles(List<QuotationFileUpload> quotations, string budgetYear, int poId)
        {
            var savedPaths = new List<string>();
            if (quotations is null || quotations.Count == 0) return savedPaths;

            string yearFolder = Path.Combine(_filePath, "Quotations", budgetYear.Replace("/", "-"));
            Directory.CreateDirectory(yearFolder);

            int sequence = 1;
            foreach (var quotation in quotations)
            {
                if (string.IsNullOrWhiteSpace(quotation.FileName) || string.IsNullOrWhiteSpace(quotation.Base64Content))
                    throw new ArgumentException("Each quotation must include a FileName and Base64Content.");

                string extension = Path.GetExtension(quotation.FileName);
                if (!AllowedExtensions.Contains(extension))
                    throw new ArgumentException($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}.");

                byte[] fileBytes;
                try
                {
                    string cleanBase64 = quotation.Base64Content.Contains(",")
                        ? quotation.Base64Content.Substring(quotation.Base64Content.IndexOf(",") + 1)
                        : quotation.Base64Content;

                    fileBytes = Convert.FromBase64String(cleanBase64);
                }
                catch (FormatException)
                {
                    throw new ArgumentException($"File '{quotation.FileName}' contains invalid base64 content.");
                }

                if (fileBytes.LongLength > MaxFileSizeBytes)
                    throw new ArgumentException($"File '{quotation.FileName}' exceeds the 10 MB size limit.");

                string safeFileName = $"{poId}_Quotation{sequence}{extension}";
                string fullPath = Path.Combine(yearFolder, safeFileName);

                File.WriteAllBytes(fullPath, fileBytes);

                savedPaths.Add(fullPath);
                sequence++;
            }

            return savedPaths;
        }

        private List<QuotationFileResponse> LoadQuotationFiles(IEnumerable<string> filePaths)
        {
            var results = new List<QuotationFileResponse>();

            foreach (var path in filePaths)
            {
                if (!File.Exists(path)) continue;

                byte[] fileBytes = File.ReadAllBytes(path);
                results.Add(new QuotationFileResponse
                {
                    FileName = Path.GetFileName(path),
                    Base64Content = Convert.ToBase64String(fileBytes)
                });
            }

            return results;
        }
        public async Task<IEnumerable<BudgetCategoryResponse>> GetBudgetCategoriesAsync(string departmentCode, string? classification)
        {
            var categories = await _repository.GetBudgetCategoriesAsync(departmentCode, classification);

            return categories.Select(c => new BudgetCategoryResponse
            {
                BudgetCategoryId = c.BudgetCategoryId,
                DepartmentCode = c.DepartmentCode,
                Classification = c.Classification,
                SubClassification = c.SubClassification,
                ItemCategory = c.ItemCategory
            });
        }

        private static readonly HashSet<string> DepartmentScopedRoles = new(StringComparer.OrdinalIgnoreCase) { "DepartmentHead" };


        public async Task<PurchaseOrderResponse> GetPurchaseOrderByIdAsync(int poId)
        {
            var po = await _repository.GetPurchaseOrderByIdAsync(poId)
                ?? throw new KeyNotFoundException($"Purchase Order {poId} not found.");

            var itemsWithCategory = await _repository.GetPurchaseOrderItemsWithCategoryAsync(poId);
            var terms = await _repository.GetPurchaseOrderTermsAsync(poId);
            var additionalTerms = await _repository.GetAdditionalTermsAsync(poId);
            var quotationPaths = await _repository.GetQuotationFilePathsAsync(poId);
            var approvalHistory = (await _repository.GetApprovalHistoryAsync(poId)).ToList();
            var deliveryLocations = await _repository.GetPurchaseOrderDeliveryLocationsAsync(poId);
            var vendorDetails = await _repository.GetVendorDetailsForPOAsync(poId);
            var payments = (await _repository.GetPaymentsForPOAsync(poId)).ToList();
            decimal? advanceRequired = string.Equals(po.TermsType, "Advance", StringComparison.OrdinalIgnoreCase) && po.AdvancePercentage is not null
                ? Math.Round(po.TotalAmount * po.AdvancePercentage.Value / 100m, 2)
                : null;
            decimal paidSoFar = payments.Where(p => p.PaymentStatus == "Success").Sum(p => p.AmountPaid);

            var pendingStep = approvalHistory.FirstOrDefault(h => h.StepNumber == po.CurrentApprovalStep);
            string? pendingRole = po.Status == "PendingApproval" ? pendingStep?.ApproverRole : null;

            var pendingUsers = new List<PendingApproverResponse>();
            if (pendingRole is not null)
            {
                string? scopeDept = DepartmentScopedRoles.Contains(pendingRole) ? po.DepartmentCode : null;
                var users = await _repository.GetUsersByRoleAsync(pendingRole, scopeDept);
                pendingUsers = users.Select(u => new PendingApproverResponse
                {
                    EmployeeCode = u.EmployeeCode,
                    Name = u.Name
                }).ToList();
            }

            var response = new PurchaseOrderResponse
            {
                POId = po.POId,
                PONumber = po.PONumber,
                DepartmentCode = po.DepartmentCode,
                POType = po.POType,
                VendorName = vendorDetails?.VendorName ?? string.Empty,
                VendorCode = vendorDetails?.VendorCode ?? string.Empty,
                VendorGSTIN = vendorDetails?.GSTIN ?? string.Empty,
                VendorAddress = vendorDetails is not null ? $"{vendorDetails.AddressLine1}, {vendorDetails.City}" : string.Empty,
                VendorState = vendorDetails?.StateName ?? string.Empty,
                VendorContactName = po.VendorContactName,
                VendorContactMobile = po.VendorContactMobile,
                VendorContactEmail = po.VendorContactEmail,
                Classification = po.Classification,
                SubClassification = po.SubClassification,
                BudgetYear = po.BudgetYear,
                TermsType = po.TermsType,
                AdvancePercentage = po.AdvancePercentage,
                SubTotal = po.SubTotal,
                GSTAmount = po.GSTAmount,
                TotalAmount = po.TotalAmount,
                IsBudgetExceeded = po.IsBudgetExceeded,
                QuotationRequired = po.QuotationRequired,
                Status = po.Status,
                PendingWithRole = pendingRole,
                PendingWithUsers = pendingUsers,
                Items = itemsWithCategory.Select(i => new PurchaseOrderItemResponse    // <-- uses itemsWithCategory
                {
                    ItemDescription = i.ItemDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount,
                    GSTRate = i.GSTRate,
                    GSTAmount = i.GSTAmount,
                    BudgetCategoryId = i.BudgetCategoryId,
                    SubClassification = i.SubClassification ?? string.Empty,
                    ItemCategory = i.ItemCategory ?? string.Empty
                }).ToList(),
                Terms = terms.Select(t => new PurchaseOrderTermResponse
                {
                    TermMasterId = t.TermMasterId,
                    TermType = t.TermType,
                    TermCategory = t.TermCategory,
                    TermsAndConditionDetails = t.TermsAndConditionDetails
                }).ToList(),
                AdditionalTerms = additionalTerms.Select(a => new PurchaseOrderAdditionalTermResponse
                {
                    POAdditionalTermId = a.POAdditionalTermId,
                    TermText = a.TermText
                }).ToList(),
                Quotations = LoadQuotationFiles(quotationPaths),
                ApprovalTrail = approvalHistory.Select(h => new ApprovalStepSummary
                {
                    StepNumber = h.StepNumber,
                    ApproverRole = h.ApproverRole,
                    Action = h.Action,
                    Remarks = h.Remarks,
                    ActionedDt = h.ActionedDt
                }).ToList(),
                DeliveryLocations = deliveryLocations.Select(l => new DeliveryLocationResponse
                {
                    LocationId = l.LocationId,
                    BranchCode = l.BranchCode,
                    BranchName = l.BranchName,
                    Address = l.Address,
                    Region = l.Region,
                    Zone = l.Zone,
                    MegaZone = l.MegaZone
                }).ToList(),
                AdvanceRequiredAmount = advanceRequired,
                AmountPaidSoFar = paidSoFar,
                Payments = payments.Select(p => new POPaymentResponse
                {
                    PaymentId = p.PaymentId,
                    PaymentStatus = p.PaymentStatus,
                    FailReason = p.FailReason,
                    UTRNumber = p.UTRNumber,
                    PaymentDate = p.PaymentDate,
                    AmountPaid = p.AmountPaid,
                    TDSAmount = p.TDSAmount,
                    CreatedBy = p.CreatedBy,
                    CreatedDt = p.CreatedDt
                }).ToList()
            };
            response.PdfBase64 = LoadPurchaseOrderPdfBase64(poId, po.BudgetYear);
            return response;
        }
        private string? LoadPurchaseOrderPdfBase64(int poId, string budgetYear)
        {
            string pdfPath = Path.Combine(_filePath, "GeneratedPDFs", budgetYear, $"{poId}_PO.pdf");

            if (!File.Exists(pdfPath))
                return null;   // e.g. an old PO created before this auto-generation was wired in

            byte[] pdfBytes = File.ReadAllBytes(pdfPath);
            return Convert.ToBase64String(pdfBytes);
        }
        public async Task<IEnumerable<PurchaseOrderResponse>> GetPurchaseOrderListAsync(string? departmentCode, string? status)
        {
            var pos = await _repository.GetPurchaseOrderListAsync(departmentCode, status);

            return pos.Select(po => new PurchaseOrderResponse
            {
                POId = po.POId,
                PONumber = po.PONumber,
                DepartmentCode = po.DepartmentCode,
                Classification = po.Classification,
                BudgetYear = po.BudgetYear,
                SubTotal = po.SubTotal,
                GSTAmount = po.GSTAmount,
                TotalAmount = po.TotalAmount,
                IsBudgetExceeded = po.IsBudgetExceeded,
                QuotationRequired = po.QuotationRequired,
                Status = po.Status
            });
        }

        //private List<string> SaveQuotationFiles(List<QuotationFileUpload> quotations, string budgetYear, int poId)
        //{
        //    var savedPaths = new List<string>();
        //    if (quotations is null || quotations.Count == 0) return savedPaths;

        //    string yearFolder = Path.Combine(_filePath, "Quotations", budgetYear.Replace("/", "-"));
        //    Directory.CreateDirectory(yearFolder);

        //    int sequence = 1;
        //    foreach (var quotation in quotations)
        //    {
        //        if (string.IsNullOrWhiteSpace(quotation.FileName) || string.IsNullOrWhiteSpace(quotation.Base64Content))
        //            throw new ArgumentException("Each quotation must include a FileName and Base64Content.");

        //        string extension = Path.GetExtension(quotation.FileName);
        //        if (!AllowedExtensions.Contains(extension))
        //            throw new ArgumentException($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}.");

        //        byte[] fileBytes;
        //        try
        //        {
        //            string cleanBase64 = quotation.Base64Content.Contains(",")
        //                ? quotation.Base64Content.Substring(quotation.Base64Content.IndexOf(",") + 1)
        //                : quotation.Base64Content;

        //            fileBytes = Convert.FromBase64String(cleanBase64);
        //        }
        //        catch (FormatException)
        //        {
        //            throw new ArgumentException($"File '{quotation.FileName}' contains invalid base64 content.");
        //        }

        //        if (fileBytes.LongLength > MaxFileSizeBytes)
        //            throw new ArgumentException($"File '{quotation.FileName}' exceeds the 10 MB size limit.");

        //        // e.g. 6_Quotation1.pdf, 6_Quotation2.pdf
        //        string safeFileName = $"{poId}_Quotation{sequence}{extension}";
        //        string fullPath = Path.Combine(yearFolder, safeFileName);

        //        File.WriteAllBytes(fullPath, fileBytes);

        //        savedPaths.Add(fullPath);
        //        sequence++;
        //    }

        //    return savedPaths;
        //}
        //private List<QuotationFileResponse> LoadQuotationFiles(IEnumerable<string> filePaths)
        //{
        //    var results = new List<QuotationFileResponse>();

        //    foreach (var path in filePaths)
        //    {
        //        if (!File.Exists(path))
        //        {
        //            // File missing on disk (moved/deleted outside the app) — skip rather than fail the whole GET
        //            continue;
        //        }

        //        byte[] fileBytes = File.ReadAllBytes(path);

        //        results.Add(new QuotationFileResponse
        //        {
        //            FileName = Path.GetFileName(path),          // e.g. "6_Quotation1.pdf"
        //            Base64Content = Convert.ToBase64String(fileBytes)
        //        });
        //    }

        //    return results;
        //}



        public async Task<IEnumerable<PurchaseOrderListItemResponse>> GetPurchaseOrderListForUserAsync(string employeeCode)
        {
            var user = await _repository.GetActiveUserByEmployeeCodeAsync(employeeCode)
                ?? throw new ArgumentException($"User '{employeeCode}' not found or inactive.");

            var roles = (await _repository.GetActiveRoleNamesForUserAsync(user.Id)).ToList();

            if (roles.Count == 0)
                throw new InvalidOperationException($"User '{employeeCode}' has no active role mapped — cannot determine PO access.");

            IEnumerable<PoPurchaseOrder> purchaseOrders;

            if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            {
                purchaseOrders = await _repository.GetAllPurchaseOrdersAsync();
            }
            else
            {
                var combined = new Dictionary<int, PoPurchaseOrder>();

                var matchedApproverRoles = roles.Where(r => ApproverRoles.Contains(r)).ToList();
                foreach (var role in matchedApproverRoles)
                {
                    var pending = await _repository.GetPurchaseOrdersPendingApprovalForRoleAsync(role);
                    foreach (var po in pending) combined[po.POId] = po;
                }

                bool hasNonApproverRole = roles.Any(r => !ApproverRoles.Contains(r, StringComparer.OrdinalIgnoreCase)
                                                          && !string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase));
                if (hasNonApproverRole)
                {
                    var deptPOs = await _repository.GetPurchaseOrdersByDepartmentAsync(user.DepartmentCode);
                    foreach (var po in deptPOs) combined[po.POId] = po;
                }

                purchaseOrders = combined.Values.OrderByDescending(po => po.CreatedDt);
            }

            var poList = purchaseOrders.ToList();

            // Single batched query for approval history across every PO in the list — avoids N+1 queries
            var allHistory = (await _repository.GetApprovalHistoryForPOsAsync(poList.Select(p => p.POId)))
                .GroupBy(h => h.POId)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.StepNumber).ToList());

            return poList.Select(po =>
            {
                allHistory.TryGetValue(po.POId, out var history);
                history ??= new List<PoPurchaseOrderApprovalHistory>();

                var pendingStep = history.FirstOrDefault(h => h.StepNumber == po.CurrentApprovalStep);

                return new PurchaseOrderListItemResponse
                {
                    POId = po.POId,
                    PONumber = po.PONumber,
                    DepartmentCode = po.DepartmentCode,
                    Classification = po.Classification,
                    BudgetYear = po.BudgetYear,
                    TotalAmount = po.TotalAmount,
                    Status = po.Status,
                    IsBudgetExceeded = po.IsBudgetExceeded,
                    CreatedBy = po.CreatedBy,
                    CreatedDt = po.CreatedDt,
                    PendingWithRole = po.Status == "PendingApproval" ? pendingStep?.ApproverRole : null,
                    ApprovalTrail = history.Select(h => new ApprovalStepSummary
                    {
                        StepNumber = h.StepNumber,
                        ApproverRole = h.ApproverRole,
                        Action = h.Action,
                        Remarks = h.Remarks,
                        ActionedDt = h.ActionedDt
                    }).ToList()
                };
            });
        }
        private static readonly HashSet<string> ApproverRoles = new(StringComparer.OrdinalIgnoreCase)
    { "DepartmentHead", "FinancialController", "FinanceHead", "CFO", "ManagingDirector" };

        public async Task<PurchaseOrderDashboardResponse> GetPurchaseOrderDashboardAsync(string employeeCode)
        {
            var user = await _repository.GetActiveUserByEmployeeCodeAsync(employeeCode)
                ?? throw new ArgumentException($"User '{employeeCode}' not found or inactive.");

            var roles = (await _repository.GetActiveRoleNamesForUserAsync(user.Id)).ToList();

            if (roles.Count == 0)
                throw new InvalidOperationException($"User '{employeeCode}' has no active role mapped — cannot determine PO access.");

            bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            var myApproverRoles = roles.Where(r => ApproverRoles.Contains(r)).ToList();

            // ---------------- Pending With Me ----------------
            List<PoPurchaseOrder> pendingWithMe = new();
            if (isAdmin)
            {
                // Admins generally aren't in the approval chain themselves; leave empty unless "Admin" ever appears as an ApproverRole.
            }
            else if (myApproverRoles.Count > 0)
            {
                pendingWithMe = (await _repository.GetPendingWithRolesAsync(myApproverRoles, user.DepartmentCode)).ToList();
            }

            var pendingWithMeIds = pendingWithMe.Select(p => p.POId).ToHashSet();

            // ---------------- In Progress ----------------
            var inProgressRaw = isAdmin
                ? await _repository.GetAllPendingAsync()
                : await _repository.GetInProgressByDepartmentAsync(user.DepartmentCode);

            // NEW: also pull in POs (any department) this user personally approved a step on, still pending downstream
            var actedByMe = isAdmin
                ? Enumerable.Empty<PoPurchaseOrder>()
                : await _repository.GetInProgressActedByUserAsync(employeeCode);

            var inProgress = inProgressRaw
                .Concat(actedByMe)
                .Where(p => !pendingWithMeIds.Contains(p.POId))
                .DistinctBy(p => p.POId)
                .ToList();

            // ---------------- Completed ----------------
            var completed = isAdmin
                ? (await _repository.GetAllCompletedAsync()).ToList()
                : (await _repository.GetCompletedByDepartmentAsync(user.DepartmentCode)).ToList();
            // ---------------- Batched "who's it pending with" lookup — ONE call for everything pending across all three buckets ----------------
            var allPendingPOs = pendingWithMe.Concat(inProgress).ToList();   // Completed POs are never "pending", so excluded
            var pendingRoleMap = await GetPendingRolesForPOsAsync(allPendingPOs);

            return new PurchaseOrderDashboardResponse
            {
                PendingWithMe = MapToListResponse(pendingWithMe, pendingRoleMap).ToList(),
                InProgress = MapToListResponse(inProgress, pendingRoleMap).ToList(),
                Completed = MapToListResponse(completed, pendingRoleMap).ToList()
            };
        }

        private static IEnumerable<PurchaseOrderListItemResponse> MapToListResponse(
      IEnumerable<PoPurchaseOrder> purchaseOrders, Dictionary<int, string?> pendingRoleMap)
      => purchaseOrders.Select(po => new PurchaseOrderListItemResponse
      {
          POId = po.POId,
          PONumber = po.PONumber,
          DepartmentCode = po.DepartmentCode,
          Classification = po.Classification,
          BudgetYear = po.BudgetYear,
          TotalAmount = po.TotalAmount,
          Status = po.Status,
          IsBudgetExceeded = po.IsBudgetExceeded,
          CreatedBy = po.CreatedBy,
          CreatedDt = po.CreatedDt,
          PendingWithRole = pendingRoleMap.TryGetValue(po.POId, out var role) ? role : null
      });
        private async Task<Dictionary<int, string?>> GetPendingRolesForPOsAsync(IEnumerable<PoPurchaseOrder> pos)
        {
            var poIds = pos.Where(p => p.Status == "PendingApproval").Select(p => p.POId).ToList();
            if (poIds.Count == 0) return new Dictionary<int, string?>();

            var history = await _repository.GetApprovalHistoryForPOsAsync(poIds);
            return pos.Where(p => p.Status == "PendingApproval").ToDictionary(
                p => p.POId,
                p => history.FirstOrDefault(h => h.POId == p.POId && h.StepNumber == p.CurrentApprovalStep)?.ApproverRole
            );
        }
        public async Task<IEnumerable<DeliveryLocationResponse>> GetLocationsAsync(string? region, string? zone)
        {
            var locations = await _repository.GetLocationsAsync(region, zone);
            return locations.Select(l => new DeliveryLocationResponse
            {
                LocationId = l.LocationId,
                BranchCode = l.BranchCode,
                BranchName = l.BranchName,
                Address = l.Address,
                Region = l.Region,
                Zone = l.Zone,
                MegaZone = l.MegaZone
            });
        }



        public async Task ActOnApprovalAsync(ApprovalActionRequest request)
        {
            var po = await _repository.GetPurchaseOrderByIdAsync(request.POId)
                ?? throw new KeyNotFoundException($"Purchase Order {request.POId} not found.");

            if (po.Status != "PendingApproval")
                throw new InvalidOperationException($"This Purchase Order is not currently awaiting approval (current status: {po.Status}).");

            var currentStep = await _repository.GetPendingApprovalStepAsync(po.POId, po.CurrentApprovalStep)
                ?? throw new InvalidOperationException("No pending approval step was found for this Purchase Order.");

            if (currentStep.Action is not null)
                throw new InvalidOperationException("This approval step has already been actioned.");

            // ---------------- Authorization: caller must hold the exact role pending at this step ----------------

            var approver = await _repository.GetActiveUserByEmployeeCodeAsync(request.ApproverEmployeeCode)
                ?? throw new ArgumentException($"Approver '{request.ApproverEmployeeCode}' not found or inactive.");

            var approverRoles = (await _repository.GetActiveRoleNamesForUserAsync(approver.Id)).ToList();

            bool holdsRequiredRole = approverRoles.Contains(currentStep.ApproverRole, StringComparer.OrdinalIgnoreCase);
            if (!holdsRequiredRole)
                throw new InvalidOperationException(
                    $"'{request.ApproverEmployeeCode}' does not hold the '{currentStep.ApproverRole}' role required for this step.");

            // DepartmentHead approval must additionally be scoped to the PO's own department
            if (DepartmentScopedRoles.Contains(currentStep.ApproverRole) &&
                !string.Equals(approver.DepartmentCode, po.DepartmentCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"'{request.ApproverEmployeeCode}' is a DepartmentHead for '{approver.DepartmentCode}', not '{po.DepartmentCode}'.");
            }

            // ---------------- Record the action ----------------

            await _repository.MarkApprovalStepActionedAsync(
                currentStep.ApprovalId, request.ApproverEmployeeCode, request.IsApproved ? "Approved" : "Rejected", request.Remarks);

            if (!request.IsApproved)
            {
                
                await _repository.UpdatePOStatusAsync(po.POId, "Rejected", 0);
                await GenerateAndSavePurchaseOrderPdfAsync(po.POId); 
                return;
            }

            // Approved -> check if there's a next step in the chain
            var allSteps = await _repository.GetApprovalHistoryAsync(po.POId);
            bool hasNextStep = allSteps.Any(s => s.StepNumber == po.CurrentApprovalStep + 1);

            if (hasNextStep)
            {
                await _repository.UpdatePOStatusAsync(po.POId, "PendingApproval", po.CurrentApprovalStep + 1);
            }
            else
            {
                // Final step cleared. Advance-payment POs route to Finance User instead of going straight to Approved.
                bool isAdvancePO = string.Equals(po.TermsType, "Advance", StringComparison.OrdinalIgnoreCase)
                                    && po.AdvancePercentage is > 0;

                string finalStatus = isAdvancePO ? "PendingPayment" : "Approved";
                await _repository.UpdatePOStatusAsync(po.POId, finalStatus, po.CurrentApprovalStep);
                await GenerateAndSavePurchaseOrderPdfAsync(po.POId);
            }
        }
        public async Task<IEnumerable<AppSettingResponse>> GetAllSettingsAsync()
        {
            var settings = await _repository.GetAllSettingsAsync();
            return settings.Select(s => new AppSettingResponse
            {
                SettingKey = s.SettingKey,
                SettingValue = s.SettingValue,
                Description = s.Description
            });
        }

        public Task UpdateSettingAsync(UpdateAppSettingRequest request)
            => _repository.UpdateSettingAsync(request.SettingKey, request.SettingValue, request.ModifiedBy);
        public Task<IEnumerable<string>> GetSubClassificationsAsync(string departmentCode, string classification)
    => _repository.GetSubClassificationsAsync(departmentCode, classification);

        public async Task<DepartmentBudgetUtilizationResponse> GetDepartmentBudgetUtilizationAsync(string departmentCode, string budgetYear)
        {
            if (!await _repository.DepartmentExistsAsync(departmentCode))
                throw new ArgumentException($"DepartmentCode '{departmentCode}' does not exist or is inactive.");

            var rows = (await _repository.GetDepartmentBudgetUtilizationAsync(departmentCode, budgetYear)).ToList();

            var classifications = rows
                .GroupBy(r => (string)r.Classification)
                .Select(group =>
                {
                    var categories = group.Select(r =>
                    {
                        decimal allocated = r.AllocatedAmount;
                        decimal used = r.UsedAmount;
                        decimal remaining = allocated - used;
                        decimal usedPct = allocated > 0 ? Math.Round((used / allocated) * 100m, 2) : 0m;

                        return new BudgetUtilizationResponse
                        {
                            BudgetCategoryId = r.BudgetCategoryId,
                            SubClassification = r.SubClassification,
                            ItemCategory = r.ItemCategory,
                            AllocatedAmount = allocated,
                            UsedAmount = used,
                            RemainingAmount = remaining,
                            UsedPercentage = usedPct
                        };
                    }).ToList();

                    decimal classAllocated = categories.Sum(c => c.AllocatedAmount);
                    decimal classUsed = categories.Sum(c => c.UsedAmount);

                    return new ClassificationBudgetUtilizationResponse
                    {
                        Classification = group.Key,
                        TotalAllocated = classAllocated,
                        TotalUsed = classUsed,
                        TotalRemaining = classAllocated - classUsed,
                        UsedPercentage = classAllocated > 0 ? Math.Round((classUsed / classAllocated) * 100m, 2) : 0m,
                        Categories = categories
                    };
                })
                .OrderBy(c => c.Classification)
                .ToList();

            decimal totalAllocated = classifications.Sum(c => c.TotalAllocated);
            decimal totalUsed = classifications.Sum(c => c.TotalUsed);

            return new DepartmentBudgetUtilizationResponse
            {
                DepartmentCode = departmentCode,
                BudgetYear = budgetYear,
                TotalAllocated = totalAllocated,
                TotalUsed = totalUsed,
                TotalRemaining = totalAllocated - totalUsed,
                OverallUsedPercentage = totalAllocated > 0 ? Math.Round((totalUsed / totalAllocated) * 100m, 2) : 0m,
                Classifications = classifications
            };
        }
        public async Task RecordPaymentAsync(RecordPaymentRequest request)
        {
            var po = await _repository.GetPurchaseOrderByIdAsync(request.POId)
                ?? throw new KeyNotFoundException($"Purchase Order {request.POId} not found.");

            if (po.Status != "PendingPayment")
                throw new InvalidOperationException($"This Purchase Order is not awaiting payment (current status: {po.Status}).");
            // ---------------- Authorization: caller must hold FinanceUser role ----------------
            var user = await _repository.GetActiveUserByEmployeeCodeAsync(request.CreatedBy)
                ?? throw new ArgumentException($"User '{request.CreatedBy}' not found or inactive.");

            var roles = (await _repository.GetActiveRoleNamesForUserAsync(user.Id)).ToList();

            if (!roles.Contains("FinanceUser", StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"'{request.CreatedBy}' does not hold the 'FinanceUser' role required to record payments.");

            // ----------------  validation  ----------------

            if (!string.Equals(request.PaymentStatus, "Success", StringComparison.OrdinalIgnoreCase) &&
     !string.Equals(request.PaymentStatus, "Failed", StringComparison.OrdinalIgnoreCase) &&
     !string.Equals(request.PaymentStatus, "Hold", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("PaymentStatus must be 'Success', 'Failed', or 'Hold'.");
            }

            if (string.Equals(request.PaymentStatus, "Failed", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(request.FailReason))
            {
                throw new ArgumentException("FailReason is required when PaymentStatus is 'Failed'.");
            }
            if (string.Equals(request.PaymentStatus, "Hold", StringComparison.OrdinalIgnoreCase) &&
    string.IsNullOrWhiteSpace(request.FailReason))
            {
                throw new ArgumentException("A reason is required (in FailReason) when PaymentStatus is 'Hold'.");
            }

            decimal advanceRequired = Math.Round(po.TotalAmount * (po.AdvancePercentage ?? 0) / 100m, 2);
            decimal alreadyPaid = await _repository.GetTotalSuccessfulPaymentsAsync(po.POId);

            if (string.Equals(request.PaymentStatus, "Success", StringComparison.OrdinalIgnoreCase) &&
                alreadyPaid + request.AmountPaid > advanceRequired)
            {
                throw new InvalidOperationException(
                    $"This payment would exceed the required advance amount. Advance required: {advanceRequired:N2}, already paid: {alreadyPaid:N2}, attempted: {request.AmountPaid:N2}.");
            }

            await _repository.AddPaymentAsync(new Models.PoPurchaseOrderPayment
            {
                POId = request.POId,
                PaymentStatus = request.PaymentStatus,
                FailReason = request.FailReason,
                UTRNumber = request.UTRNumber,
                PaymentDate = request.PaymentDate,
                AmountPaid = request.AmountPaid,
                TDSAmount = request.TDSAmount,
                CreatedBy = request.CreatedBy
            });

            if (string.Equals(request.PaymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
            {
                decimal totalPaidNow = alreadyPaid + request.AmountPaid;
                if (totalPaidNow >= advanceRequired)
                {
                    await _repository.UpdatePOStatusAsync(po.POId, "Approved", po.CurrentApprovalStep);
                }
                // else: stays in PendingPayment, awaiting the remaining installment(s)
            }
        }

        public async Task<IEnumerable<PendingPaymentPOResponse>> GetPendingPaymentPOsAsync()
        {
            var rows = (await _repository.GetPendingPaymentPOsAsync()).ToList();
            var result = new List<PendingPaymentPOResponse>();

            foreach (var row in rows)
            {
                decimal advanceRequired = Math.Round((decimal)row.TotalAmount * (decimal)(row.AdvancePercentage ?? 0) / 100m, 2);
                decimal paidSoFar = await _repository.GetTotalSuccessfulPaymentsAsync((int)row.POId);

                result.Add(new PendingPaymentPOResponse
                {
                    POId = row.POId,
                    PONumber = row.PONumber,
                    VendorName = row.VendorName,
                    TotalAmount = row.TotalAmount,
                    AdvanceRequiredAmount = advanceRequired,
                    AmountPaidSoFar = paidSoFar,
                    AmountRemaining = advanceRequired - paidSoFar
                });
            }

            return result;
        }


        public async Task<POVendorDetailResponse> CreateVendorAsync(CreateVendorRequest request)
        {
            if (await _repository.VendorPANExistsAsync(request.VendorPAN))
                throw new ArgumentException($"A vendor with PAN '{request.VendorPAN}' is already Pending or Active.");

            if (request.PANDocument is null)
                throw new ArgumentException("PAN document is required.");

            if (request.BankAccountProofDocument is null)
                throw new ArgumentException("Bank account proof document is required.");

            if (request.MSMEVendor && request.MSMEDocument is null)
                throw new ArgumentException("MSME document is required when MSMEVendor is true.");

            // Use a temp folder key since VendorCode doesn't exist yet — VendorPAN is unique, safe to use here
            string vendorFolder = Path.Combine(_filePath, "VendorDocuments", request.VendorPAN);
            Directory.CreateDirectory(vendorFolder);

            string panPath = SaveVendorDocument(request.PANDocument, vendorFolder, "PAN");
            string bankProofPath = SaveVendorDocument(request.BankAccountProofDocument, vendorFolder, "BankProof");
            string? msmePath = request.MSMEVendor ? SaveVendorDocument(request.MSMEDocument!, vendorFolder, "MSME") : null;

            var vendor = new Models.POVendorMaster
            {
                VendorName = request.VendorName,
                BankHolderName = request.BankHolderName,
                BankAccountNo = request.BankAccountNo,
                BankName = request.BankName,
                IFSCCode = request.IFSCCode,
                MSMEVendor = request.MSMEVendor,
                UDYAMRegnNo = request.UDYAMRegnNo,
                VendorPAN = request.VendorPAN,
                PANDocumentPath = panPath,
                MSMEDocumentPath = msmePath,
                BankAccountProofPath = bankProofPath,
                CreatedBy = request.CreatedBy
            };

            long vendorId = await _repository.CreateVendorAsync(vendor);

            var (createdVendor, createdAddresses) = await _repository.GetVendorByIdAsync(vendorId);
            if (createdVendor is null)
                throw new InvalidOperationException("Vendor was created but could not be reloaded.");

            return MapToVendorDetailResponse(createdVendor, createdAddresses);
        }

        public async Task ApproveVendorAsync(ApproveVendorRequest request)
        {
            var (vendor, _) = await _repository.GetVendorByIdAsync(request.VendorId);
            if (vendor is null)
                throw new KeyNotFoundException($"Vendor {request.VendorId} not found.");

            if (vendor.Status != "PENDING")
                throw new InvalidOperationException($"This vendor is not pending approval (current status: {vendor.Status}).");

            bool isNewVendor = string.IsNullOrWhiteSpace(vendor.VendorCode);

            if (isNewVendor)
            {
                // First-time approval: Finance User must assign a VendorCode
                if (string.IsNullOrWhiteSpace(request.VendorCode))
                    throw new ArgumentException("VendorCode is required to approve a new vendor.");

                if (await _repository.VendorCodeExistsAsync(request.VendorCode))
                    throw new ArgumentException($"VendorCode '{request.VendorCode}' is already in use.");

                await _repository.ApproveNewVendorAsync(request.VendorId, request.VendorCode, request.ApprovedBy);
            }
            else
            {
                // Re-approval of an edited existing vendor: code stays as-is, just re-activate
                if (!string.IsNullOrWhiteSpace(request.VendorCode) &&
                    !string.Equals(request.VendorCode, vendor.VendorCode, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException($"This vendor already has VendorCode '{vendor.VendorCode}' — it cannot be changed during a details-only approval.");
                }

                await _repository.ApproveExistingVendorAsync(request.VendorId, request.ApprovedBy);
            }
        }
        public async Task RejectVendorAsync(RejectVendorRequest request)
        {
            var (vendor, _) = await _repository.GetVendorByIdAsync(request.VendorId);
            if (vendor is null)
                throw new KeyNotFoundException($"Vendor {request.VendorId} not found.");

            if (vendor.Status != "PENDING")
                throw new InvalidOperationException($"This vendor is not pending approval (current status: {vendor.Status}).");

            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                throw new ArgumentException("RejectionReason is required.");

            await _repository.RejectVendorAsync(request.VendorId, request.RejectionReason, request.RejectedBy);
        }

        public async Task<IEnumerable<POVendorDetailResponse>> GetPendingVendorsAsync()
        {
            var vendors = await _repository.GetPendingVendorsAsync();
            var result = new List<POVendorDetailResponse>();

            foreach (var v in vendors)
            {
                var (_, addresses) = await _repository.GetVendorByIdAsync(v.VendorId);
                result.Add(MapToVendorDetailResponse(v, addresses));
            }

            return result;
        }

        private POVendorDetailResponse MapToVendorDetailResponse(POVendorMaster v, IEnumerable<POVendorAddressMaster> addresses) => new()
        {
            VendorId = v.VendorId,
            VendorCode = v.VendorCode,
            VendorName = v.VendorName,
            BankHolderName = v.BankHolderName,
            BankAccountNo = v.BankAccountNo,
            BankName = v.BankName,
            IFSCCode = v.IFSCCode,
            MSMEVendor = v.MSMEVendor,
            UDYAMRegnNo = v.UDYAMRegnNo,
            VendorPAN = v.VendorPAN,
            Status = v.Status,
            RejectionReason = v.RejectionReason,
            Addresses = addresses.Select(a => new POVendorAddressResponse
            {
                VendorAddressId = a.VendorAddressId,
                AddressLine1 = a.AddressLine1,
                City = a.City,
                StateName = a.StateName,
                StateCode = a.StateCode,
                PinCode = a.PinCode,
                GSTIN = a.GSTIN,
                IsPrimaryAddress = a.IsPrimaryAddress,
                Status = a.Status
            }).ToList(),
            PANDocument = LoadVendorDocument(v.PANDocumentPath),
            MSMEDocument = LoadVendorDocument(v.MSMEDocumentPath),
            BankAccountProofDocument = LoadVendorDocument(v.BankAccountProofPath)
        };
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
        private static VendorDocumentResponse? LoadDocument(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

            return new VendorDocumentResponse
            {
                FileName = Path.GetFileName(path),
                Base64Content = Convert.ToBase64String(File.ReadAllBytes(path))
            };
        }
        private static POVendorResponse MapToResponse(Models.POVendorMaster v) => new()
        {
            VendorId = v.VendorId,
            VendorCode = v.VendorCode,
            VendorName = v.VendorName,
            MSMEVendor = v.MSMEVendor,
            Status = v.Status,
            PANDocument = LoadDocument(v.PANDocumentPath),
            MSMEDocument = LoadDocument(v.MSMEDocumentPath),
            BankAccountProofDocument = LoadDocument(v.BankAccountProofPath)
        };

        public async Task<POVendorDetailResponse> UpdateVendorAsync(UpdateVendorRequest request)
        {
            var existing = await _repository.GetVendorForUpdateAsync(request.VendorId)
                ?? throw new KeyNotFoundException($"Vendor {request.VendorId} not found.");

            // Editable from either state: REJECTED (fixing a rejection) or ACTIVE (updating a live vendor's details)
            if (existing.Status != "REJECTED" && existing.Status != "ACTIVE")
                throw new InvalidOperationException($"This vendor cannot be edited from its current status ({existing.Status}).");

            if (!string.Equals(existing.VendorPAN, request.VendorPAN, StringComparison.OrdinalIgnoreCase))
            {
                if (await _repository.VendorPANExistsAsync(request.VendorPAN))
                    throw new ArgumentException($"A vendor with PAN '{request.VendorPAN}' is already Pending or Active.");
            }

            if (request.MSMEVendor && request.MSMEDocument is null && existing.MSMEDocumentPath is null)
                throw new ArgumentException("MSME document is required when MSMEVendor is true.");

            string vendorFolder = Path.Combine(_filePath, "VendorDocuments", request.VendorPAN);
            Directory.CreateDirectory(vendorFolder);

            string panPath = request.PANDocument is not null
                ? SaveVendorDocument(request.PANDocument, vendorFolder, "PAN")
                : existing.PANDocumentPath ?? throw new ArgumentException("PAN document is required.");

            string bankProofPath = request.BankAccountProofDocument is not null
                ? SaveVendorDocument(request.BankAccountProofDocument, vendorFolder, "BankProof")
                : existing.BankAccountProofPath ?? throw new ArgumentException("Bank account proof document is required.");

            string? msmePath = request.MSMEVendor
                ? (request.MSMEDocument is not null
                    ? SaveVendorDocument(request.MSMEDocument, vendorFolder, "MSME")
                    : existing.MSMEDocumentPath)
                : null;

            var vendor = new Models.POVendorMaster
            {
                VendorId = request.VendorId,
                VendorName = request.VendorName,
                BankHolderName = request.BankHolderName,
                BankAccountNo = request.BankAccountNo,
                BankName = request.BankName,
                IFSCCode = request.IFSCCode,
                MSMEVendor = request.MSMEVendor,
                UDYAMRegnNo = request.UDYAMRegnNo,
                VendorPAN = request.VendorPAN,
                PANDocumentPath = panPath,
                MSMEDocumentPath = msmePath,
                BankAccountProofPath = bankProofPath,
                ModifiedBy = request.UpdatedBy
            };

            // Sets Status back to PENDING regardless of whether it came from REJECTED or ACTIVE —
            // VendorCode (if any) is preserved by the UPDATE statement, since it's never touched here.
            await _repository.UpdateVendorAsync(vendor);

            var (updated, addresses) = await _repository.GetVendorByIdAsync(request.VendorId);
            if (updated is null)
                throw new InvalidOperationException("Vendor was updated but could not be reloaded.");

            return MapToVendorDetailResponse(updated, addresses);
        }
        public async Task<IEnumerable<POVendorDetailResponse>> GetMyRejectedVendorsAsync(string preparerEmployeeCode)
        {
            var vendors = await _repository.GetRejectedVendorsByPreparerAsync(preparerEmployeeCode);
            var result = new List<POVendorDetailResponse>();

            foreach (var v in vendors)
            {
                var (_, addresses) = await _repository.GetVendorByIdAsync(v.VendorId);
                result.Add(MapToVendorDetailResponse(v, addresses));
            }

            return result;
        }

        
        //public async Task<byte[]> GeneratePurchaseOrderPdfAsync(int poId)
        //{
        //    var po = await GetPurchaseOrderByIdAsync(poId);
        //    string html = BuildHtml(po);

        //    var browserFetcher = new BrowserFetcher();
        //    var installedBrowser = await browserFetcher.DownloadAsync();

        //    await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        //    {
        //        Headless = true,
        //        ExecutablePath = installedBrowser.GetExecutablePath(),
        //        Args = new[] { "--no-sandbox", "--disable-gpu" }
        //    });

        //    await using var page = await browser.NewPageAsync();
        //    await page.SetContentAsync(html, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });

        //    var pdfBytes = await page.PdfDataAsync(new PdfOptions
        //    {
        //        Format = PaperFormat.A4,
        //        PrintBackground = true,
        //        MarginOptions = new MarginOptions { Top = "0", Bottom = "0", Left = "0", Right = "0" }
        //    });

        //    // ---------------- Save to disk before returning ----------------
        //    string folder = Path.Combine(_filePath, "GeneratedPDFs", po.BudgetYear);
        //    Directory.CreateDirectory(folder);

        //    string fileName = $"{poId}_PO.pdf";
        //    string fullPath = Path.Combine(folder, fileName);
        //    await File.WriteAllBytesAsync(fullPath, pdfBytes);

        //    return pdfBytes;
        //}
        private string BuildHtml(PurchaseOrderResponse po)
        {
            string template = File.ReadAllText(_templatePath);

            string logoBase64 = "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(_logoPath));

            var itemRows = new StringBuilder();
            int i = 1;
            foreach (var item in po.Items)
            {
                itemRows.Append($@"
<tr>
    <td>{i}</td>
    <td><strong>{System.Net.WebUtility.HtmlEncode(item.ItemDescription)}</strong>
        <small>Sub-category: {System.Net.WebUtility.HtmlEncode(item.SubClassification)} &nbsp;·&nbsp; Item category: {System.Net.WebUtility.HtmlEncode(item.ItemCategory)}{(item.IsBudgetExceeded ? " &nbsp;·&nbsp; <span style='color:#A12531'>Budget Exceeded</span>" : "")}</small>
    </td>
    <td class=""center"">{item.Quantity:N0}</td>
    <td class=""right"">₹{item.UnitPrice:N0}</td>
    <td class=""right"">₹{item.Amount:N0}</td>
    <td class=""right"">₹{item.GSTAmount:N0}</td>
    <td class=""right""><strong>₹{item.Amount + item.GSTAmount:N0}</strong></td>
</tr>");
                i++;
            }

            var termsList = new StringBuilder();
            foreach (var t in po.Terms.Select(t => t.TermsAndConditionDetails).Concat(po.AdditionalTerms.Select(a => a.TermText)))
                termsList.Append($"<li>{System.Net.WebUtility.HtmlEncode(t)}</li>");

            string statusWatermark = po.Status == "Approved" ? "" : po.Status.ToUpper();

            string deliveryNames = string.Join(", ", po.DeliveryLocations.Select(l => l.BranchName));
            string deliveryAddresses = string.Join("<br>", po.DeliveryLocations.Select(l => System.Net.WebUtility.HtmlEncode(l.Address)));

            string vendorContact = string.Join(" · ", new[]
            {
        po.VendorContactName,
        po.VendorContactMobile,
        po.VendorContactEmail
    }.Where(x => !string.IsNullOrWhiteSpace(x)));

            // ---------------- NEW: Advance / Balance amount calculation ----------------
            decimal? advanceAmount = po.AdvancePercentage is not null
                ? Math.Round(po.TotalAmount * po.AdvancePercentage.Value / 100m, 2)
                : null;

            decimal? balanceAmount = advanceAmount is not null
                ? po.TotalAmount - advanceAmount
                : null;

            string advanceDisplay = po.AdvancePercentage is not null
                ? $"{po.AdvancePercentage}% (₹{advanceAmount:N0})"
                : "None";

            string balanceDisplay = balanceAmount is not null
                ? $"₹{balanceAmount:N0}"
                : "-";
            // Round total amount consistently
            decimal roundedTotalAmount = Math.Round(
                po.TotalAmount,
                0,
                MidpointRounding.AwayFromZero
            );

            return template
                .Replace("{{LogoBase64}}", logoBase64)
                .Replace("{{PONumber}}", po.PONumber)
                .Replace("{{IssueDate}}", DateTime.Now.ToString("dd MMM yyyy"))
                .Replace("{{BudgetYear}}", po.BudgetYear)
                .Replace("{{DepartmentCode}}", po.DepartmentCode)
                .Replace("{{Classification}}", po.Classification)
                .Replace("{{SubClassification}}", po.SubClassification ?? "-")
                .Replace("{{POType}}", po.POType)
                .Replace("{{DeliveryLocations}}", deliveryNames)
                .Replace("{{DeliveryAddress}}", deliveryAddresses)
                .Replace("{{Advance}}", advanceDisplay)                    // now includes amount, not just %
                .Replace("{{AdvanceAmount}}", advanceAmount is not null ? $"₹{advanceAmount:N0}" : "-")
                .Replace("{{BalanceAmount}}", balanceDisplay)               // NEW token
                .Replace("{{Status}}", po.Status)
                .Replace("{{StatusWatermark}}", statusWatermark)
                .Replace("{{VendorName}}", po.VendorName)
                .Replace("{{VendorCode}}", po.VendorCode)
                .Replace("{{VendorGSTIN}}", po.VendorGSTIN)
                .Replace("{{VendorState}}", po.VendorState)
                .Replace("{{VendorContactName}}", po.VendorContactName ?? "-")
                .Replace("{{VendorContactMobile}}", po.VendorContactMobile ?? "-")
                .Replace("{{VendorContactEmail}}", po.VendorContactEmail ?? "-")
                .Replace("{{VendorContact}}", string.IsNullOrWhiteSpace(vendorContact) ? "-" : vendorContact)  // now actually used
                .Replace("{{CompanyAddress}}", "No.59&60-23, 22nd Cross, 29th Main Rd, EWS Colony, BTM Layout 2nd Stage, BTM Layout, Bengaluru, Karnataka, 560076")  
                .Replace("{{CompanyGSTIN}}", "29XXXXXXXXXXXZX")                                     // TODO: same as above
                .Replace("{{TermsType}}", po.TermsType)
                .Replace("{{SubTotal}}", po.SubTotal.ToString("N0"))
                .Replace("{{GSTAmount}}", po.GSTAmount.ToString("N0"))
                .Replace("{{TotalAmount}}", po.TotalAmount.ToString("N0"))
                .Replace("{{AmountInWords}}", ConvertToWords(roundedTotalAmount))
                .Replace("{{ItemRows}}", itemRows.ToString())
                .Replace("{{TermsList}}", termsList.ToString());
        }

        private static string ConvertNumberToWords(long number)
        {
            if (number == 0)
                return "Zero";

            if (number < 0)
                return "Minus " + ConvertNumberToWords(Math.Abs(number));

            string words = "";

            if (number >= 10000000)
            {
                words += ConvertNumberToWords(number / 10000000)
                       + " Crore ";

                number %= 10000000;
            }

            if (number >= 100000)
            {
                words += ConvertNumberToWords(number / 100000)
                       + " Lakh ";

                number %= 100000;
            }

            if (number >= 1000)
            {
                words += ConvertNumberToWords(number / 1000)
                       + " Thousand ";

                number %= 1000;
            }

            if (number >= 100)
            {
                words += ConvertNumberToWords(number / 100)
                       + " Hundred ";

                number %= 100;
            }

            if (number > 0)
            {
                if (words != "")
                    words += "and ";

                string[] ones =
                {
            "", "One", "Two", "Three", "Four", "Five",
            "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen",
            "Fifteen", "Sixteen", "Seventeen", "Eighteen",
            "Nineteen"
        };

                string[] tens =
                {
            "", "", "Twenty", "Thirty", "Forty", "Fifty",
            "Sixty", "Seventy", "Eighty", "Ninety"
        };

                if (number < 20)
                {
                    words += ones[number];
                }
                else
                {
                    words += tens[number / 10];

                    if ((number % 10) > 0)
                        words += " " + ones[number % 10];
                }
            }

            return words.Trim();
        }
        private static string ConvertToWords(decimal amount)
        {
            long roundedAmount = (long)Math.Round(
                amount,
                0,
                MidpointRounding.AwayFromZero
            );

            return $"Rupees {ConvertNumberToWords(roundedAmount)} Only";
        }

        private async Task<byte[]> GenerateAndSavePurchaseOrderPdfAsync(int poId)
        {
            var po = await GetPurchaseOrderByIdAsync(poId);
            string html = BuildHtml(po);

            var browserFetcher = new BrowserFetcher();
            var installedBrowser = await browserFetcher.DownloadAsync();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = installedBrowser.GetExecutablePath(),
                Args = new[] { "--no-sandbox", "--disable-gpu" }
            });

            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });

            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions { Top = "0", Bottom = "0", Left = "0", Right = "0" }
            });

            string folder = Path.Combine(_filePath, "GeneratedPDFs", po.BudgetYear);
            Directory.CreateDirectory(folder);

            string fileName = $"{poId}_PO.pdf";   // overwrites — always reflects the PO's current/latest state
            await File.WriteAllBytesAsync(Path.Combine(folder, fileName), pdfBytes);

            return pdfBytes;
        }

        // The public endpoint method now just calls the shared helper — no duplicated logic
        public Task<byte[]> GeneratePurchaseOrderPdfAsync(int poId) => GenerateAndSavePurchaseOrderPdfAsync(poId);
    }
}