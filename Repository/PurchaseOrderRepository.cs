using Dapper;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public sealed class PurchaseOrderRepository
    {
        private readonly string _conStr;
       

        public PurchaseOrderRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")
                ?? throw new InvalidOperationException("DBConn connection string is missing.");
        }
        // ---------------- User Access ----------------
        public async Task<PoUser?> GetActiveUserByEmployeeCodeAsync(string employeeCode)
        {
            const string sql = @"
                SELECT [Id], [EmployeeCode], [Name], [DepartmentCode], [IsDepartmentHead], [IsActive]
                FROM [dbo].[PO_Users]
                WHERE [EmployeeCode] = @EmployeeCode AND [IsActive] = 1;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryFirstOrDefaultAsync<PoUser>(sql, new { EmployeeCode = employeeCode });
        }

        public async Task<IEnumerable<string>> GetActiveRoleNamesForUserAsync(int userId)
        {
            const string sql = @"
        SELECT r.[RoleName]
        FROM [dbo].[PO_UserRoles] ur
        INNER JOIN [dbo].[PO_Roles] r ON r.[Id] = ur.[RoleId]
        WHERE ur.[UserId] = @UserId AND r.[Status] = 1;";
            //--was: r.[Status] = 'Active'

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<string>(sql, new { UserId = userId });
        }
        // ---------------- Vendor Master ----------------

      
        public async Task<IEnumerable<POVendorMaster>> GetAllVendorsAsync(string? status = null)
        {
            const string sql = @"
                SELECT [VendorId], [VendorCode], [VendorName], [MSMEVendor], [Status]
                FROM [dbo].[PO_VendorMaster]
                WHERE (@Status IS NULL OR [Status] = @Status)
                ORDER BY [VendorName];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<POVendorMaster>(sql, new { Status = status });
        }
     
        public async Task<(POVendorMaster? Vendor, IEnumerable<POVendorAddressMaster> Addresses)> GetVendorByIdAsync(int vendorId)
        {
            const string sql = @"
                SELECT [VendorId], [VendorCode], [VendorName], [BankHolderName], [BankAccountNo],
                       [BankName], [IFSCCode], [MSMEVendor], [UDYAMRegnNo], [VendorPAN],
                       [Status], [CreatedBy], [CreatedDate], [ModifiedBy], [ModifiedDate]
                FROM [dbo].[PO_VendorMaster]
                WHERE [VendorId] = @VendorId;

                SELECT [VendorAddressId], [VendorId], [AddressLine1], [City], [StateName],
                       [StateCode], [PinCode], [GSTIN], [IsPrimaryAddress], [Status]
                FROM [dbo].[PO_VendorAddressMaster]
                WHERE [VendorId] = @VendorId
                ORDER BY [IsPrimaryAddress] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            using var multi = await connection.QueryMultipleAsync(sql, new { VendorId = vendorId });
            var vendor = await multi.ReadFirstOrDefaultAsync<POVendorMaster>();
            var addresses = await multi.ReadAsync<POVendorAddressMaster>();

            return (vendor, addresses);
        }
        public async Task<IEnumerable<PoTermsConditionsMaster>> GetTermsMasterAsync(string? termCategory = null)
        {
            const string sql = @"
        SELECT [TermMasterId], [TermType], [TermCategory], [TermsAndConditionDetails], [IsActive]
        FROM [dbo].[PO_TermsConditionsMaster]
        WHERE [IsActive] = 1
          AND (@TermCategory IS NULL OR [TermCategory] = @TermCategory)
        ORDER BY [TermCategory], [TermType];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoTermsConditionsMaster>(sql, new { TermCategory = termCategory });
        }

        // ---------------- Department / Budget (used by PO creation) ----------------

        public async Task<IEnumerable<PoDepartment>> GetDepartmentsAsync()
        {
            const string sql = @"
                SELECT [Id], [Department], [DepartmentCode], [ActiveStatus]
                FROM [dbo].[PO_DepartmentsMaster]
                WHERE [ActiveStatus] = 1
                ORDER BY [Department];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoDepartment>(sql);
        }


        /// <summary>Admin view: full department-wise budget list, optionally scoped to a financial year (e.g. "FY2026-27").</summary>
        public async Task<IEnumerable<PoDeptWiseBudget>> GetBudgetAsync(string? budgetYear = null)
        {
            const string sql = @"
        SELECT
            b.[Id],
            bc.[DepartmentCode],
            bc.[Classification],
            bc.[SubClassification],
            bc.[ItemCategory],
            b.[BudgetYear],
            b.[Budget],
            b.[IsActive]
        FROM [dbo].[PO_DeptWise_Budget] b
        INNER JOIN [dbo].[PO_BudgetCategoryMaster] bc ON bc.[BudgetCategoryId] = b.[BudgetCategoryId]
        WHERE b.[IsActive] = 1
          AND (@BudgetYear IS NULL OR b.[BudgetYear] = @BudgetYear)
        ORDER BY bc.[DepartmentCode], bc.[Classification], bc.[SubClassification], bc.[ItemCategory];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoDeptWiseBudget>(sql, new { BudgetYear = budgetYear });
        }

        public async Task<IEnumerable<PoDeptWiseBudget>> GetDepartmentWiseBudgetAsync(
            string? department, string? budgetYear, string? classification)
        {
            const string sql = @"
        SELECT [Id], [Department], [Classification], [SubClassification], [ItemCategory],
               [BudgetYear], [Budget], [IsActive]
        FROM [dbo].[PO_DeptWise_Budget]
        WHERE [IsActive] = 1
          AND (@Department IS NULL OR [Department] = @Department)
          AND (@BudgetYear IS NULL OR [BudgetYear] = @BudgetYear)
          AND (@Classification IS NULL OR [Classification] = @Classification)
        ORDER BY [Department], [Classification];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoDeptWiseBudget>(sql, new
            {
                Department = department,
                BudgetYear = budgetYear,
                Classification = classification
            });
        }




        // ---------------- Purchase Order Creation ----------------

        public async Task<string?> GetVendorAddressStateCodeAsync(long vendorAddressId)
        {
            const string sql = "SELECT [StateCode] FROM [dbo].[PO_VendorAddressMaster] WHERE [VendorAddressId] = @VendorAddressId;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<string?>(sql, new { VendorAddressId = vendorAddressId });
        }

        // ---------------- Budget Check (BudgetCategoryId already implies department) ----------------

        public async Task<decimal?> GetBudgetAmountAsync(int budgetCategoryId, string budgetYear)
        {
            const string sql = @"
        SELECT TOP 1 [Budget]
        FROM [dbo].[PO_DeptWise_Budget]
        WHERE [BudgetCategoryId] = @BudgetCategoryId
          AND [BudgetYear] = @BudgetYear
          AND [IsActive] = 1;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<decimal?>(sql, new { BudgetCategoryId = budgetCategoryId, BudgetYear = budgetYear });
        }

        public async Task<decimal> GetConsumedBudgetAsync(int budgetCategoryId, string budgetYear)
        {
            const string sql = @"
        SELECT ISNULL(SUM([TotalAmount]), 0)
        FROM [dbo].[PO_PurchaseOrder]
        WHERE [BudgetCategoryId] = @BudgetCategoryId
          AND [BudgetYear] = @BudgetYear
          AND [Status] = 'Approved';";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<decimal>(sql, new { BudgetCategoryId = budgetCategoryId, BudgetYear = budgetYear });
        }

        public async Task<string?> GetLastPoNumberAsync(string budgetYear)
        {
            const string sql = @"
        SELECT TOP 1 [PONumber] FROM [dbo].[PO_PurchaseOrder]
        WHERE [PONumber] LIKE 'PO-' + @BudgetYear + '-%'
        ORDER BY [POId] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<string?>(sql, new { BudgetYear = budgetYear });
        }

        public async Task<int> CreatePurchaseOrderAsync(PoPurchaseOrder po)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_PurchaseOrder]
            ([PONumber], [VendorId], [VendorAddressId], [DepartmentCode], [Classification], [SubClassification], [BudgetYear],
             [POType], [TermsType], [AdvancePercentage],
             [VendorContactName], [VendorContactMobile], [VendorContactEmail],
             [SubTotal], [GSTAmount], [TotalAmount],
             [IsBudgetExceeded], [QuotationRequired], [Status], [CurrentApprovalStep], [CreatedBy], [CreatedDt])
        OUTPUT INSERTED.[POId]
        VALUES
            (@PONumber, @VendorId, @VendorAddressId, @DepartmentCode, @Classification, @SubClassification, @BudgetYear,
             @POType, @TermsType, @AdvancePercentage,
             @VendorContactName, @VendorContactMobile, @VendorContactEmail,
             @SubTotal, @GSTAmount, @TotalAmount,
             @IsBudgetExceeded, @QuotationRequired, @Status, @CurrentApprovalStep, @CreatedBy, GETDATE());";


            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, po);
        }

        public async Task AddPurchaseOrderItemsAsync(int poId, IEnumerable<PoPurchaseOrderItem> items)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_PurchaseOrderItems]
            ([POId], [ItemDescription], [Quantity], [UnitPrice], [Amount], [GSTRate], [GSTAmount], [BudgetCategoryId], [IsBudgetExceeded])
        VALUES
            (@POId, @ItemDescription, @Quantity, @UnitPrice, @Amount, @GSTRate, @GSTAmount, @BudgetCategoryId, @IsBudgetExceeded);";


            using IDbConnection connection = new SqlConnection(_conStr);
            foreach (var item in items) item.POId = poId;
            await connection.ExecuteAsync(sql, items);
        }

        public async Task AddQuotationsAsync(int poId, IEnumerable<string> filePaths)
        {
            if (!filePaths.Any()) return;

            const string sql = @"
        INSERT INTO [dbo].[PO_PurchaseOrderQuotations] ([POId], [FilePath], [UploadedDt])
        VALUES (@POId, @FilePath, GETDATE());";

            using IDbConnection connection = new SqlConnection(_conStr);
            await connection.ExecuteAsync(sql, filePaths.Select(f => new { POId = poId, FilePath = f }));
        }
        public async Task AddPurchaseOrderTermsAsync(int poId, IEnumerable<long> termMasterIds, string createdBy)
        {
            var ids = termMasterIds.ToList();
            if (ids.Count == 0) return;

            const string sql = @"
        INSERT INTO [dbo].[PO_PurchaseOrderTerms] ([POId], [TermMasterId], [CreatedBy], [CreatedDt])
        VALUES (@POId, @TermMasterId, @CreatedBy, GETDATE());";

            using IDbConnection connection = new SqlConnection(_conStr);
            var rows = ids.Select(id => new { POId = poId, TermMasterId = id, CreatedBy = createdBy }).ToList();
            await connection.ExecuteAsync(sql, rows);
        }

        public async Task AddApprovalStepsAsync(int poId, IEnumerable<PoPurchaseOrderApprovalHistory> steps)
        {
            var stepList = steps.ToList();   // materialize defensively — protects against any future lazy-IEnumerable caller

            const string sql = @"
        INSERT INTO [dbo].[PO_PurchaseOrderApprovalHistory] ([POId], [StepNumber], [ApproverRole])
        VALUES (@POId, @StepNumber, @ApproverRole);";

            using IDbConnection connection = new SqlConnection(_conStr);
            foreach (var step in stepList) step.POId = poId;
            await connection.ExecuteAsync(sql, stepList);
        }
        public async Task<IEnumerable<PoDeptWiseBudget>> GetBudgetLinesAsync(string departmentCode, string classification, string budgetYear)
        {
            const string sql = @"
        SELECT [Id], [Department], [Classification], [SubClassification], [ItemCategory], [BudgetYear], [Budget], [IsActive]
        FROM [dbo].[PO_DeptWise_Budget]
        WHERE [Department] = @DepartmentCode
          AND [Classification] = @Classification
          AND [BudgetYear] = @BudgetYear
          AND [IsActive] = 1
        ORDER BY [SubClassification], [ItemCategory];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoDeptWiseBudget>(sql,
                new { DepartmentCode = departmentCode, Classification = classification, BudgetYear = budgetYear });
        }

        // ---------------- Budget Category Master (scoped by Department) ----------------

        public async Task<IEnumerable<PoBudgetCategory>> GetBudgetCategoriesAsync(string departmentCode, string? classification = null)
        {
            const string sql = @"
        SELECT [BudgetCategoryId], [DepartmentCode], [Classification], [SubClassification], [ItemCategory], [IsActive]
        FROM [dbo].[PO_BudgetCategoryMaster]
        WHERE [DepartmentCode] = @DepartmentCode
          AND [IsActive] = 1
          AND (@Classification IS NULL OR [Classification] = @Classification)
        ORDER BY [Classification], [SubClassification], [ItemCategory];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoBudgetCategory>(sql, new { DepartmentCode = departmentCode, Classification = classification });
        }

        // ---------------- Purchase Order Retrieval ----------------

        public async Task<PoPurchaseOrder?> GetPurchaseOrderByIdAsync(int poId)
        {
            const string sql = "SELECT * FROM [dbo].[PO_PurchaseOrder] WHERE [POId] = @POId;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryFirstOrDefaultAsync<PoPurchaseOrder>(sql, new { POId = poId });
        }

        public async Task<IEnumerable<PoPurchaseOrderItem>> GetPurchaseOrderItemsAsync(int poId)
        {
            const string sql = "SELECT * FROM [dbo].[PO_PurchaseOrderItems] WHERE [POId] = @POId;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrderItem>(sql, new { POId = poId });
        }
        public async Task<IEnumerable<PoTermsConditionsMaster>> GetPurchaseOrderTermsAsync(int poId)
        {
            const string sql = @"
        SELECT tm.[TermMasterId], tm.[TermType], tm.[TermCategory], tm.[TermsAndConditionDetails], tm.[IsActive]
        FROM [dbo].[PO_PurchaseOrderTerms] pot
        INNER JOIN [dbo].[PO_TermsConditionsMaster] tm ON tm.[TermMasterId] = pot.[TermMasterId]
        WHERE pot.[POId] = @POId;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoTermsConditionsMaster>(sql, new { POId = poId });
        }
        public async Task<IEnumerable<PoPurchaseOrder>> GetPurchaseOrderListAsync(string? departmentCode, string? status)
        {
            const string sql = @"
        SELECT * FROM [dbo].[PO_PurchaseOrder]
        WHERE (@DepartmentCode IS NULL OR [DepartmentCode] = @DepartmentCode)
          AND (@Status IS NULL OR [Status] = @Status)
        ORDER BY [CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrder>(sql, new { DepartmentCode = departmentCode, Status = status });
        }

        public async Task<bool> VendorExistsAsync(long vendorId)
        {
            const string sql = "SELECT COUNT(1) FROM [dbo].[PO_VendorMaster] WHERE [VendorId] = @VendorId AND [Status] = 'ACTIVE';";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, new { VendorId = vendorId }) > 0;
        }

        public async Task<bool> VendorAddressExistsAsync(long vendorAddressId, long vendorId)
        {
            const string sql = "SELECT COUNT(1) FROM [dbo].[PO_VendorAddressMaster] WHERE [VendorAddressId] = @VendorAddressId AND [VendorId] = @VendorId;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, new { VendorAddressId = vendorAddressId, VendorId = vendorId }) > 0;
        }

        public async Task<bool> DepartmentExistsAsync(string departmentCode)
        {
            const string sql = "SELECT COUNT(1) FROM [dbo].[PO_DepartmentsMaster] WHERE [DepartmentCode] = @DepartmentCode AND [ActiveStatus] = 1;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, new { DepartmentCode = departmentCode }) > 0;
        }

        public async Task<bool> BudgetCategoryExistsAsync(int budgetCategoryId, string departmentCode)
        {
            const string sql = @"
        SELECT COUNT(1) FROM [dbo].[PO_BudgetCategoryMaster]
        WHERE [BudgetCategoryId] = @BudgetCategoryId AND [DepartmentCode] = @DepartmentCode AND [IsActive] = 1;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, new { BudgetCategoryId = budgetCategoryId, DepartmentCode = departmentCode }) > 0;
        }
        public async Task AddAdditionalTermsAsync(int poId, IEnumerable<string> termTexts, string createdBy)
        {
            var texts = termTexts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (texts.Count == 0) return;

            const string sql = @"
        INSERT INTO [dbo].[PO_PurchaseOrderAdditionalTerms] ([POId], [TermText], [CreatedBy], [CreatedDt])
        VALUES (@POId, @TermText, @CreatedBy, GETDATE());";

            using IDbConnection connection = new SqlConnection(_conStr);
            var rows = texts.Select(t => new { POId = poId, TermText = t, CreatedBy = createdBy }).ToList();
            await connection.ExecuteAsync(sql, rows);
        }

        public async Task<IEnumerable<PoPurchaseOrderAdditionalTerm>> GetAdditionalTermsAsync(int poId)
        {
            const string sql = "SELECT * FROM [dbo].[PO_PurchaseOrderAdditionalTerms] WHERE [POId] = @POId;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrderAdditionalTerm>(sql, new { POId = poId });
        }
        public async Task<IEnumerable<string>> GetQuotationFilePathsAsync(int poId)
        {
            const string sql = "SELECT [FilePath] FROM [dbo].[PO_PurchaseOrderQuotations] WHERE [POId] = @POId;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<string>(sql, new { POId = poId });
        }

        public async Task<IEnumerable<PoPurchaseOrder>> GetAllPurchaseOrdersAsync()
        {
            const string sql = "SELECT * FROM [dbo].[PO_PurchaseOrder] ORDER BY [CreatedDt] DESC;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrder>(sql);
        }

        public async Task<IEnumerable<PoPurchaseOrder>> GetPurchaseOrdersPendingApprovalForRoleAsync(string approverRole)
        {
            const string sql = @"
        SELECT po.*
        FROM [dbo].[PO_PurchaseOrder] po
        INNER JOIN [dbo].[PO_PurchaseOrderApprovalHistory] ah
            ON ah.[POId] = po.[POId] AND ah.[StepNumber] = po.[CurrentApprovalStep]
        WHERE po.[Status] = 'PendingApproval'
          AND ah.[ApproverRole] = @ApproverRole
        ORDER BY po.[CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrder>(sql, new { ApproverRole = approverRole });
        }

        public async Task<IEnumerable<PoPurchaseOrder>> GetPurchaseOrdersByDepartmentAsync(string departmentCode)
        {
            const string sql = @"
        SELECT * FROM [dbo].[PO_PurchaseOrder]
        WHERE [DepartmentCode] = @DepartmentCode
        ORDER BY [CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrder>(sql, new { DepartmentCode = departmentCode });
        }
        public async Task<IEnumerable<PoPurchaseOrderApprovalHistory>> GetApprovalHistoryForPOsAsync(IEnumerable<int> poIds)
        {
            var ids = poIds.ToList();
            if (ids.Count == 0) return Enumerable.Empty<PoPurchaseOrderApprovalHistory>();

            const string sql = @"
        SELECT * FROM [dbo].[PO_PurchaseOrderApprovalHistory]
        WHERE [POId] IN @POIds
        ORDER BY [POId], [StepNumber];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrderApprovalHistory>(sql, new { POIds = ids });
        }
        public async Task<IEnumerable<PoPurchaseOrderApprovalHistory>> GetApprovalHistoryAsync(int poId)
        {
            const string sql = @"
        SELECT * FROM [dbo].[PO_PurchaseOrderApprovalHistory]
        WHERE [POId] = @POId
        ORDER BY [StepNumber];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrderApprovalHistory>(sql, new { POId = poId });
        }
        public async Task<IEnumerable<PoUser>> GetUsersByRoleAsync(string roleName, string? departmentCode = null)
        {
            const string sql = @"
        SELECT u.[Id], u.[EmployeeCode], u.[Name], u.[DepartmentCode], u.[IsDepartmentHead], u.[IsActive]
        FROM [dbo].[PO_Users] u
        INNER JOIN [dbo].[PO_UserRoles] ur ON ur.[UserId] = u.[Id]
        INNER JOIN [dbo].[PO_Roles] r ON r.[Id] = ur.[RoleId]
        WHERE r.[RoleName] = @RoleName
          AND r.[Status] = 1                          -- was: r.[Status] = 'Active'
          AND u.[IsActive] = 1
          AND (@DepartmentCode IS NULL OR u.[DepartmentCode] = @DepartmentCode);";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoUser>(sql, new { RoleName = roleName, DepartmentCode = departmentCode });
        }

        public async Task<IEnumerable<PoPurchaseOrder>> GetPendingWithRolesAsync(IEnumerable<string> roles, string userDepartmentCode)
        {
            const string sql = @"
        SELECT DISTINCT po.*
        FROM [dbo].[PO_PurchaseOrder] po
        INNER JOIN [dbo].[PO_PurchaseOrderApprovalHistory] ah
            ON ah.[POId] = po.[POId] AND ah.[StepNumber] = po.[CurrentApprovalStep]
        WHERE po.[Status] = 'PendingApproval'
          AND ah.[ApproverRole] IN @Roles
          AND (ah.[ApproverRole] <> 'DepartmentHead' OR po.[DepartmentCode] = @UserDepartmentCode)
        ORDER BY po.[CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrder>(sql, new { Roles = roles.ToList(), UserDepartmentCode = userDepartmentCode });
        }

        public async Task<IEnumerable<PoPurchaseOrder>> GetAllPendingAsync()
        {
            const string sql = "SELECT * FROM [dbo].[PO_PurchaseOrder] WHERE [Status] = 'PendingApproval' ORDER BY [CreatedDt] DESC;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrder>(sql);
        }

        public async Task<IEnumerable<PoPurchaseOrder>> GetInProgressByDepartmentAsync(string departmentCode)
        {
            const string sql = @"
        SELECT * FROM [dbo].[PO_PurchaseOrder]
        WHERE [DepartmentCode] = @DepartmentCode AND [Status] = 'PendingApproval'
        ORDER BY [CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrder>(sql, new { DepartmentCode = departmentCode });
        }

        public async Task<IEnumerable<PoPurchaseOrder>> GetCompletedByDepartmentAsync(string departmentCode)
        {
            const string sql = @"
        SELECT * FROM [dbo].[PO_PurchaseOrder]
        WHERE [DepartmentCode] = @DepartmentCode AND [Status] IN ('Approved','Rejected','Cancelled')
        ORDER BY [ModifiedDt] DESC, [CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrder>(sql, new { DepartmentCode = departmentCode });
        }

        public async Task<IEnumerable<PoPurchaseOrder>> GetAllCompletedAsync()
        {
            const string sql = @"
        SELECT * FROM [dbo].[PO_PurchaseOrder]
        WHERE [Status] IN ('Approved','Rejected','Cancelled')
        ORDER BY [ModifiedDt] DESC, [CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoPurchaseOrder>(sql);
        }

        public async Task<dynamic?> GetVendorDetailsForPOAsync(int poId)
        {
            const string sql = @"
        SELECT v.[VendorName], v.[VendorCode], va.[GSTIN], va.[AddressLine1], va.[City], va.[StateName]
        FROM [dbo].[PO_PurchaseOrder] po
        INNER JOIN [dbo].[PO_VendorMaster] v ON v.[VendorId] = po.[VendorId]
        INNER JOIN [dbo].[PO_VendorAddressMaster] va ON va.[VendorAddressId] = po.[VendorAddressId]
        WHERE po.[POId] = @POId;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryFirstOrDefaultAsync(sql, new { POId = poId });
        }

    

        public async Task<bool> AllLocationsExistAsync(IEnumerable<int> locationIds)
        {
            var ids = locationIds.Distinct().ToList();
            if (ids.Count == 0) return false;

            const string sql = "SELECT COUNT(1) FROM [dbo].[PO_Location] WHERE [LocationId] IN @Ids AND [IsActive] = 1;";
            using IDbConnection connection = new SqlConnection(_conStr);
            int matched = await connection.ExecuteScalarAsync<int>(sql, new { Ids = ids });
            return matched == ids.Count;
        }

        public async Task AddPurchaseOrderDeliveryLocationsAsync(int poId, IEnumerable<int> locationIds, string createdBy)
        {
            var ids = locationIds.Distinct().ToList();
            if (ids.Count == 0) return;

            const string sql = @"
        INSERT INTO [dbo].[PO_PurchaseOrderDeliveryLocations] ([POId], [LocationId], [CreatedBy], [CreatedDt])
        VALUES (@POId, @LocationId, @CreatedBy, GETDATE());";

            using IDbConnection connection = new SqlConnection(_conStr);
            var rows = ids.Select(id => new { POId = poId, LocationId = id, CreatedBy = createdBy }).ToList();
            await connection.ExecuteAsync(sql, rows);
        }

        public async Task<IEnumerable<PoLocation>> GetLocationsAsync(string? region = null, string? zone = null)
        {
            const string sql = @"
        SELECT [LocationId], [BranchCode], [BranchName], [Address], [Region], [Zone], [MegaZone], [IsActive]
        FROM [dbo].[PO_Location]
        WHERE [IsActive] = 1
          AND (@Region IS NULL OR [Region] = @Region)
          AND (@Zone IS NULL OR [Zone] = @Zone)
        ORDER BY [MegaZone], [Zone], [Region], [BranchName];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoLocation>(sql, new { Region = region, Zone = zone });
        }

        public async Task<IEnumerable<PoLocation>> GetPurchaseOrderDeliveryLocationsAsync(int poId)
        {
            const string sql = @"
        SELECT l.[LocationId], l.[BranchCode], l.[BranchName], l.[Address], l.[Region], l.[Zone], l.[MegaZone], l.[IsActive]
        FROM [dbo].[PO_PurchaseOrderDeliveryLocations] pdl
        INNER JOIN [dbo].[PO_Location] l ON l.[LocationId] = pdl.[LocationId]
        WHERE pdl.[POId] = @POId;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoLocation>(sql, new { POId = poId });
        }
        public async Task<PoPurchaseOrderApprovalHistory?> GetPendingApprovalStepAsync(int poId, int stepNumber)
        {
            const string sql = @"
        SELECT * FROM [dbo].[PO_PurchaseOrderApprovalHistory]
        WHERE [POId] = @POId AND [StepNumber] = @StepNumber;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryFirstOrDefaultAsync<PoPurchaseOrderApprovalHistory>(sql, new { POId = poId, StepNumber = stepNumber });
        }

        public async Task MarkApprovalStepActionedAsync(int approvalId, string approverEmployeeCode, string action, string? remarks)
        {
            const string sql = @"
        UPDATE [dbo].[PO_PurchaseOrderApprovalHistory]
        SET [ApproverEmployeeCode] = @ApproverEmployeeCode, [Action] = @Action, [Remarks] = @Remarks, [ActionedDt] = GETDATE()
        WHERE [ApprovalId] = @ApprovalId;";

            using IDbConnection connection = new SqlConnection(_conStr);
            await connection.ExecuteAsync(sql, new { ApprovalId = approvalId, ApproverEmployeeCode = approverEmployeeCode, Action = action, Remarks = remarks });
        }

        public async Task UpdatePOStatusAsync(int poId, string status, int currentApprovalStep)
        {
            const string sql = @"
        UPDATE [dbo].[PO_PurchaseOrder]
        SET [Status] = @Status, [CurrentApprovalStep] = @CurrentApprovalStep, [ModifiedDt] = GETDATE()
        WHERE [POId] = @POId;";

            using IDbConnection connection = new SqlConnection(_conStr);
            await connection.ExecuteAsync(sql, new { POId = poId, Status = status, CurrentApprovalStep = currentApprovalStep });
        }

        public async Task<Dictionary<int, decimal>> GetBudgetAmountsAsync(IEnumerable<int> budgetCategoryIds, string budgetYear)
        {
            var ids = budgetCategoryIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, decimal>();

            const string sql = @"
        SELECT [BudgetCategoryId], [Budget]
        FROM [dbo].[PO_DeptWise_Budget]
        WHERE [BudgetCategoryId] IN @Ids AND [BudgetYear] = @BudgetYear AND [IsActive] = 1;";

            using IDbConnection connection = new SqlConnection(_conStr);
            var rows = await connection.QueryAsync<(int BudgetCategoryId, decimal Budget)>(sql, new { Ids = ids, BudgetYear = budgetYear });
            return rows.ToDictionary(r => r.BudgetCategoryId, r => r.Budget);
        }
        public async Task<Dictionary<int, decimal>> GetConsumedBudgetsAsync(IEnumerable<int> budgetCategoryIds, string budgetYear)
        {
            var ids = budgetCategoryIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, decimal>();

            const string sql = @"
        SELECT poi.[BudgetCategoryId], ISNULL(SUM(poi.[Amount] + poi.[GSTAmount]), 0) AS Consumed
        FROM [dbo].[PO_PurchaseOrderItems] poi
        INNER JOIN [dbo].[PO_PurchaseOrder] po ON po.[POId] = poi.[POId]
        WHERE poi.[BudgetCategoryId] IN @Ids
          AND po.[BudgetYear] = @BudgetYear
          AND po.[Status] = 'Approved'
        GROUP BY poi.[BudgetCategoryId];";

            using IDbConnection connection = new SqlConnection(_conStr);
            var rows = await connection.QueryAsync<(int BudgetCategoryId, decimal Consumed)>(sql, new { Ids = ids, BudgetYear = budgetYear });
            return rows.ToDictionary(r => r.BudgetCategoryId, r => r.Consumed);
        }
        public async Task<IEnumerable<dynamic>> GetPurchaseOrderItemsWithCategoryAsync(int poId)
        {
            const string sql = @"
        SELECT poi.*, bc.[SubClassification], bc.[ItemCategory]
        FROM [dbo].[PO_PurchaseOrderItems] poi
        LEFT JOIN [dbo].[PO_BudgetCategoryMaster] bc ON bc.[BudgetCategoryId] = poi.[BudgetCategoryId]
        WHERE poi.[POId] = @POId;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql, new { POId = poId });
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            const string sql = "SELECT [SettingValue] FROM [dbo].[PO_AppSettings] WHERE [SettingKey] = @Key;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<string?>(sql, new { Key = key });
        }

        public async Task<Dictionary<string, string>> GetSettingsAsync(IEnumerable<string> keys)
        {
            const string sql = "SELECT [SettingKey], [SettingValue] FROM [dbo].[PO_AppSettings] WHERE [SettingKey] IN @Keys;";
            using IDbConnection connection = new SqlConnection(_conStr);
            var rows = await connection.QueryAsync<(string SettingKey, string SettingValue)>(sql, new { Keys = keys.ToList() });
            return rows.ToDictionary(r => r.SettingKey, r => r.SettingValue);
        }

        public async Task UpdateSettingAsync(string key, string value, string modifiedBy)
        {
            const string sql = @"
        UPDATE [dbo].[PO_AppSettings]
        SET [SettingValue] = @Value, [ModifiedBy] = @ModifiedBy, [ModifiedDt] = GETDATE()
        WHERE [SettingKey] = @Key;";

            using IDbConnection connection = new SqlConnection(_conStr);
            int rows = await connection.ExecuteAsync(sql, new { Key = key, Value = value, ModifiedBy = modifiedBy });
            if (rows == 0)
                throw new KeyNotFoundException($"Setting '{key}' does not exist.");
        }
        public async Task<IEnumerable<(string SettingKey, string SettingValue, string? Description)>> GetAllSettingsAsync()
        {
            const string sql = "SELECT [SettingKey], [SettingValue], [Description] FROM [dbo].[PO_AppSettings];";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<(string, string, string?)>(sql);
        }
        public async Task<IEnumerable<string>> GetSubClassificationsAsync(string departmentCode, string classification)
        {
            const string sql = @"
        SELECT DISTINCT [SubClassification]
        FROM [dbo].[PO_BudgetCategoryMaster]
        WHERE [DepartmentCode] = @DepartmentCode
          AND [Classification] = @Classification
          AND [IsActive] = 1
        ORDER BY [SubClassification];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<string>(sql, new { DepartmentCode = departmentCode, Classification = classification });
        }
        public async Task<bool> SubClassificationExistsAsync(string departmentCode, string classification, string subClassification)
        {
            const string sql = @"
        SELECT COUNT(1) FROM [dbo].[PO_BudgetCategoryMaster]
        WHERE [DepartmentCode] = @DepartmentCode
          AND [Classification] = @Classification
          AND [SubClassification] = @SubClassification
          AND [IsActive] = 1;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, new { DepartmentCode = departmentCode, Classification = classification, SubClassification = subClassification }) > 0;
        }
        public async Task<IEnumerable<dynamic>> GetDepartmentBudgetUtilizationAsync(string departmentCode, string budgetYear)
        {
            const string sql = @"
        SELECT
            bc.[BudgetCategoryId],
            bc.[Classification],
            bc.[SubClassification],
            bc.[ItemCategory],
            b.[Budget]              AS AllocatedAmount,
            ISNULL(consumed.UsedAmount, 0) AS UsedAmount
        FROM [dbo].[PO_BudgetCategoryMaster] bc
        INNER JOIN [dbo].[PO_DeptWise_Budget] b
            ON b.[BudgetCategoryId] = bc.[BudgetCategoryId] AND b.[BudgetYear] = @BudgetYear AND b.[IsActive] = 1
        LEFT JOIN (
            SELECT poi.[BudgetCategoryId], SUM(poi.[Amount] + poi.[GSTAmount]) AS UsedAmount
            FROM [dbo].[PO_PurchaseOrderItems] poi
            INNER JOIN [dbo].[PO_PurchaseOrder] po ON po.[POId] = poi.[POId]
            WHERE po.[BudgetYear] = @BudgetYear AND po.[Status] = 'Approved'
            GROUP BY poi.[BudgetCategoryId]
        ) consumed ON consumed.[BudgetCategoryId] = bc.[BudgetCategoryId]
        WHERE bc.[DepartmentCode] = @DepartmentCode
          AND bc.[IsActive] = 1
        ORDER BY bc.[Classification], bc.[SubClassification], bc.[ItemCategory];";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql, new { DepartmentCode = departmentCode, BudgetYear = budgetYear });
        }
    }
}
