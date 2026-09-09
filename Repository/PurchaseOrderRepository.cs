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
        SELECT [Id], [Department], [Classification], [SubClassification], [ItemCategory],
               [BudgetYear], [Budget], [IsActive]
        FROM [dbo].[PO_DeptWise_Budget]
        WHERE [IsActive] = 1
          AND (@BudgetYear IS NULL OR [BudgetYear] = @BudgetYear)
        ORDER BY [Department], [Classification];";

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

        public async Task<decimal?> GetBudgetAmountAsync(string departmentCode, string classification, string budgetYear)
        {
            const string sql = @"
        SELECT TOP 1 [Budget]
        FROM [dbo].[PO_DeptWise_Budget]
        WHERE [Department] = @DepartmentCode
          AND [Classification] = @Classification
          AND [BudgetYear] = @BudgetYear
          AND [IsActive] = 1;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<decimal?>(sql,
                new { DepartmentCode = departmentCode, Classification = classification, BudgetYear = budgetYear });
        }

        public async Task<decimal> GetConsumedBudgetAsync(string departmentCode, string classification, string budgetYear)
        {
            const string sql = @"
        SELECT ISNULL(SUM([TotalAmount]), 0)
        FROM [dbo].[PO_PurchaseOrder]
        WHERE [DepartmentCode] = @DepartmentCode
          AND [Classification] = @Classification
          AND [BudgetYear] = @BudgetYear
          AND [Status] = 'Approved';";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<decimal>(sql,
                new { DepartmentCode = departmentCode, Classification = classification, BudgetYear = budgetYear });
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
             [TermsType], [CustomAdvanceText], [SubTotal], [GSTAmount], [TotalAmount],
             [IsBudgetExceeded], [QuotationRequired], [Status], [CurrentApprovalStep], [CreatedBy], [CreatedDt])
        OUTPUT INSERTED.[POId]
        VALUES
            (@PONumber, @VendorId, @VendorAddressId, @DepartmentCode, @Classification, @BudgetYear,
             @TermsType, @CustomAdvanceText, @SubTotal, @GSTAmount, @TotalAmount,
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

        public async Task AddApprovalStepsAsync(int poId, IEnumerable<PoPurchaseOrderApprovalHistory> steps)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_PurchaseOrderApprovalHistory] ([POId], [StepNumber], [ApproverRole])
        VALUES (@POId, @StepNumber, @ApproverRole);";

            using IDbConnection connection = new SqlConnection(_conStr);
            foreach (var step in steps) step.POId = poId;
            await connection.ExecuteAsync(sql, steps);
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

    }
    }
