using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Repository;
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

        public PurchaseOrderService( PurchaseOrderRepository repository,
            ILogger<PurchaseOrderService> logger, AppSettings appSettings)
        {
            _repository = repository;
            _logger = logger;
            _filePath = appSettings.FilePaths.POBasePath;
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
        public async Task<POVendorDetailResponse> GetVendorByIdAsync(int vendorId)
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
                }).ToList()
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
        //private List<string> BuildApprovalChain(decimal totalAmount, bool budgetExceeded)
        //{
        //    List<string> chain;

        //    if (totalAmount < Tier1Threshold)
        //        chain = new List<string> { "DepartmentHead", "FinancialController" };
        //    else if (totalAmount <= Tier2Threshold)
        //        chain = new List<string> { "DepartmentHead", "FinancialController", "FinanceHead" };
        //    else
        //        chain = new List<string> { "DepartmentHead", "FinancialController", "FinanceHead", "ManagingDirector" };

        //    // Budget-exceeded is a minimum escalation floor: bump < ₹1L POs up to include FinanceHead.
        //    // Tiers that already include FinanceHead (or higher) are unaffected.
        //    if (budgetExceeded && !chain.Contains("FinanceHead"))
        //        chain.Add("FinanceHead");

        //    return chain;
        //}
       

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

            return new PurchaseOrderResponse
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
        
        private static readonly HashSet<string> DepartmentScopedRoles = new(StringComparer.OrdinalIgnoreCase){ "DepartmentHead" };


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

            return new PurchaseOrderResponse
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
                }).ToList()
            };
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

            var inProgress = inProgressRaw.Where(p => !pendingWithMeIds.Contains(p.POId)).ToList();

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
                Address= l.Address,
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
                // Rejected at any stage -> back to Maker; status becomes Rejected, all approvals must be re-taken on resubmission.
                await _repository.UpdatePOStatusAsync(po.POId, "Rejected", 0);
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
                // Final step cleared -> PO fully approved
                await _repository.UpdatePOStatusAsync(po.POId, "Approved", po.CurrentApprovalStep);
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
    }
}
