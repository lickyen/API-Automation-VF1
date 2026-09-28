
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Repository;
//using PuppeteerSharp;
//using PuppeteerSharp.Media;
using System.Text;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

        private static readonly HashSet<string> ApproverRoles = new(StringComparer.OrdinalIgnoreCase)
    { "DepartmentHead", "FinanceHead", "CFO", "ManagingDirector" };
        private static readonly HashSet<string> DepartmentScopedRoles = new(StringComparer.OrdinalIgnoreCase) { "DepartmentHead" };
        private static readonly HashSet<string> ValidPOFrequencies = new(StringComparer.OrdinalIgnoreCase)
    { "OneTime", "Monthly", "Quarterly", "HalfYearly", "Annually" };

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
            if (request.ExpectedInvoiceDate is not null && request.ExpectedInvoiceDate.Value.Date < DateTime.Today)
                throw new ArgumentException("ExpectedInvoiceDate cannot be in the past.");

            if (!ValidPOFrequencies.Contains(request.POFrequency))
                throw new ArgumentException("POFrequency must be one of: OneTime, Monthly, Quarterly, HalfYearly, Annually.");

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

            //bool quotationRequired = totalAmount > quotationThreshold;
            //if (quotationRequired && (request.Quotations is null || request.Quotations.Count == 0))
            //    throw new InvalidOperationException($"Comparative quotations are mandatory for POs above Rs. {quotationThreshold:N0}.");
            int quotationCount = request.Quotations?.Count ?? 0;
            bool quotationRequired = totalAmount > quotationThreshold;
            bool hasThreeQuotations = quotationCount >= 2;

            if (!hasThreeQuotations && string.IsNullOrWhiteSpace(request.QuotationWaiverReason))
            {
                throw new ArgumentException(
                    $"Only {quotationCount} quotation(s) provided. A QuotationWaiverReason is required when fewer than 2 comparative quotations are attached.");
            }

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
                QuotationWaiverReason = hasThreeQuotations ? null : request.QuotationWaiverReason,
                ExpectedInvoiceDate = request.ExpectedInvoiceDate,
                POFrequency = request.POFrequency,
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
                QuotationWaiverReason = hasThreeQuotations ? null : request.QuotationWaiverReason,
                ExpectedInvoiceDate = request.ExpectedInvoiceDate,
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
            try
            {
                await GeneratePurchaseOrderPdfAsync(poId);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "PDF generation failed for PO {POId} after creation. PO was still created successfully.", poId);
            }

            //await GenerateAndSavePurchaseOrderPdfAsync(poId);
            return response;
        }

        // ---------------- helpers ----------------

        private List<string> BuildApprovalChain(decimal totalAmount, bool budgetExceeded, decimal tier1Threshold, decimal tier2Threshold)
        {
            List<string> chain;

            if (totalAmount < tier1Threshold)
                chain = new List<string> { "DepartmentHead", "FinanceHead" };
            else if (totalAmount <= tier2Threshold)
                chain = new List<string> { "DepartmentHead", "FinanceHead", "CFO" };
            else
                chain = new List<string> { "DepartmentHead", "FinanceHead", "CFO", "ManagingDirector" };

            if (budgetExceeded && !chain.Contains("CFO"))
                chain.Add("CFO");

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
        public async Task<IEnumerable<BudgetCategoryResponse>> GetBudgetCategoriesAsync(string? departmentCode, string? classification)
        {
            var categories = await _repository.GetBudgetCategoriesAsync(departmentCode, classification);

            return categories.Select(c => new BudgetCategoryResponse
            {
                BudgetCategoryId = c.BudgetCategoryId,
                DepartmentCode = c.DepartmentCode,
                Classification = c.Classification,
                SubClassification = c.SubClassification,
                ItemCategory = c.ItemCategory,
                IsActive=c.IsActive
            });
        }

     


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
                QuotationWaiverReason = po.QuotationWaiverReason,
                ExpectedInvoiceDate = po.ExpectedInvoiceDate,
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
        

        public async Task<PurchaseOrderDashboardResponse> GetPurchaseOrderDashboardAsync(string employeeCode)
        {
            var user = await _repository.GetActiveUserByEmployeeCodeAsync(employeeCode)
                ?? throw new ArgumentException($"User '{employeeCode}' not found or inactive.");

            var roles = (await _repository.GetActiveRoleNamesForUserAsync(user.Id)).ToList();

            if (roles.Count == 0)
                throw new InvalidOperationException($"User '{employeeCode}' has no active role mapped — cannot determine PO access.");

            bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            bool isFinanceUser = roles.Contains("FinanceUser", StringComparer.OrdinalIgnoreCase);
            var myApproverRoles = roles.Where(r => ApproverRoles.Contains(r)).ToList();

            // ---------------- Pending With Me ----------------
            List<PoPurchaseOrder> pendingWithMe = new();

            if (isFinanceUser)
            {
                // FinanceUser's "pending with me" = POs awaiting THEIR payment action, not approval
                pendingWithMe = (await _repository.GetPendingPaymentPOEntitiesAsync()).ToList();  
            }
            else if (!isAdmin && myApproverRoles.Count > 0)
            {
                pendingWithMe = (await _repository.GetPendingWithRolesAsync(myApproverRoles, user.DepartmentCode)).ToList();
            }

            var pendingWithMeIds = pendingWithMe.Select(p => p.POId).ToHashSet();

            // ---------------- In Progress ----------------
            var inProgressRaw = isAdmin
                ? await _repository.GetAllPendingAsync()
                : await _repository.GetInProgressByDepartmentAsync(user.DepartmentCode);

            var actedByMe = (isAdmin || isFinanceUser)
                ? Enumerable.Empty<PoPurchaseOrder>()
                : await _repository.GetInProgressActedByUserAsync(employeeCode);

            var inProgress = inProgressRaw
                .Concat(actedByMe)
                .Where(p => !pendingWithMeIds.Contains(p.POId))
                .DistinctBy(p => p.POId)
                .ToList();

            // ---------------- Rejected — Needs Resubmission ----------------
            var rejected = isAdmin
                ? (await _repository.GetAllRejectedAsync()).ToList()
                : (await _repository.GetRejectedByCreatorAsync(employeeCode)).ToList();

            // ---------------- Completed ----------------
            List<PoPurchaseOrder> completed;

            if (isAdmin)
            {
                completed = (await _repository.GetAllCompletedAsync()).ToList();
            }
            else if (isFinanceUser)
            {
                // FinanceUser's "completed" = POs where THEY specifically recorded the payment
                completed = (await _repository.GetCompletedPaidByFinanceUserAsync(employeeCode)).ToList();
            }
            else
            {
                var completedDept = (await _repository.GetCompletedByDepartmentAsync(user.DepartmentCode)).ToList();
                var actedByMeCompleted = await _repository.GetCompletedActedByUserAsync(employeeCode);

                completed = completedDept
                    .Concat(actedByMeCompleted)
                    .DistinctBy(p => p.POId)
                    .OrderByDescending(p => p.ModifiedDt ?? p.CreatedDt)
                    .ToList();
            }

            // ---------------- Pending-role lookup ----------------
            var allPendingPOs = pendingWithMe.Concat(inProgress).ToList();
            var pendingRoleMap = await GetPendingRolesForPOsAsync(allPendingPOs);

            return new PurchaseOrderDashboardResponse
            {
                PendingWithMe = MapToListResponse(pendingWithMe, pendingRoleMap).ToList(),
                InProgress = MapToListResponse(inProgress, pendingRoleMap).ToList(),
                RejectedNeedsResubmission = MapToListResponse(rejected, pendingRoleMap).ToList(),
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
        public async Task<IEnumerable<DeliveryLocationResponse>> GetLocationsAsync(string? region, string? zone, bool? isActive)
        {
            var locations = await _repository.GetLocationsAsync(region, zone, isActive);
            return locations.Select(l => new DeliveryLocationResponse
            {
                LocationId = l.LocationId,
                BranchCode = l.BranchCode,
                BranchName = l.BranchName,
                Address = l.Address,
                Region = l.Region,
                Zone = l.Zone,
                MegaZone = l.MegaZone,
                IsActive = l.IsActive
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
                await _repository.MarkApprovalStepActionedAsync(
       currentStep.ApprovalId, request.ApproverEmployeeCode, "Rejected", request.Remarks);
                await _repository.UpdatePOStatusAndReasonAsync(po.POId, "Rejected", 0, request.Remarks);

                //await _repository.UpdatePOStatusAsync(po.POId, "Rejected", 0);
                try
                {
                    await GeneratePurchaseOrderPdfAsync(po.POId);
                }
                catch (Exception ex)
                {
                    
                    _logger.LogError(ex, "PDF generation failed for PO {POId} after creation. PO was still created successfully.", po.POId);
                }

             
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
                try
                {
                    await GeneratePurchaseOrderPdfAsync(po.POId);
                }
                catch (Exception ex)
                {

                    _logger.LogError(ex, "PDF generation failed for PO {POId} after creation. PO was still created successfully.", po.POId);
                }

              //  await GenerateAndSavePurchaseOrderPdfAsync(po.POId);
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

        public async Task<DepartmentBudgetUtilizationResponse> GetDepartmentBudgetUtilizationAsync(string? departmentCode, string budgetYear)
        {
            if (!string.IsNullOrWhiteSpace(departmentCode) && !await _repository.DepartmentExistsAsync(departmentCode))
                throw new ArgumentException($"DepartmentCode '{departmentCode}' does not exist or is inactive.");

            var rows = (await _repository.GetDepartmentBudgetUtilizationAsync(departmentCode, budgetYear)).ToList();

            List<ClassificationBudgetUtilizationResponse> BuildClassifications(IEnumerable<dynamic> deptRows)
                => deptRows
                    .GroupBy(r => (string)r.Classification)
                    .Select(group =>
                    {
                        var categories = group.Select(r => new BudgetUtilizationResponse
                        {
                            BudgetCategoryId = r.BudgetCategoryId,
                            SubClassification = r.SubClassification,
                            ItemCategory = r.ItemCategory,
                            AllocatedAmount = r.AllocatedAmount,
                            UsedAmount = r.UsedAmount,
                            RemainingAmount = r.AllocatedAmount - r.UsedAmount,
                            UsedPercentage = r.AllocatedAmount > 0 ? Math.Round((decimal)r.UsedAmount / r.AllocatedAmount * 100m, 2) : 0m
                        }).ToList();

                        decimal classAllocated = categories.Sum(c => c.AllocatedAmount);
                        decimal classUsed = categories.Sum(c => c.UsedAmount);

                        return new ClassificationBudgetUtilizationResponse
                        {
                            Classification = group.Key,
                            TotalAllocated = classAllocated,
                            TotalUsed = classUsed,
                            TotalRemaining = classAllocated - classUsed,
                            UsedPercentage = classAllocated > 0 ? Math.Round(classUsed / classAllocated * 100m, 2) : 0m,
                            Categories = categories
                        };
                    })
                    .OrderBy(c => c.Classification)
                    .ToList();

            if (!string.IsNullOrWhiteSpace(departmentCode))
            {
                // Single department requested — same shape as before
                var classifications = BuildClassifications(rows);
                decimal totalAllocated = classifications.Sum(c => c.TotalAllocated);
                decimal totalUsed = classifications.Sum(c => c.TotalUsed);

                return new DepartmentBudgetUtilizationResponse
                {
                    DepartmentCode = departmentCode,
                    BudgetYear = budgetYear,
                    TotalAllocated = totalAllocated,
                    TotalUsed = totalUsed,
                    TotalRemaining = totalAllocated - totalUsed,
                    OverallUsedPercentage = totalAllocated > 0 ? Math.Round(totalUsed / totalAllocated * 100m, 2) : 0m,
                    Classifications = classifications
                };
            }

            // No department specified — group by department, then classification within each
            var departments = rows
                .GroupBy(r => (string)r.DepartmentCode)
                .Select(deptGroup =>
                {
                    var classifications = BuildClassifications(deptGroup);
                    decimal deptAllocated = classifications.Sum(c => c.TotalAllocated);
                    decimal deptUsed = classifications.Sum(c => c.TotalUsed);

                    return new DepartmentBreakdownResponse
                    {
                        DepartmentCode = deptGroup.Key,
                        TotalAllocated = deptAllocated,
                        TotalUsed = deptUsed,
                        TotalRemaining = deptAllocated - deptUsed,
                        UsedPercentage = deptAllocated > 0 ? Math.Round(deptUsed / deptAllocated * 100m, 2) : 0m,
                        Classifications = classifications
                    };
                })
                .OrderBy(d => d.DepartmentCode)
                .ToList();

            decimal orgAllocated = departments.Sum(d => d.TotalAllocated);
            decimal orgUsed = departments.Sum(d => d.TotalUsed);

            return new DepartmentBudgetUtilizationResponse
            {
                DepartmentCode = null,
                BudgetYear = budgetYear,
                TotalAllocated = orgAllocated,
                TotalUsed = orgUsed,
                TotalRemaining = orgAllocated - orgUsed,
                OverallUsedPercentage = orgAllocated > 0 ? Math.Round(orgUsed / orgAllocated * 100m, 2) : 0m,
                Departments = departments
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

    


        public async Task<byte[]> GeneratePurchaseOrderPdfAsync(int poId)
    {
        var po = await GetPurchaseOrderByIdAsync(poId);

        var navy = "#1F3763";
        var grey = "#6D6E71";
        var mist = "#A7B6BF";
        var line = "#D9E0E5";
        var panelBg = "#F4F7F9";

        decimal? advanceAmount = po.AdvancePercentage is not null
            ? Math.Round(po.TotalAmount * po.AdvancePercentage.Value / 100m, 2)
            : null;
        decimal? balanceAmount = advanceAmount is not null ? po.TotalAmount - advanceAmount : null;

        const float sidebarWidth = 100f;
        const float a4HeightPt = 841.89f;
        const float pageMarginPt = 8f;

        void PartyBox(IContainer container, string label, string boldLine, string detail)
        {
            container.Border(1).BorderColor(Colors.White).Background(panelBg).Padding(8).Column(c =>
            {
                c.Item().Text(label.ToUpper()).FontColor(grey).FontSize(6);
                c.Item().PaddingTop(2).Text(boldLine).FontColor(navy).FontSize(9).Bold();
                c.Item().PaddingTop(2).Text(detail).FontSize(7).LineHeight(1.35f);
            });
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(pageMarginPt);
                page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9));

                page.Content().Layers(layers =>
                {
                    // Watermark — declared first so it renders BEHIND the primary content layer
                    layers.Layer().AlignCenter().AlignMiddle()
                        .Rotate(-30)
                        .Text(FormatStatus(po.Status))
                        .FontSize(60).Bold().FontColor("#F6D9DC");

                    layers.PrimaryLayer().Row(row =>
                    {
                        row.ConstantItem(sidebarWidth);   // reserved space; visual sidebar drawn in the background layer below

                        row.RelativeItem().Padding(18).Column(content =>
                        {
                            content.Item().Column(c =>
                            {
                                c.Item().Text("SUPPLIER DOCUMENT").FontColor(grey).FontSize(7);
                                c.Item().Text("Purchase Order").FontColor(navy).FontSize(16).Bold();
                            });

                            // Row 1: Supplier | Bill To
                            content.Item().PaddingTop(14).Row(r =>
                            {
                                r.RelativeItem().PaddingRight(6).Element(e =>
                                    PartyBox(e, "Supplier", po.VendorName,
                                        $"Vendor code: {po.VendorCode}\nGSTIN: {po.VendorGSTIN}\n{po.VendorState}"));

                                r.RelativeItem().PaddingLeft(6).Element(e =>
                                    PartyBox(e, "Bill to", "Vistaar Financial Services Private Limited",
                                        "Registered Office Address, Bengaluru, Karnataka\nGSTIN: 29XXXXXXXXXXXZX"));
                            });

                            // Row 2: Deliver To | Commercial Terms
                            content.Item().PaddingTop(8).Row(r =>
                            {
                                r.RelativeItem().PaddingRight(6).Element(e =>
                                    PartyBox(e, "Deliver to",
                                        string.Join(", ", po.DeliveryLocations.Select(l => l.BranchName)),
                                        string.Join("\n", po.DeliveryLocations.Select(l => l.Address))));

                                r.RelativeItem().PaddingLeft(6).Element(e =>
                                    PartyBox(e, "Commercial terms", "Currency: INR",
                                        $"Advance: {(po.AdvancePercentage is not null ? $"{po.AdvancePercentage}% (₹{advanceAmount:N0})" : "None")}\nBalance due: {(balanceAmount is not null ? $"₹{balanceAmount:N0}" : "-")}"));
                            });

                            content.Item().PaddingTop(14).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(20);
                                    c.RelativeColumn(4);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(1.5f);
                                    c.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    void H(string text) => header.Cell().Background(grey).Padding(5)
                                        .Text(text.ToUpper()).FontColor(Colors.White).FontSize(6);

                                    H("#"); H("Description / classification"); H("Qty"); H("Rate"); H("Taxable"); H("Tax"); H("Total");
                                });

                                int i = 1;
                                foreach (var item in po.Items)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(line).Padding(5).Text(i.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(line).Padding(5).Column(c =>
                                    {
                                        c.Item().Text(item.ItemDescription).Bold().FontColor(navy);
                                        c.Item().Text($"Sub-category: {item.SubClassification} · Item category: {item.ItemCategory}{(item.IsBudgetExceeded ? " · Budget Exceeded" : "")}")
                                            .FontSize(7).FontColor(item.IsBudgetExceeded ? Colors.Red.Medium : grey);
                                    });
                                    table.Cell().BorderBottom(1).BorderColor(line).Padding(5).AlignCenter().Text(item.Quantity.ToString("N0"));
                                    table.Cell().BorderBottom(1).BorderColor(line).Padding(5).AlignRight().Text(item.UnitPrice.ToString("N0"));
                                    table.Cell().BorderBottom(1).BorderColor(line).Padding(5).AlignRight().Text(item.Amount.ToString("N0"));
                                    table.Cell().BorderBottom(1).BorderColor(line).Padding(5).AlignRight().Text(item.GSTAmount.ToString("N0"));
                                    table.Cell().BorderBottom(1).BorderColor(line).Padding(5).AlignRight().Text((item.Amount + item.GSTAmount).ToString("N0")).Bold();
                                    i++;
                                }
                            });

                            content.Item().PaddingTop(12).Background(navy).Padding(10).Row(t =>
                            {
                                t.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Total amount").FontColor(Colors.White).FontSize(8);
                                    c.Item().Text($"Taxable {po.SubTotal:N0} · Tax {po.GSTAmount:N0}").FontColor("#CDD7E1").FontSize(7);
                                });
                                t.ConstantItem(100).AlignRight().AlignMiddle()
                                    .Text(po.TotalAmount.ToString("N0")).FontColor(Colors.White).FontSize(16).Bold();
                            });

                            content.Item().PaddingTop(6).Text(t =>
                            {
                                t.Span("AMOUNT IN WORDS  ").FontColor(grey).FontSize(7);
                                t.Span(ConvertToWords(po.TotalAmount)).FontColor(navy).Bold();
                            });

                            content.Item().PaddingTop(16).BorderTop(1).BorderColor(line).PaddingTop(8).Column(terms =>
                            {
                                terms.Item().Text("Commercial and control terms").FontColor(navy).FontSize(9).Bold();

                                int n = 1;
                                var allTerms = po.Terms.Select(t => t.TermsAndConditionDetails)
                                    .Concat(po.AdditionalTerms.Select(a => a.TermText));

                                foreach (var t in allTerms)
                                {
                                    terms.Item().PaddingTop(3).Text($"{n}. {t}").FontSize(8).LineHeight(1.4f);
                                    n++;
                                }
                            });

                            content.Item().PaddingTop(14).Border(1).BorderColor(mist).Background(panelBg)
                                .Padding(8).AlignCenter()
                                .Text("This is a system-generated Purchase Order. Hence, no signature is required.")
                                .FontColor(navy).Bold().FontSize(8);
                        });
                    });

                    // Background layer: fixed-height navy sidebar
                    layers.Layer().Row(row =>
                    {
                        row.ConstantItem(sidebarWidth)
                            .Height(a4HeightPt - (pageMarginPt * 2))
                            .Background(navy).Padding(12).Column(side =>
                            {
                                side.Item().PaddingBottom(14).Image(_logoPath).FitWidth();
                                side.Item().Text("PURCHASE\nORDER").FontColor(Colors.White).FontSize(13).Bold().LineHeight(1.05f);
                                side.Item().PaddingTop(12).Text(po.PONumber).FontColor(Colors.White).FontSize(10).Bold();

                                void Field(string label, string value)
                                {
                                    side.Item().PaddingTop(9).Text(label.ToUpper()).FontColor(mist).FontSize(6);
                                    side.Item().Text(value).FontColor(Colors.White).FontSize(7.5f).Bold();
                                }

                                Field("Issue date", DateTime.Now.ToString("dd MMM yyyy"));
                                Field("Financial year", po.BudgetYear);
                                Field("Department", $"{po.DepartmentCode} · {po.Classification}");
                                Field("Sub classification", po.SubClassification ?? "-");
                                Field("PO type", po.POType);
                                Field("Delivery", string.Join(", ", po.DeliveryLocations.Select(l => l.BranchName)));
                                Field("Advance", po.AdvancePercentage is not null ? $"{po.AdvancePercentage}% (₹{advanceAmount:N0})" : "None");

                                side.Item().ExtendVertical();

                                side.Item().PaddingTop(10).LineHorizontal(1).LineColor("#5F7697");
                                side.Item().PaddingTop(8).Text("Vistaar Financial Services Private Limited")
                                .FontColor(Colors.White).FontSize(7).Bold();
                                side.Item().PaddingTop(3).Text("System-generated controlled procurement document")
                                .FontColor("#CBD6DF").FontSize(6).LineHeight(1.4f);
                            });

                        row.RelativeItem();
                    });
                });
            });
        });

        var pdfBytes = document.GeneratePdf();

        string folder = Path.Combine(_filePath, "GeneratedPDFs", po.BudgetYear);
        Directory.CreateDirectory(folder);
        string fileName = $"{poId}_PO.pdf";
        await File.WriteAllBytesAsync(Path.Combine(folder, fileName), pdfBytes);

        return pdfBytes;
    }

        private static string FormatStatus(string status)
        => System.Text.RegularExpressions.Regex.Replace(status, "(?<!^)([A-Z])", " $1").ToUpper();

        public async Task<BudgetCategoryResponse> CreateBudgetCategoryAsync(CreateBudgetCategoryRequest request)
        {
            if (!await _repository.DepartmentExistsAsync(request.DepartmentCode))
                throw new ArgumentException($"DepartmentCode '{request.DepartmentCode}' does not exist or is inactive.");

            if (!string.Equals(request.Classification, "Capex", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.Classification, "Opex", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Classification must be either 'Capex' or 'Opex'.");
            }

            if (string.IsNullOrWhiteSpace(request.SubClassification) || string.IsNullOrWhiteSpace(request.ItemCategory))
                throw new ArgumentException("SubClassification and ItemCategory are required.");

            if (await _repository.BudgetCategoryDuplicateExistsAsync(
                request.DepartmentCode, request.Classification, request.SubClassification, request.ItemCategory))
            {
                throw new ArgumentException("A budget category with this Department, Classification, SubClassification and ItemCategory already exists.");
            }

            var category = new Models.PoBudgetCategory
            {
                DepartmentCode = request.DepartmentCode,
                Classification = request.Classification,
                SubClassification = request.SubClassification,
                ItemCategory = request.ItemCategory,
                CreatedBy = request.CreatedBy
            };

            int budgetCategoryId = await _repository.CreateBudgetCategoryAsync(category);

            return new BudgetCategoryResponse
            {
                BudgetCategoryId = budgetCategoryId,
                DepartmentCode = request.DepartmentCode,
                Classification = request.Classification,
                SubClassification = request.SubClassification,
                ItemCategory = request.ItemCategory,
                IsActive = true
            };
        }

        public async Task<AllocatedBudgetResponse> AllocateBudgetAsync(AllocateBudgetRequest request)
        {
            var category = await _repository.GetBudgetCategoryByIdAsync(request.BudgetCategoryId)
                ?? throw new KeyNotFoundException($"BudgetCategoryId {request.BudgetCategoryId} not found.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.BudgetYear, @"^FY[1-2]\d{3}-\d{2}$"))
                throw new ArgumentException("BudgetYear must be in the format 'FY2026-27'.");

            if (request.Budget <= 0)
                throw new ArgumentException("Budget must be greater than 0.");

            if (await _repository.BudgetAllocationExistsAsync(request.BudgetCategoryId, request.BudgetYear))
                throw new ArgumentException($"A budget allocation already exists for this category in {request.BudgetYear}. Use an update instead.");

            int id = await _repository.AllocateBudgetAsync(request.BudgetCategoryId, request.BudgetYear, request.Budget, request.CreatedBy);

            return new AllocatedBudgetResponse
            {
                Id = id,
                BudgetCategoryId = category.BudgetCategoryId,
                DepartmentCode = category.DepartmentCode,
                Classification = category.Classification,
                SubClassification = category.SubClassification,
                ItemCategory = category.ItemCategory,
                BudgetYear = request.BudgetYear,
                Budget = request.Budget
            };
        }

        public async Task UpdateBudgetCategoryStatusAsync(UpdateBudgetCategoryStatusRequest request)
        {
            var category = await _repository.GetBudgetCategoryByIdAsync(request.BudgetCategoryId)
                ?? throw new KeyNotFoundException($"BudgetCategoryId {request.BudgetCategoryId} not found.");

            if (category.IsActive == request.IsActive)
            {
                throw new InvalidOperationException(
                    $"BudgetCategoryId {request.BudgetCategoryId} is already {(request.IsActive ? "active" : "inactive")}.");
            }

            await _repository.UpdateBudgetCategoryStatusAsync(request.BudgetCategoryId, request.IsActive, request.ModifiedBy);
        }

        public async Task<PurchaseOrderResponse> UpdatePurchaseOrderAsync(UpdatePurchaseOrderRequest request)
        {
            var existing = await _repository.GetPurchaseOrderForUpdateAsync(request.POId)
                ?? throw new KeyNotFoundException($"Purchase Order {request.POId} not found.");

            if (existing.Status != "Rejected")
                throw new InvalidOperationException($"This Purchase Order is not in Rejected status (current status: {existing.Status}). Only rejected POs can be edited and resubmitted.");

            var settings = await _repository.GetSettingsAsync(
                new[] { "QuotationThresholdAmount", "ApprovalTier1Threshold", "ApprovalTier2Threshold" });

            decimal quotationThreshold = settings.TryGetValue("QuotationThresholdAmount", out var qt) && decimal.TryParse(qt, out var qtVal)
                ? qtVal : 25000m;

            decimal tier1Threshold = settings.TryGetValue("ApprovalTier1Threshold", out var t1) && decimal.TryParse(t1, out var t1Val)
                ? t1Val : 100000m;

            decimal tier2Threshold = settings.TryGetValue("ApprovalTier2Threshold", out var t2) && decimal.TryParse(t2, out var t2Val)
                ? t2Val : 3000000m;

            // ---------------- pre-validation (unchanged) ----------------
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
                throw new ArgumentException("POType must be either 'Standard' or 'Blanket'.");

            if (!string.Equals(request.TermsType, "Normal", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(request.TermsType, "Advance", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("TermsType must be either 'Normal' or 'Advance'.");

            if (string.Equals(request.TermsType, "Advance", StringComparison.OrdinalIgnoreCase))
            {
                if (request.AdvancePercentage is null || request.AdvancePercentage <= 0 || request.AdvancePercentage > 100)
                    throw new ArgumentException("AdvancePercentage is required and must be between 0 and 100 when TermsType is 'Advance'.");
            }
            else if (request.AdvancePercentage is not null)
            {
                throw new ArgumentException("AdvancePercentage should not be provided when TermsType is 'Normal'.");
            }

            if (!await _repository.SubClassificationExistsAsync(request.DepartmentCode, request.Classification, request.SubClassification))
                throw new ArgumentException($"SubClassification '{request.SubClassification}' is not valid for department '{request.DepartmentCode}' / classification '{request.Classification}'.");

            if (request.DeliveryLocationIds is null || request.DeliveryLocationIds.Count == 0)
                throw new ArgumentException("At least one DeliveryLocationId is required.");

            if (!await _repository.AllLocationsExistAsync(request.DeliveryLocationIds))
                throw new ArgumentException("One or more DeliveryLocationIds do not exist or are inactive.");

            if (request.ExpectedInvoiceDate is not null && request.ExpectedInvoiceDate.Value.Date < DateTime.Today)   // NEW
                throw new ArgumentException("ExpectedInvoiceDate cannot be in the past.");

            if (!ValidPOFrequencies.Contains(request.POFrequency))
                throw new ArgumentException("POFrequency must be one of: OneTime, Monthly, Quarterly, HalfYearly, Annually.");

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

            // ---------------- Build items (GST calc unchanged) ----------------
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

            // ---------------- Quotation rule — same as Create ----------------
            bool quotationRequired = totalAmount > quotationThreshold;

            const int MinimumQuotationsRequired = 2;
            int quotationCount = request.Quotations?.Count ?? 0;
            bool hasEnoughQuotations = quotationCount >= MinimumQuotationsRequired;

            if (!hasEnoughQuotations && string.IsNullOrWhiteSpace(request.QuotationWaiverReason))
            {
                throw new ArgumentException(
                    $"Only {quotationCount} quotation(s) provided. A QuotationWaiverReason is required when fewer than {MinimumQuotationsRequired} comparative quotations are attached.");
            }

            // ---------------- Budget check (unchanged) ----------------
            var categoryTotals = items.GroupBy(i => i.BudgetCategoryId).ToDictionary(g => g.Key, g => g.Sum(i => i.Amount + i.GSTAmount));
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

            // ---------------- Update header, wipe & re-insert child rows ----------------
            var po = new Models.PoPurchaseOrder
            {
                POId = request.POId,
                VendorId = request.VendorId,
                VendorAddressId = request.VendorAddressId,
                VendorContactName = request.VendorContactName,
                VendorContactMobile = request.VendorContactMobile,
                VendorContactEmail = request.VendorContactEmail,
                DepartmentCode = request.DepartmentCode,
                POType = request.POType,
                Classification = request.Classification,
                SubClassification = request.SubClassification,
                BudgetYear = request.BudgetYear,
                TermsType = request.TermsType,
                AdvancePercentage = request.AdvancePercentage,
                QuotationRequired = quotationRequired,                                          // NEW — was missing entirely
                QuotationWaiverReason = hasEnoughQuotations ? null : request.QuotationWaiverReason,  // NEW
                ExpectedInvoiceDate = request.ExpectedInvoiceDate,
                POFrequency = request.POFrequency,
                SubTotal = subTotal,
                GSTAmount = gstTotal,
                TotalAmount = totalAmount,
                IsBudgetExceeded = budgetExceeded,
                ModifiedBy = request.UpdatedBy
            };

            await _repository.DeletePurchaseOrderChildDataAsync(request.POId);
            await _repository.UpdatePurchaseOrderHeaderAsync(po);

            var itemsList = items.ToList();
            await _repository.AddPurchaseOrderItemsAsync(request.POId, itemsList);

            var savedQuotationPaths = SaveQuotationFiles(request.Quotations, request.BudgetYear, request.POId);
            await _repository.AddQuotationsAsync(request.POId, savedQuotationPaths);

            if (selectedTerms.Count > 0)
                await _repository.AddPurchaseOrderTermsAsync(request.POId, request.TermMasterIds, request.UpdatedBy);

            var cleanedAdditionalTerms = (request.AdditionalTerms ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList();
            if (cleanedAdditionalTerms.Count > 0)
                await _repository.AddAdditionalTermsAsync(request.POId, cleanedAdditionalTerms, request.UpdatedBy);

            await _repository.AddPurchaseOrderDeliveryLocationsAsync(request.POId, request.DeliveryLocationIds, request.UpdatedBy);

            // Fresh approval chain — uses the current role names (FinanceHead/CFO), re-enters at step 1
            var roles = BuildApprovalChain(totalAmount, budgetExceeded, tier1Threshold, tier2Threshold);
            var steps = roles.Select((role, i) => new Models.PoPurchaseOrderApprovalHistory
            {
                StepNumber = i + 1,
                ApproverRole = role
            }).ToList();
            await _repository.AddApprovalStepsAsync(request.POId, steps);

            var response = await GetPurchaseOrderByIdAsync(request.POId);

            try { await GeneratePurchaseOrderPdfAsync(request.POId); }
            catch (Exception ex) { _logger.LogError(ex, "PDF generation failed for PO {POId} after resubmission.", request.POId); }

            return response;
        }
        public async Task CancelPurchaseOrderAsync(CancelPurchaseOrderRequest request)
        {
            var po = await _repository.GetPurchaseOrderForUpdateAsync(request.POId)
                ?? throw new KeyNotFoundException($"Purchase Order {request.POId} not found.");

            // Only the Preparer who created it can cancel
            if (!string.Equals(po.CreatedBy, request.CancelledBy, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only the Preparer who created this Purchase Order can cancel it.");

            // Can cancel at any stage EXCEPT once fully Approved (or already Cancelled/Rejected)
            var cancellableStatuses = new[] { "Draft", "PendingApproval", "PendingPayment" };
            if (!cancellableStatuses.Contains(po.Status))
                throw new InvalidOperationException($"This Purchase Order cannot be cancelled from its current status ({po.Status}).");

            if (string.IsNullOrWhiteSpace(request.CancellationReason))
                throw new ArgumentException("CancellationReason is required.");

            await _repository.CancelPurchaseOrderAsync(request.POId, request.CancellationReason, request.CancelledBy);

            try { await GeneratePurchaseOrderPdfAsync(request.POId); }
            catch (Exception ex) { _logger.LogError(ex, "PDF generation failed for PO {POId} after cancellation.", request.POId); }
        }

        public async Task<POVendorDashboardResponse> GetVendorDashboardAsync(string employeeCode)
        {
            var user = await _repository.GetActiveUserByEmployeeCodeAsync(employeeCode)
                ?? throw new ArgumentException($"User '{employeeCode}' not found or inactive.");

            var roles = (await _repository.GetActiveRoleNamesForUserAsync(user.Id)).ToList();

            if (roles.Count == 0)
                throw new InvalidOperationException($"User '{employeeCode}' has no active role mapped — cannot determine vendor access.");

            bool isFinanceUser = roles.Contains("FinanceUser", StringComparer.OrdinalIgnoreCase);
            bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            bool seesAllPendingRejected = isFinanceUser || isAdmin;

            var approved = (await _repository.GetVendorsByStatusAsync("ACTIVE")).ToList();
            var disabled = (await _repository.GetDisabledVendorsAsync()).ToList();

            List<POVendorMaster> pending, rejected;

            if (seesAllPendingRejected)
            {
                pending = (await _repository.GetVendorsByStatusAsync("PENDING")).ToList();
                rejected = (await _repository.GetVendorsByStatusAsync("REJECTED")).ToList();
            }
            else
            {
                pending = (await _repository.GetVendorsByStatusAndCreatorAsync("PENDING", employeeCode)).ToList();
                rejected = (await _repository.GetVendorsByStatusAndCreatorAsync("REJECTED", employeeCode)).ToList();
            }

            // NEW — pending addresses, only for FinanceUser/Admin (the ones who actually approve them)
            var pendingAddresses = new List<PendingVendorAddressResponse>();
            if (seesAllPendingRejected)
            {
                var addressRows = await _repository.GetPendingVendorAddressesAsync();
                pendingAddresses = addressRows.Select(r => new PendingVendorAddressResponse
                {
                    VendorAddressId = r.VendorAddressId,
                    VendorId = r.VendorId,
                    VendorName = r.VendorName,
                    VendorCode = r.VendorCode,
                    AddressLine1 = r.AddressLine1,
                    City = r.City,
                    StateName = r.StateName,
                    PinCode = r.PinCode,
                    GSTIN = r.GSTIN,
                    IsPrimaryAddress = r.IsPrimaryAddress,
                    CreatedBy = r.CreatedBy,
                    CreatedDate = r.CreatedDate
                }).ToList();
            }

            return new POVendorDashboardResponse
            {
                Approved = MapToVendorListResponse(approved),
                Pending = MapToVendorListResponse(pending),
                Rejected = MapToVendorListResponse(rejected),
                Disabled = MapToVendorListResponse(disabled),
                PendingAddresses = pendingAddresses
            };
        }

        private static List<POVendorListItemResponse> MapToVendorListResponse(IEnumerable<POVendorMaster> vendors)
            => vendors.Select(v => new POVendorListItemResponse
            {
                VendorId = v.VendorId,
                VendorCode = v.VendorCode,
                VendorName = v.VendorName,
                MSMEVendor = v.MSMEVendor,
                Status = v.Status,
                RejectionReason = v.RejectionReason,
                CreatedBy = v.CreatedBy,
                CreatedDate = v.CreatedDate
            }).ToList();

        public async Task UpdateVendorStatusAsync(UpdateVendorStatusRequest request)
        {
            var (vendor, _) = await _repository.GetVendorByIdAsync(request.VendorId);
            if (vendor is null)
                throw new KeyNotFoundException($"Vendor {request.VendorId} not found.");

            // Only ACTIVE and INACTIVE vendors can be toggled this way — PENDING/REJECTED go through the approval flow instead
            if (vendor.Status != "ACTIVE" && vendor.Status != "INACTIVE")
                throw new InvalidOperationException($"Vendor cannot be enabled/disabled from its current status ({vendor.Status}). Only Active or Inactive vendors can be toggled.");

            bool alreadyInTargetState = (request.IsActive && vendor.Status == "ACTIVE") || (!request.IsActive && vendor.Status == "INACTIVE");
            if (alreadyInTargetState)
                throw new InvalidOperationException($"Vendor is already {(request.IsActive ? "Active" : "Inactive")}.");

            if (!request.IsActive && string.IsNullOrWhiteSpace(request.Reason))
                throw new ArgumentException("A reason is required when disabling a vendor.");

            string newStatus = request.IsActive ? "ACTIVE" : "INACTIVE";
            string? reason = request.IsActive ? null : request.Reason;   // clear the reason when re-enabling

            await _repository.UpdateVendorActiveStatusAsync(request.VendorId, newStatus, reason, request.ModifiedBy);
        }

        public async Task<VendorAddressResponse> CreateVendorAddressAsync(CreateVendorAddressRequest request)
        {
            var (vendor, _) = await _repository.GetVendorByIdAsync(request.VendorId);
            if (vendor is null)
                throw new ArgumentException($"VendorId {request.VendorId} does not exist.");

            if (!string.IsNullOrWhiteSpace(request.GSTIN))
            {
                if (await _repository.GstinExistsAsync(request.GSTIN))
                    throw new ArgumentException($"GSTIN '{request.GSTIN}' is already registered against another address.");

                if (request.GSTDocument is null)
                    throw new ArgumentException("GSTDocument is required when GSTIN is provided.");
            }

            string? gstDocPath = null;
            if (request.GSTDocument is not null)
            {
                string folder = Path.Combine(_filePath, "VendorAddressDocuments", request.VendorId.ToString());
                Directory.CreateDirectory(folder);
                gstDocPath = SaveVendorDocument(request.GSTDocument, folder, "GST");
            }
            // If this new address is being marked primary, demote any existing primary for this vendor first
            if (request.IsPrimaryAddress)
            {
                await _repository.ClearPrimaryAddressFlagAsync(request.VendorId);
            }


            var address = new Models.POVendorAddressMaster
            {
                VendorId = request.VendorId,
                AddressLine1 = request.AddressLine1,
                City = request.City,
                StateName = request.StateName,
               
                PinCode = request.PinCode,
                GSTIN = request.GSTIN,
                IsPrimaryAddress = request.IsPrimaryAddress,
                GSTDocumentPath = gstDocPath,
                CreatedBy = request.CreatedBy
            };

            long vendorAddressId = await _repository.CreateVendorAddressAsync(address);

            var created = await _repository.GetVendorAddressByIdAsync(vendorAddressId)
                ?? throw new InvalidOperationException("Vendor address was created but could not be reloaded.");

            return MapToAddressResponse(created);
        }

        public async Task<VendorAddressResponse> UpdateVendorAddressAsync(UpdateVendorAddressRequest request)
        {
            var existing = await _repository.GetVendorAddressByIdAsync(request.VendorAddressId)
                ?? throw new KeyNotFoundException($"VendorAddress {request.VendorAddressId} not found.");

            if (existing.Status != "REJECTED")
                throw new InvalidOperationException($"This address cannot be edited from its current status ({existing.Status}). Only rejected addresses can be resubmitted.");

            if (!string.IsNullOrWhiteSpace(request.GSTIN) &&
                !string.Equals(request.GSTIN, existing.GSTIN, StringComparison.OrdinalIgnoreCase) &&
                await _repository.GstinExistsAsync(request.GSTIN))
            {
                throw new ArgumentException($"GSTIN '{request.GSTIN}' is already registered against another address.");
            }

            string? gstDocPath = existing.GSTDocumentPath;
            if (request.GSTDocument is not null)
            {
                string folder = Path.Combine(_filePath, "VendorAddressDocuments", existing.VendorId.ToString());
                Directory.CreateDirectory(folder);
                gstDocPath = SaveVendorDocument(request.GSTDocument, folder, "GST");
            }

            var address = new Models.POVendorAddressMaster
            {
                VendorAddressId = request.VendorAddressId,
                AddressLine1 = request.AddressLine1,
                City = request.City,
                StateName = request.StateName,
                
                PinCode = request.PinCode,
                GSTIN = request.GSTIN,
                IsPrimaryAddress = request.IsPrimaryAddress,
                GSTDocumentPath = gstDocPath,
                ModifiedBy = request.UpdatedBy
            };

            await _repository.UpdateVendorAddressAsync(address);

            var updated = await _repository.GetVendorAddressByIdAsync(request.VendorAddressId)
                ?? throw new InvalidOperationException("Vendor address was updated but could not be reloaded.");

            return MapToAddressResponse(updated);
        }

        public async Task ApproveVendorAddressAsync(ApproveVendorAddressRequest request)
        {
            var address = await _repository.GetVendorAddressByIdAsync(request.VendorAddressId)
                ?? throw new KeyNotFoundException($"VendorAddress {request.VendorAddressId} not found.");

            if (address.Status != "PENDING")
                throw new InvalidOperationException($"This address is not pending approval (current status: {address.Status}).");

            await _repository.ApproveVendorAddressAsync(request.VendorAddressId, request.ApprovedBy);
        }

        public async Task RejectVendorAddressAsync(RejectVendorAddressRequest request)
        {
            var address = await _repository.GetVendorAddressByIdAsync(request.VendorAddressId)
                ?? throw new KeyNotFoundException($"VendorAddress {request.VendorAddressId} not found.");

            if (address.Status != "PENDING")
                throw new InvalidOperationException($"This address is not pending approval (current status: {address.Status}).");

            if (string.IsNullOrWhiteSpace(request.RejectionReason))
                throw new ArgumentException("RejectionReason is required.");

            await _repository.RejectVendorAddressAsync(request.VendorAddressId, request.RejectionReason, request.RejectedBy);
        }

        public async Task UpdateVendorAddressStatusAsync(UpdateVendorAddressStatusRequest request)
        {
            var address = await _repository.GetVendorAddressByIdAsync(request.VendorAddressId)
                ?? throw new KeyNotFoundException($"VendorAddress {request.VendorAddressId} not found.");

            if (address.Status != "ACTIVE" && address.Status != "INACTIVE")
                throw new InvalidOperationException($"Address cannot be enabled/disabled from its current status ({address.Status}).");

            string newStatus = request.IsActive ? "ACTIVE" : "INACTIVE";
            await _repository.UpdateVendorAddressActiveStatusAsync(request.VendorAddressId, newStatus, request.ModifiedBy);
        }

        private static VendorAddressResponse MapToAddressResponse(Models.POVendorAddressMaster a) => new()
        {
            VendorAddressId = a.VendorAddressId,
            VendorId = a.VendorId,
            AddressLine1 = a.AddressLine1,
            City = a.City,
            StateName = a.StateName,
           
            PinCode = a.PinCode,
            GSTIN = a.GSTIN,
            IsPrimaryAddress = a.IsPrimaryAddress,
            Status = a.Status,
            RejectionReason = a.RejectionReason,
            GSTDocument = LoadVendorDocument(a.GSTDocumentPath)
        };

        public async Task<DeliveryLocationResponse> CreateLocationAsync(CreateLocationRequest request)
        {
            if (await _repository.BranchCodeExistsAsync(request.BranchCode))
                throw new ArgumentException($"BranchCode '{request.BranchCode}' already exists.");

            var location = new Models.PoLocation
            {
                BranchCode = request.BranchCode,
                BranchName = request.BranchName,
                Address = request.Address,
                Region = request.Region,
                Zone = request.Zone,
                MegaZone = request.MegaZone,
                CreatedBy = request.CreatedBy
            };

            int locationId = await _repository.CreateLocationAsync(location);

            var created = await _repository.GetLocationByIdAsync(locationId)
                ?? throw new InvalidOperationException("Location was created but could not be reloaded.");

            return MapToLocationResponse(created);
        }

        public async Task<DeliveryLocationResponse> UpdateLocationAsync(UpdateLocationRequest request)
        {
            var existing = await _repository.GetLocationByIdAsync(request.LocationId)
                ?? throw new KeyNotFoundException($"Location {request.LocationId} not found.");

            if (!string.Equals(existing.BranchCode, request.BranchCode, StringComparison.OrdinalIgnoreCase) &&
                await _repository.BranchCodeExistsAsync(request.BranchCode, request.LocationId))
            {
                throw new ArgumentException($"BranchCode '{request.BranchCode}' already exists.");
            }

            var location = new Models.PoLocation
            {
                LocationId = request.LocationId,
                BranchCode = request.BranchCode,
                BranchName = request.BranchName,
                Address = request.Address,
                Region = request.Region,
                Zone = request.Zone,
                MegaZone = request.MegaZone,
                ModifiedBy = request.ModifiedBy
            };

            await _repository.UpdateLocationAsync(location);

            var updated = await _repository.GetLocationByIdAsync(request.LocationId)
                ?? throw new InvalidOperationException("Location was updated but could not be reloaded.");

            return MapToLocationResponse(updated);
        }

        public async Task UpdateLocationStatusAsync(UpdateLocationStatusRequest request)
        {
            var existing = await _repository.GetLocationByIdAsync(request.LocationId)
                ?? throw new KeyNotFoundException($"Location {request.LocationId} not found.");

            if (existing.IsActive == request.IsActive)
                throw new InvalidOperationException($"Location is already {(request.IsActive ? "active" : "inactive")}.");

            await _repository.UpdateLocationStatusAsync(request.LocationId, request.IsActive, request.ModifiedBy);
        }

        private static DeliveryLocationResponse MapToLocationResponse(Models.PoLocation l) => new()
        {
            LocationId = l.LocationId,
            BranchCode = l.BranchCode,
            BranchName = l.BranchName,
            Address = l.Address,
            Region = l.Region,
            Zone = l.Zone,
            MegaZone = l.MegaZone
        };

        public async Task<AllocatedBudgetResponse> UpdateBudgetAllocationAsync(UpdateBudgetAllocationRequest request)
        {
            var category = await _repository.GetBudgetCategoryByIdAsync(request.BudgetCategoryId)
                ?? throw new KeyNotFoundException($"BudgetCategoryId {request.BudgetCategoryId} not found.");

            var existing = await _repository.GetBudgetAllocationAsync(request.BudgetCategoryId, request.BudgetYear);
            if (existing is null)
                throw new KeyNotFoundException($"No budget allocation found for this category in {request.BudgetYear}. Use AllocateBudget to create one first.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.BudgetYear, @"^FY[1-2]\d{3}-\d{2}$"))
                throw new ArgumentException("BudgetYear must be in the format 'FY2026-27'.");

            if (request.Budget <= 0)
                throw new ArgumentException("Budget must be greater than 0.");

            // Warn/block if reducing below what's already been consumed by approved POs
            var consumedByCategory = await _repository.GetConsumedBudgetsAsync(new[] { request.BudgetCategoryId }, request.BudgetYear);
            decimal alreadyConsumed = consumedByCategory.TryGetValue(request.BudgetCategoryId, out var c) ? c : 0m;

            if (request.Budget < alreadyConsumed)
            {
                throw new InvalidOperationException(
                    $"Cannot set budget to {request.Budget:N2} — {alreadyConsumed:N2} has already been consumed by approved POs against this category in {request.BudgetYear}.");
            }

            await _repository.UpdateBudgetAllocationAsync(request.BudgetCategoryId, request.BudgetYear, request.Budget, request.ModifiedBy);

            return new AllocatedBudgetResponse
            {
                Id = (int)existing.Id,
                BudgetCategoryId = category.BudgetCategoryId,
                DepartmentCode = category.DepartmentCode,
                Classification = category.Classification,
                SubClassification = category.SubClassification,
                ItemCategory = category.ItemCategory,
                BudgetYear = request.BudgetYear,
                Budget = request.Budget
            };
        }

    }
}