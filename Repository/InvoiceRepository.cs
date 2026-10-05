using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;
using Dapper;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Presentation;
using Org.BouncyCastle.Crypto.Agreement;
namespace ProductivityApi.Repository
{
    public class InvoiceRepository
    {
        private readonly string _conStr;


        public InvoiceRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")
                ?? throw new InvalidOperationException("DBConn connection string is missing.");
        }


        public async Task<PoPurchaseOrder?> GetApprovedPOForInvoiceAsync(int poId)
        {
            const string sql = "SELECT * FROM [dbo].[PO_PurchaseOrder] WHERE [POId] = @POId AND [Status] = 'Approved';";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryFirstOrDefaultAsync<PoPurchaseOrder>(sql, new { POId = poId });
        }

        public async Task<decimal> GetTotalInvoicedOnPOAsync(int poId, int? excludeInvoiceId = null)
        {
            const string sql = @"
        SELECT ISNULL(SUM([InvoiceAmount]), 0) FROM [dbo].[PO_Invoice]
        WHERE [POId] = @POId
          AND [Status] IN ('PendingApproval', 'Approved', 'PendingPayment', 'Paid')
          AND (@ExcludeInvoiceId IS NULL OR [InvoiceId] <> @ExcludeInvoiceId);";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<decimal>(sql, new { POId = poId, ExcludeInvoiceId = excludeInvoiceId });
        }

        public async Task<bool> POHasFinalInvoiceAsync(int poId)
        {
            const string sql = @"
        SELECT COUNT(1) FROM [dbo].[PO_Invoice]
        WHERE [POId] = @POId AND [IsFinalInvoice] = 1
          AND [Status] IN ('PendingApproval', 'Approved', 'PendingPayment', 'Paid');";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, new { POId = poId }) > 0;
        }

