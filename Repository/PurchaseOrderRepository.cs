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
            ([PONumber], [VendorId], [VendorAddressId], [DepartmentCode], [Classification], [BudgetYear],
             [BudgetCategoryId], [TermsType], [AdvancePercentage],
             [SubTotal], [GSTAmount], [TotalAmount],
             [IsBudgetExceeded], [QuotationRequired], [Status], [CurrentApprovalStep], [CreatedBy], [CreatedDt])
        OUTPUT INSERTED.[POId]
        VALUES
            (@PONumber, @VendorId, @VendorAddressId, @DepartmentCode, @Classification, @BudgetYear,
             @BudgetCategoryId, @TermsType, @AdvancePercentage,
             @SubTotal, @GSTAmount, @TotalAmount,
             @IsBudgetExceeded, @QuotationRequired, @Status, @CurrentApprovalStep, @CreatedBy, GETDATE());";


            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, po);
        }

        public async Task AddPurchaseOrderItemsAsync(int poId, IEnumerable<PoPurchaseOrderItem> items)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_PurchaseOrderItems]
            ([POId], [ItemDescription], [Quantity], [UnitPrice], [Amount], [GSTRate], [GSTType], [CGSTAmount], [SGSTAmount], [IGSTAmount])
        VALUES
            (@POId, @ItemDescription, @Quantity, @UnitPrice, @Amount, @GSTRate, @GSTType, @CGSTAmount, @SGSTAmount, @IGSTAmount);";

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



    }
}
