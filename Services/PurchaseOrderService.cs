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

        private const decimal DefaultQuotationThreshold = 25000m; // TODO: move to a settings table once one exists

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

            // ---------------- Explicit pre-validation ----------------

            if (!await _repository.VendorExistsAsync(request.VendorId))
                throw new ArgumentException($"VendorId {request.VendorId} does not exist or is not Approved.");

            if (!await _repository.VendorAddressExistsAsync(request.VendorAddressId, request.VendorId))
                throw new ArgumentException($"VendorAddressId {request.VendorAddressId} does not exist for VendorId {request.VendorId}.");

            if (!await _repository.DepartmentExistsAsync(request.DepartmentCode))
                throw new ArgumentException($"DepartmentCode '{request.DepartmentCode}' does not exist or is inactive.");

            if (!await _repository.BudgetCategoryExistsAsync(request.BudgetCategoryId, request.DepartmentCode))
                throw new ArgumentException($"BudgetCategoryId {request.BudgetCategoryId} does not exist for department '{request.DepartmentCode}'.");

            // Validate terms up-front AND keep the matched objects — reused later to populate the response, no second DB call needed.
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

            // ---------------- GST split ----------------

            var vendorStateCode = await _repository.GetVendorAddressStateCodeAsync(request.VendorAddressId)
                ?? throw new ArgumentException("Invalid vendor address selected.");

            bool isIntraState = string.Equals(request.BuyerStateCode, vendorStateCode, StringComparison.OrdinalIgnoreCase);

            var items = new List<Models.PoPurchaseOrderItem>();
            decimal subTotal = 0m, gstTotal = 0m;

            foreach (var line in request.Items)
            {
                decimal amount = line.Quantity * line.UnitPrice;
                decimal gstAmount = Math.Round(amount * line.GSTRate / 100m, 2);

                var item = new Models.PoPurchaseOrderItem
                {
                    ItemDescription = line.ItemDescription,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Amount = amount,
                    GSTRate = line.GSTRate,
                    GSTType = isIntraState ? "CGST_SGST" : "IGST"
                };

                if (isIntraState)
                {
                    item.CGSTAmount = Math.Round(gstAmount / 2, 2);
                    item.SGSTAmount = gstAmount - item.CGSTAmount;
                }
                else
                {
                    item.IGSTAmount = gstAmount;
                }

                items.Add(item);
                subTotal += amount;
                gstTotal += gstAmount;
            }

            decimal totalAmount = subTotal + gstTotal;

            // ---------------- Quotation check ----------------
            bool quotationRequired = totalAmount > DefaultQuotationThreshold;
            if (quotationRequired && (request.Quotations is null || request.Quotations.Count == 0))
                throw new InvalidOperationException($"Comparative quotations are mandatory for POs above Rs. {DefaultQuotationThreshold:N0}.");

            // ---------------- Budget check ----------------

            var allocatedBudget = await _repository.GetBudgetAmountAsync(request.BudgetCategoryId, request.BudgetYear);
            var consumedBudget = await _repository.GetConsumedBudgetAsync(request.BudgetCategoryId, request.BudgetYear);
            bool budgetExceeded = allocatedBudget is null || (consumedBudget + totalAmount) > allocatedBudget;

            // ---------------- PO number ----------------

            var lastPoNumber = await _repository.GetLastPoNumberAsync(request.BudgetYear);
            int nextSeq = 1;
            if (!string.IsNullOrEmpty(lastPoNumber))
            {
                string prefix = $"PO-{request.BudgetYear}-";
                if (lastPoNumber.StartsWith(prefix) && int.TryParse(lastPoNumber.Substring(prefix.Length), out var seq))
                    nextSeq = seq + 1;
            }
            string poNumber = $"PO-{request.BudgetYear}-{nextSeq:D5}";

            var po = new Models.PoPurchaseOrder
            {
                PONumber = poNumber,
                VendorId = request.VendorId,
                VendorAddressId = request.VendorAddressId,
                DepartmentCode = request.DepartmentCode,
                Classification = request.Classification,
                BudgetYear = request.BudgetYear,
                BudgetCategoryId = request.BudgetCategoryId,
                TermsType = request.TermsType,
                AdvancePercentage = request.AdvancePercentage,
                SubTotal = subTotal,
                GSTAmount = gstTotal,
                TotalAmount = totalAmount,
                IsBudgetExceeded = budgetExceeded,
                QuotationRequired = quotationRequired,
                Status = request.SaveAsDraft ? "Draft" : "PendingApproval",
                CurrentApprovalStep = request.SaveAsDraft ? 0 : 1,
                CreatedBy = request.CreatedBy
            };

            // ---------------- Inserts ----------------

            int poId = await _repository.CreatePurchaseOrderAsync(po);

            var itemsList = items.ToList();
            await _repository.AddPurchaseOrderItemsAsync(poId, itemsList);
            var savedQuotationPaths = SaveQuotationFiles(request.Quotations, request.BudgetYear, poId);

            await _repository.AddQuotationsAsync(poId, savedQuotationPaths);

            if (selectedTerms.Count > 0)
            {
                await _repository.AddPurchaseOrderTermsAsync(poId, request.TermMasterIds, request.CreatedBy);
            }
            var cleanedAdditionalTerms = (request.AdditionalTerms ?? new List<string>())
    .Where(t => !string.IsNullOrWhiteSpace(t))
    .Select(t => t.Trim())
    .ToList();

            if (cleanedAdditionalTerms.Count > 0)
            {
                await _repository.AddAdditionalTermsAsync(poId, cleanedAdditionalTerms, request.CreatedBy);
            }

            if (!request.SaveAsDraft)
            {
                var roles = new List<string> { "DepartmentHead", "FinancialController" };
                if (budgetExceeded) roles.Add("CFO");

                var steps = roles.Select((role, i) => new Models.PoPurchaseOrderApprovalHistory
                {
                    StepNumber = i + 1,
                    ApproverRole = role
                }).ToList();

                await _repository.AddApprovalStepsAsync(poId, steps);
            }

            // ---------------- Response ----------------

            return new PurchaseOrderResponse
            {
                POId = poId,
                PONumber = poNumber,
                DepartmentCode = request.DepartmentCode,
                Classification = request.Classification,
                BudgetYear = request.BudgetYear,
                SubTotal = subTotal,
                GSTAmount = gstTotal,
                TotalAmount = totalAmount,
                IsBudgetExceeded = budgetExceeded,
                QuotationRequired = quotationRequired,
                Status = po.Status,
                Items = itemsList.Select(i => new PurchaseOrderItemResponse
                {
                    ItemDescription = i.ItemDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount,
                    GSTRate = i.GSTRate,
                    GSTType = i.GSTType,
                    CGSTAmount = i.CGSTAmount,
                    SGSTAmount = i.SGSTAmount,
                    IGSTAmount = i.IGSTAmount
                }).ToList(),
                Terms = selectedTerms.Select(t => new PurchaseOrderTermResponse   // <-- now populated from the same data used for validation
                {
                    TermMasterId = t.TermMasterId,
                    TermType = t.TermType,
                    TermCategory = t.TermCategory,
                    TermsAndConditionDetails = t.TermsAndConditionDetails
                }).ToList(),
                AdditionalTerms = cleanedAdditionalTerms.Select((text, i) => new PurchaseOrderAdditionalTermResponse
                {
                    POAdditionalTermId = 0,   // not known until re-fetched; fine for immediate create-response echo
                    TermText = text
                }).ToList()
            };
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
        public async Task<PurchaseOrderResponse> GetPurchaseOrderByIdAsync(int poId)
        {
            var po = await _repository.GetPurchaseOrderByIdAsync(poId)
                ?? throw new KeyNotFoundException($"Purchase Order {poId} not found.");

            var items = await _repository.GetPurchaseOrderItemsAsync(poId);
            var terms = await _repository.GetPurchaseOrderTermsAsync(poId);
            var additionalTerms = await _repository.GetAdditionalTermsAsync(poId);
            var quotationPaths = await _repository.GetQuotationFilePathsAsync(poId);


            return new PurchaseOrderResponse
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
                Status = po.Status,
                Items = items.Select(i => new PurchaseOrderItemResponse
                {
                    ItemDescription = i.ItemDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount,
                    GSTRate = i.GSTRate,
                    GSTType = i.GSTType,
                    CGSTAmount = i.CGSTAmount,
                    SGSTAmount = i.SGSTAmount,
                    IGSTAmount = i.IGSTAmount
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
                Quotations = LoadQuotationFiles(quotationPaths)
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

                // e.g. 6_Quotation1.pdf, 6_Quotation2.pdf
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
                if (!File.Exists(path))
                {
                    // File missing on disk (moved/deleted outside the app) — skip rather than fail the whole GET
                    continue;
                }

                byte[] fileBytes = File.ReadAllBytes(path);

                results.Add(new QuotationFileResponse
                {
                    FileName = Path.GetFileName(path),          // e.g. "6_Quotation1.pdf"
                    Base64Content = Convert.ToBase64String(fileBytes)
                });
            }

            return results;
        }
    }
}