        public async Task<int> CreateInvoiceAsync(PoInvoice invoice)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_Invoice]
            ([InvoiceNumber], [InvoiceType], [POId], [AgreementId], [DepartmentCode], [Classification], [SubClassification],
             [VendorId], [VendorAddressId],
             [VendorContactName], [VendorContactMobile], [VendorContactEmail],
             [InvoiceDate], [InvoiceAmount], [SubTotal], [GSTAmount], [TotalAmount], [IsBudgetExceeded],
             [IsFinalInvoice], [InvoiceCopyPath], [Status], [CurrentApprovalStep], [CreatedBy], [CreatedDt])
        OUTPUT INSERTED.[InvoiceId]
        VALUES
            (@InvoiceNumber, @InvoiceType, @POId, @AgreementId, @DepartmentCode, @Classification, @SubClassification,
             @VendorId, @VendorAddressId,
             @VendorContactName, @VendorContactMobile, @VendorContactEmail,
             @InvoiceDate, @InvoiceAmount, @SubTotal, @GSTAmount, @TotalAmount, @IsBudgetExceeded,
             @IsFinalInvoice, @InvoiceCopyPath, 'PendingApproval', 1, @CreatedBy, GETDATE());";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, invoice);
        }

        public async Task AddInvoiceApprovalStepsAsync(int invoiceId, IEnumerable<PoInvoiceApprovalHistory> steps)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_InvoiceApprovalHistory] ([InvoiceId], [StepNumber], [ApproverRole])
        VALUES (@InvoiceId, @StepNumber, @ApproverRole);";

            using IDbConnection connection = new SqlConnection(_conStr);
            var list = steps.ToList();
            foreach (var s in list) s.InvoiceId = invoiceId;
            await connection.ExecuteAsync(sql, list);
        }

        public async Task<PoInvoice?> GetInvoiceByIdAsync(int invoiceId)
        {
            const string sql = "SELECT * FROM [dbo].[PO_Invoice] WHERE [InvoiceId] = @InvoiceId;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryFirstOrDefaultAsync<PoInvoice>(sql, new { InvoiceId = invoiceId });
        }

        public async Task<IEnumerable<PoInvoiceApprovalHistory>> GetInvoiceApprovalHistoryAsync(int invoiceId)
        {
            const string sql = "SELECT * FROM [dbo].[PO_InvoiceApprovalHistory] WHERE [InvoiceId] = @InvoiceId ORDER BY [StepNumber];";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoInvoiceApprovalHistory>(sql, new { InvoiceId = invoiceId });
        }

        public async Task<PoInvoiceApprovalHistory?> GetPendingInvoiceApprovalStepAsync(int invoiceId, int stepNumber)
        {
            const string sql = "SELECT * FROM [dbo].[PO_InvoiceApprovalHistory] WHERE [InvoiceId] = @InvoiceId AND [StepNumber] = @StepNumber;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryFirstOrDefaultAsync<PoInvoiceApprovalHistory>(sql, new { InvoiceId = invoiceId, StepNumber = stepNumber });
        }

        public async Task MarkInvoiceApprovalStepActionedAsync(int approvalId, string approverEmployeeCode, string action, string? remarks)
        {
            const string sql = @"
        UPDATE [dbo].[PO_InvoiceApprovalHistory]
        SET [ApproverEmployeeCode] = @ApproverEmployeeCode, [Action] = @Action, [Remarks] = @Remarks, [ActionedDt] = GETDATE()
        WHERE [ApprovalId] = @ApprovalId;";

            using IDbConnection connection = new SqlConnection(_conStr);
            await connection.ExecuteAsync(sql, new { ApprovalId = approvalId, ApproverEmployeeCode = approverEmployeeCode, Action = action, Remarks = remarks });
        }

        public async Task UpdateInvoiceStatusAsync(int invoiceId, string status, int currentApprovalStep, string? rejectionReason = null)
        {
            const string sql = @"
        UPDATE [dbo].[PO_Invoice]
        SET [Status] = @Status, [CurrentApprovalStep] = @CurrentApprovalStep, [RejectionReason] = @RejectionReason, [ModifiedDt] = GETDATE()
        WHERE [InvoiceId] = @InvoiceId;";

            using IDbConnection connection = new SqlConnection(_conStr);
            await connection.ExecuteAsync(sql, new { InvoiceId = invoiceId, Status = status, CurrentApprovalStep = currentApprovalStep, RejectionReason = rejectionReason });
        }

        public async Task AddInvoicePaymentAsync(PoInvoicePayment payment)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_InvoicePayments]
            ([InvoiceId], [PaymentStatus], [FailReason], [UTRNumber], [PaymentDate], [AmountPaid], [TDSAmount], [CreatedBy], [CreatedDt])
        VALUES
            (@InvoiceId, @PaymentStatus, @FailReason, @UTRNumber, @PaymentDate, @AmountPaid, @TDSAmount, @CreatedBy, GETDATE());";

            using IDbConnection connection = new SqlConnection(_conStr);
            await connection.ExecuteAsync(sql, payment);
        }

        public async Task<IEnumerable<PoInvoicePayment>> GetInvoicePaymentsAsync(int invoiceId)
        {
            const string sql = "SELECT * FROM [dbo].[PO_InvoicePayments] WHERE [InvoiceId] = @InvoiceId ORDER BY [CreatedDt];";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync<PoInvoicePayment>(sql, new { InvoiceId = invoiceId });
        }

        public async Task<decimal> GetTotalSuccessfulInvoicePaymentsAsync(int invoiceId)
        {
            const string sql = @"
        SELECT ISNULL(SUM([AmountPaid]), 0) FROM [dbo].[PO_InvoicePayments]
        WHERE [InvoiceId] = @InvoiceId AND [PaymentStatus] = 'Success';";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<decimal>(sql, new { InvoiceId = invoiceId });
        }

        //--------------Aggremnet Related-------------
        public async Task<bool> AgreementNumberExistsAsync(string agreementNumber)
        {
            const string sql = "SELECT COUNT(1) FROM [dbo].[PO_AgreementMaster] WHERE [AgreementNumber] = @AgreementNumber;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, new { AgreementNumber = agreementNumber }) > 0;
        }

        public async Task<int> CreateAgreementAsync(PoAgreementMaster agreement)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_AgreementMaster]
            ([AgreementNumber], [VendorId], [EffectiveDate], [ExpiryDate], [IsValid], [AgreementCopyPath], [CreatedBy], [CreatedDt])
        OUTPUT INSERTED.[AgreementId]
        VALUES
            (@AgreementNumber, @VendorId, @EffectiveDate, @ExpiryDate, 1, @AgreementCopyPath, @CreatedBy, GETDATE());";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.ExecuteScalarAsync<int>(sql, agreement);
        }

        public async Task<PoAgreementMaster?> GetAgreementByIdAsync(int agreementId)
        {
            const string sql = "SELECT * FROM [dbo].[PO_AgreementMaster] WHERE [AgreementId] = @AgreementId;";
            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryFirstOrDefaultAsync<PoAgreementMaster>(sql, new { AgreementId = agreementId });
        }

        public async Task UpdateAgreementAsync(PoAgreementMaster agreement)
        {
            const string sql = @"
        UPDATE [dbo].[PO_AgreementMaster]
        SET [EffectiveDate] = @EffectiveDate, [ExpiryDate] = @ExpiryDate,
            [AgreementCopyPath] = @AgreementCopyPath, [ModifiedBy] = @ModifiedBy, [ModifiedDt] = GETDATE()
        WHERE [AgreementId] = @AgreementId;";

            using IDbConnection connection = new SqlConnection(_conStr);
            int rows = await connection.ExecuteAsync(sql, agreement);
            if (rows == 0) throw new KeyNotFoundException($"Agreement {agreement.AgreementId} not found.");
        }

        public async Task UpdateAgreementValidityAsync(int agreementId, bool isValid, string modifiedBy)
        {
            const string sql = @"
        UPDATE [dbo].[PO_AgreementMaster]
        SET [IsValid] = @IsValid, [ModifiedBy] = @ModifiedBy, [ModifiedDt] = GETDATE()
        WHERE [AgreementId] = @AgreementId;";

            using IDbConnection connection = new SqlConnection(_conStr);
            int rows = await connection.ExecuteAsync(sql, new { AgreementId = agreementId, IsValid = isValid, ModifiedBy = modifiedBy });
            if (rows == 0) throw new KeyNotFoundException($"Agreement {agreementId} not found.");
        }

        public async Task<IEnumerable<dynamic>> GetAgreementsByVendorAsync(long vendorId)
        {
            const string sql = @"
        SELECT a.*, v.[VendorName]
        FROM [dbo].[PO_AgreementMaster] a
        INNER JOIN [dbo].[PO_VendorMaster] v ON v.[VendorId] = a.[VendorId]
        WHERE a.[VendorId] = @VendorId
        ORDER BY a.[EffectiveDate] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql, new { VendorId = vendorId });
        }

        public async Task<IEnumerable<dynamic>> GetInvoicesCreatedByAsync(string employeeCode)
        {
            const string sql = @"
        SELECT i.[InvoiceId], i.[InvoiceNumber], i.[InvoiceType], i.[POId], po.[PONumber],
               v.[VendorName], i.[InvoiceAmount], i.[Status], i.[CreatedBy], i.[CreatedDt], i.[CurrentApprovalStep]
        FROM [dbo].[PO_Invoice] i
        LEFT JOIN [dbo].[PO_PurchaseOrder] po ON po.[POId] = i.[POId]
        INNER JOIN [dbo].[PO_VendorMaster] v ON v.[VendorId] = i.[VendorId]
        WHERE i.[CreatedBy] = @EmployeeCode
        ORDER BY i.[CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql, new { EmployeeCode = employeeCode });
        }

        public async Task<IEnumerable<dynamic>> GetInvoicesPendingForDeptHeadAsync(string departmentCode)
        {
            const string sql = @"
        SELECT i.[InvoiceId], i.[InvoiceNumber], i.[InvoiceType], i.[POId], po.[PONumber],
               v.[VendorName], i.[InvoiceAmount], i.[Status], i.[CreatedBy], i.[CreatedDt]
        FROM [dbo].[PO_Invoice] i
        LEFT JOIN [dbo].[PO_PurchaseOrder] po ON po.[POId] = i.[POId]
        INNER JOIN [dbo].[PO_VendorMaster] v ON v.[VendorId] = i.[VendorId]
        INNER JOIN [dbo].[PO_InvoiceApprovalHistory] ah
            ON ah.[InvoiceId] = i.[InvoiceId] AND ah.[StepNumber] = i.[CurrentApprovalStep]
        WHERE i.[Status] = 'PendingApproval'
          AND ah.[ApproverRole] = 'DepartmentHead'
          AND i.[DepartmentCode] = @DepartmentCode;";


            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql, new { DepartmentCode = departmentCode });
        }

        public async Task<IEnumerable<dynamic>> GetInvoicesActedByUserAsync(string employeeCode)
        {
            const string sql = @"
        SELECT DISTINCT i.[InvoiceId], i.[InvoiceNumber], i.[InvoiceType], i.[POId], po.[PONumber],
               v.[VendorName], i.[InvoiceAmount], i.[Status], i.[CreatedBy], i.[CreatedDt]
        FROM [dbo].[PO_Invoice] i
        LEFT JOIN [dbo].[PO_PurchaseOrder] po ON po.[POId] = i.[POId]
        INNER JOIN [dbo].[PO_VendorMaster] v ON v.[VendorId] = i.[VendorId]
        INNER JOIN [dbo].[PO_InvoiceApprovalHistory] ah ON ah.[InvoiceId] = i.[InvoiceId]
        WHERE ah.[ApproverEmployeeCode] = @EmployeeCode
        ORDER BY i.[CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql, new { EmployeeCode = employeeCode });
        }

        public async Task<IEnumerable<dynamic>> GetInvoicesPendingPaymentAllAsync()
        {
            const string sql = @"
        SELECT i.[InvoiceId], i.[InvoiceNumber], i.[InvoiceType], i.[POId], po.[PONumber],
               v.[VendorName], i.[InvoiceAmount], i.[Status], i.[CreatedBy], i.[CreatedDt]
        FROM [dbo].[PO_Invoice] i
        LEFT JOIN [dbo].[PO_PurchaseOrder] po ON po.[POId] = i.[POId]
        INNER JOIN [dbo].[PO_VendorMaster] v ON v.[VendorId] = i.[VendorId]
        WHERE i.[Status] = 'PendingPayment'
        ORDER BY i.[CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql);
        }

        public async Task<IEnumerable<dynamic>> GetInvoicesPaidByUserAsync(string employeeCode)
        {
            const string sql = @"
        SELECT DISTINCT i.[InvoiceId], i.[InvoiceNumber], i.[InvoiceType], i.[POId], po.[PONumber],
               v.[VendorName], i.[InvoiceAmount], i.[Status], i.[CreatedBy], i.[CreatedDt]
        FROM [dbo].[PO_Invoice] i
        LEFT JOIN [dbo].[PO_PurchaseOrder] po ON po.[POId] = i.[POId]
        INNER JOIN [dbo].[PO_VendorMaster] v ON v.[VendorId] = i.[VendorId]
        INNER JOIN [dbo].[PO_InvoicePayments] p ON p.[InvoiceId] = i.[InvoiceId]
        WHERE p.[CreatedBy] = @EmployeeCode AND i.[Status] = 'Paid'
        ORDER BY i.[CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql, new { EmployeeCode = employeeCode });
        }
        public async Task<IEnumerable<dynamic>> GetInvoicesRejectedByCreatorAsync(string employeeCode)
        {
            const string sql = @"
        SELECT i.[InvoiceId], i.[InvoiceNumber], i.[InvoiceType], i.[POId], po.[PONumber],
               v.[VendorName], i.[InvoiceAmount], i.[Status], i.[CreatedBy], i.[CreatedDt]
        FROM [dbo].[PO_Invoice] i
        LEFT JOIN [dbo].[PO_PurchaseOrder] po ON po.[POId] = i.[POId]
        INNER JOIN [dbo].[PO_VendorMaster] v ON v.[VendorId] = i.[VendorId]
        WHERE i.[CreatedBy] = @EmployeeCode AND i.[Status] = 'Rejected'
        ORDER BY i.[CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql, new { EmployeeCode = employeeCode });
        }

        public async Task<IEnumerable<dynamic>> GetInvoicesInProgressByCreatorAsync(string employeeCode)
        {
            const string sql = @"
        SELECT i.[InvoiceId], i.[InvoiceNumber], i.[InvoiceType], i.[POId], po.[PONumber],
               v.[VendorName], i.[InvoiceAmount], i.[Status], i.[CreatedBy], i.[CreatedDt]
        FROM [dbo].[PO_Invoice] i
        LEFT JOIN [dbo].[PO_PurchaseOrder] po ON po.[POId] = i.[POId]
        INNER JOIN [dbo].[PO_VendorMaster] v ON v.[VendorId] = i.[VendorId]
        WHERE i.[CreatedBy] = @EmployeeCode AND i.[Status] IN ('PendingApproval', 'PendingPayment')
        ORDER BY i.[CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql, new { EmployeeCode = employeeCode });
        }

        public async Task<IEnumerable<dynamic>> GetInvoicesPaidByCreatorAsync(string employeeCode)
        {
            const string sql = @"
        SELECT i.[InvoiceId], i.[InvoiceNumber], i.[InvoiceType], i.[POId], po.[PONumber],
               v.[VendorName], i.[InvoiceAmount], i.[Status], i.[CreatedBy], i.[CreatedDt]
        FROM [dbo].[PO_Invoice] i
        LEFT JOIN [dbo].[PO_PurchaseOrder] po ON po.[POId] = i.[POId]
        INNER JOIN [dbo].[PO_VendorMaster] v ON v.[VendorId] = i.[VendorId]
        WHERE i.[CreatedBy] = @EmployeeCode AND i.[Status] = 'Paid'
        ORDER BY i.[CreatedDt] DESC;";

            using IDbConnection connection = new SqlConnection(_conStr);
            return await connection.QueryAsync(sql, new { EmployeeCode = employeeCode });
        }

        public async Task AddInvoiceItemsAsync(int invoiceId, IEnumerable<PoInvoiceItem> items)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_InvoiceItems]
            ([InvoiceId], [ItemDescription], [Quantity], [UnitPrice], [Amount], [GSTRate], [GSTAmount], [BudgetCategoryId], [IsBudgetExceeded])
        VALUES
            (@InvoiceId, @ItemDescription, @Quantity, @UnitPrice, @Amount, @GSTRate, @GSTAmount, @BudgetCategoryId, @IsBudgetExceeded);";

            using IDbConnection connection = new SqlConnection(_conStr);
            var list = items.ToList();
            foreach (var item in list) item.InvoiceId = invoiceId;
            await connection.ExecuteAsync(sql, list);
        }

        public async Task AddInvoiceDeliveryLocationsAsync(int invoiceId, IEnumerable<int> locationIds, string createdBy)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_InvoiceDeliveryLocations] ([InvoiceId], [LocationId], [CreatedBy], [CreatedDt])
        VALUES (@InvoiceId, @LocationId, @CreatedBy, GETDATE());";

            using IDbConnection connection = new SqlConnection(_conStr);
            var rows = locationIds.Select(id => new { InvoiceId = invoiceId, LocationId = id, CreatedBy = createdBy }).ToList();
            await connection.ExecuteAsync(sql, rows);
        }

        public async Task AddInvoiceTermsAsync(int invoiceId, IEnumerable<long> termMasterIds, string createdBy)
        {
            const string sql = @"
        INSERT INTO [dbo].[PO_InvoiceTerms] ([InvoiceId], [TermMasterId], [CreatedBy], [CreatedDt])
        VALUES (@InvoiceId, @TermMasterId, @CreatedBy, GETDATE());";

            using IDbConnection connection = new SqlConnection(_conStr);
            var rows = termMasterIds.Select(id => new { InvoiceId = invoiceId, TermMasterId = id, CreatedBy = createdBy }).ToList();
            await connection.ExecuteAsync(sql, rows);
        }
    }
}
