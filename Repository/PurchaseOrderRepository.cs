using Dapper;
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

        public async Task<CreatePOResponse> CreatePOAsync(
            CreatePORequest request,
            decimal totalBaseValue,
            decimal totalGSTAmount,
            decimal totalValueInclGST,
            bool quotationRequired,
            int requiredQuotationCount,
            decimal quotationThreshold,
            string createdBy)
        {
            await using SqlConnection connection = new(_conStr);
            await connection.OpenAsync();

            await using SqlCommand command = new("dbo.sp_PO_Management_Create", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddPurchaseOrderParameters(
                command,
                request,
                totalBaseValue,
                totalGSTAmount,
                totalValueInclGST,
                quotationRequired,
                requiredQuotationCount,
                quotationThreshold,
                createdBy);

            AddTableValuedParameters(command, request);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("Create PO procedure returned no result.");

            return ReadWorkflowCreateResponse(reader);
        }

        public async Task<PurchaseOrderWorkflowResponse> UpdateDraftAsync(
            long poId,
            CreatePORequest request,
            PurchaseOrderValidationResponse totals,
            string modifiedBy)
        {
            await using SqlConnection connection = new(_conStr);
            await connection.OpenAsync();

            await using SqlCommand command = new("dbo.sp_PO_Management_UpdateDraft", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("@POId", SqlDbType.BigInt).Value = poId;
            AddPurchaseOrderParameters(
                command,
                request,
                totals.TotalBaseValue,
                totals.TotalGSTAmount,
                totals.TotalValueInclGST,
                totals.IsQuotationRequired,
                totals.RequiredQuotationCount,
                totals.QuotationThresholdAmount,
                modifiedBy,
                "@ModifiedBy");
            AddTableValuedParameters(command, request);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("Update PO draft procedure returned no result.");

            return ReadWorkflowResponse(reader);
        }

        public async Task<PurchaseOrderDetailsResponse?> GetByIdAsync(long poId)
        {
            await using SqlConnection connection = new(_conStr);
            await connection.OpenAsync();

            using SqlMapper.GridReader results = await connection.QueryMultipleAsync(
                "dbo.sp_PO_Management_GetById",
                new { POId = poId },
                commandType: CommandType.StoredProcedure);

            PurchaseOrderDetailsResponse? header =
                await results.ReadSingleOrDefaultAsync<PurchaseOrderDetailsResponse>();

            if (header is null)
                return null;

            header.Items = (await results.ReadAsync<PurchaseOrderItemResponse>()).AsList();
            header.Quotations = (await results.ReadAsync<PurchaseOrderQuotationResponse>()).AsList();
            header.Terms = (await results.ReadAsync<PurchaseOrderTermResponse>()).AsList();
            header.ApprovalHistory =
                (await results.ReadAsync<PurchaseOrderApprovalHistoryResponse>()).AsList();

            return header;
        }

        public async Task<PagedPurchaseOrderResponse> SearchAsync(
            PurchaseOrderFilterRequest filter)
        {
            DynamicParameters parameters = new();
            parameters.Add("@EmployeeId", filter.EmployeeId, DbType.String, size: 50);
            parameters.Add("@Department", filter.Department, DbType.String, size: 100);
            parameters.Add("@Status", filter.Status, DbType.String, size: 30);
            parameters.Add("@ExpenseType", filter.ExpenseType, DbType.String, size: 10);
            parameters.Add("@VendorId", filter.VendorId, DbType.Int64);
            parameters.Add("@FromDate", filter.FromDate, DbType.Date);
            parameters.Add("@ToDate", filter.ToDate, DbType.Date);
            parameters.Add("@Search", filter.Search, DbType.String, size: 100);
            parameters.Add("@PageNumber", filter.PageNumber, DbType.Int32);
            parameters.Add("@PageSize", filter.PageSize, DbType.Int32);

            return await ReadPagedAsync("dbo.sp_PO_Management_Search", parameters, filter.PageNumber, filter.PageSize);
        }

        public async Task<PagedPurchaseOrderResponse> GetActionableAsync(
            string employeeId,
            string userRole,
            int pageNumber,
            int pageSize)
        {
            DynamicParameters parameters = new();
            parameters.Add("@EmployeeId", employeeId, DbType.String, size: 50);
            parameters.Add("@UserRole", userRole, DbType.String, size: 50);
            parameters.Add("@PageNumber", pageNumber, DbType.Int32);
            parameters.Add("@PageSize", pageSize, DbType.Int32);

            return await ReadPagedAsync(
                "dbo.sp_PO_Management_GetActionable",
                parameters,
                pageNumber,
                pageSize);
        }

        public async Task<IReadOnlyList<PurchaseOrderApprovalHistoryResponse>>
            GetApprovalHistoryAsync(long poId)
        {
            await using SqlConnection connection = new(_conStr);
            IEnumerable<PurchaseOrderApprovalHistoryResponse> result = await connection.QueryAsync<
                PurchaseOrderApprovalHistoryResponse>(
                "dbo.sp_PO_Management_GetApprovalHistory",
                new { POId = poId },
                commandType: CommandType.StoredProcedure);

            return result.AsList();
        }

        public async Task<PurchaseOrderWorkflowResponse> TransitionAsync(
            long poId,
            string action,
            string actionBy,
            string userRole,
            string? remarks)
        {
            await using SqlConnection connection = new(_conStr);
            PurchaseOrderWorkflowResponse result = await connection.QuerySingleAsync<
                PurchaseOrderWorkflowResponse>(
                "dbo.sp_PO_Management_Transition",
                new
                {
                    POId = poId,
                    Action = action,
                    ActionBy = actionBy,
                    UserRole = userRole,
                    Remarks = remarks
                },
                commandType: CommandType.StoredProcedure);

            return result;
        }

        public async Task<POAddendumResponse> CreateAddendumAsync(
            long poId,
            decimal additionalAmount,
            string reason,
            string? supportingDocumentName,
            string createdBy)
        {
            await using SqlConnection connection = new(_conStr);
            POAddendumResponse result = await connection.QuerySingleAsync<POAddendumResponse>(
                "dbo.sp_PO_Management_CreateAddendum",
                new
                {
                    POId = poId,
                    AdditionalAmount = additionalAmount,
                    Reason = reason,
                    SupportingDocumentName = supportingDocumentName,
                    CreatedBy = createdBy
                },
                commandType: CommandType.StoredProcedure);

            return result;
        }

        public async Task<IReadOnlyList<POAddendumResponse>> GetAddendumsAsync(long poId)
        {
            await using SqlConnection connection = new(_conStr);
            IEnumerable<POAddendumResponse> result = await connection.QueryAsync<POAddendumResponse>(
                "dbo.sp_PO_Management_GetAddendums",
                new { POId = poId },
                commandType: CommandType.StoredProcedure);

            return result.AsList();
        }

        public async Task<PurchaseOrderDocumentResponse?> GetPdfAsync(long poId, bool draft)
        {
            await using SqlConnection connection = new(_conStr);
            PurchaseOrderDocumentRecord? record = await connection.QuerySingleOrDefaultAsync<
                PurchaseOrderDocumentRecord>(
                "dbo.sp_PO_Management_GetPdf",
                new { POId = poId, IsDraft = draft },
                commandType: CommandType.StoredProcedure);

            if (record?.Content is null || record.Content.Length == 0)
                return null;

            return new PurchaseOrderDocumentResponse
            {
                Content = record.Content,
                ContentType = string.IsNullOrWhiteSpace(record.ContentType)
                    ? "application/pdf"
                    : record.ContentType,
                FileName = string.IsNullOrWhiteSpace(record.FileName)
                    ? $"PO_{poId}{(draft ? "_DRAFT" : string.Empty)}.pdf"
                    : record.FileName
            };
        }

        private async Task<PagedPurchaseOrderResponse> ReadPagedAsync(
            string storedProcedure,
            DynamicParameters parameters,
            int pageNumber,
            int pageSize)
        {
            await using SqlConnection connection = new(_conStr);
            using SqlMapper.GridReader results = await connection.QueryMultipleAsync(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure);

            List<PurchaseOrderSummaryResponse> items =
                (await results.ReadAsync<PurchaseOrderSummaryResponse>()).AsList();
            int totalCount = await results.ReadSingleAsync<int>();

            return new PagedPurchaseOrderResponse
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private static void AddPurchaseOrderParameters(
            SqlCommand command,
            CreatePORequest request,
            decimal totalBaseValue,
            decimal totalGSTAmount,
            decimal totalValueInclGST,
            bool quotationRequired,
            int requiredQuotationCount,
            decimal quotationThreshold,
            string userId,
            string userParameterName = "@CreatedBy")
        {
            command.Parameters.Add("@Department", SqlDbType.VarChar, 100).Value = request.Department;
            command.Parameters.Add("@PODate", SqlDbType.Date).Value = request.PODate.Date;
            command.Parameters.Add("@BudgetYear", SqlDbType.VarChar, 9).Value = request.BudgetYear;
            command.Parameters.Add("@VendorId", SqlDbType.BigInt).Value = request.VendorId;
            command.Parameters.Add("@VendorAddressId", SqlDbType.BigInt).Value = request.VendorAddressId;
            command.Parameters.Add("@VendorContactName", SqlDbType.NVarChar, 200).Value =
                (object?)request.VendorContactName ?? DBNull.Value;
            command.Parameters.Add("@VendorContactNumber", SqlDbType.VarChar, 20).Value =
                (object?)request.VendorContactNumber ?? DBNull.Value;
            command.Parameters.Add("@VendorEmailId", SqlDbType.VarChar, 254).Value =
                (object?)request.VendorEmailId ?? DBNull.Value;

            AddDecimal(command, "@TotalBaseValue", totalBaseValue, 18, 2);
            AddDecimal(command, "@TotalGSTAmount", totalGSTAmount, 18, 2);
            AddDecimal(command, "@TotalValueInclGST", totalValueInclGST, 18, 2);
            command.Parameters.Add("@IsQuotationRequired", SqlDbType.Bit).Value = quotationRequired;
            command.Parameters.Add("@RequiredQuotationCount", SqlDbType.Int).Value = requiredQuotationCount;
            AddDecimal(command, "@QuotationThresholdAmount", quotationThreshold, 18, 2);
            command.Parameters.Add(userParameterName, SqlDbType.VarChar, 50).Value = userId;
        }

        private static void AddTableValuedParameters(
            SqlCommand command,
            CreatePORequest request)
        {
            SqlParameter itemParameter = command.Parameters.AddWithValue(
                "@Items",
                CreateItemsTable(request.Items));
            itemParameter.SqlDbType = SqlDbType.Structured;
            itemParameter.TypeName = "dbo.PO_ItemTableType";

            SqlParameter quotationParameter = command.Parameters.AddWithValue(
                "@Quotations",
                CreateQuotationsTable(request.Quotations));
            quotationParameter.SqlDbType = SqlDbType.Structured;
            quotationParameter.TypeName = "dbo.PO_QuotationTableType";

            SqlParameter termParameter = command.Parameters.AddWithValue(
                "@Terms",
                CreateTermsTable(request.Terms));
            termParameter.SqlDbType = SqlDbType.Structured;
            termParameter.TypeName = "dbo.PO_TermTableType";
        }

        private static SqlParameter AddDecimal(
            SqlCommand command,
            string name,
            decimal value,
            byte precision,
            byte scale)
        {
            SqlParameter parameter = command.Parameters.Add(name, SqlDbType.Decimal);
            parameter.Precision = precision;
            parameter.Scale = scale;
            parameter.Value = value;
            return parameter;
        }

        private static CreatePOResponse ReadWorkflowCreateResponse(SqlDataReader reader)
        {
            return new CreatePOResponse
            {
                POId = reader.GetInt64(reader.GetOrdinal("POId")),
                PONumber = reader.GetString(reader.GetOrdinal("PONumber")),
                POStatus = reader.GetString(reader.GetOrdinal("POStatus"))
            };
        }

        private static PurchaseOrderWorkflowResponse ReadWorkflowResponse(SqlDataReader reader)
        {
            int currentRoleOrdinal = reader.GetOrdinal("CurrentApprovalRole");
            return new PurchaseOrderWorkflowResponse
            {
                POId = reader.GetInt64(reader.GetOrdinal("POId")),
                POStatus = reader.GetString(reader.GetOrdinal("POStatus")),
                CurrentApprovalRole = reader.IsDBNull(currentRoleOrdinal)
                    ? null
                    : reader.GetString(currentRoleOrdinal),
                ApprovalCycle = reader.GetInt32(reader.GetOrdinal("ApprovalCycle"))
            };
        }

        private static DataTable CreateItemsTable(IEnumerable<CreatePOItemRequest> items)
        {
            DataTable table = new();
            table.Columns.Add("ItemSequence", typeof(int));
            table.Columns.Add("Classification", typeof(string));
            table.Columns.Add("SubClassification", typeof(string));
            table.Columns.Add("ItemCategory", typeof(string));
            table.Columns.Add("ItemDescription", typeof(string));
            table.Columns.Add("Quantity", typeof(decimal));
            table.Columns.Add("BaseValue", typeof(decimal));
            table.Columns.Add("GSTPercentage", typeof(decimal));
            table.Columns.Add("GSTAmount", typeof(decimal));
            table.Columns.Add("ValueInclGST", typeof(decimal));
            table.Columns.Add("BudgetId", typeof(long));

            foreach (CreatePOItemRequest item in items)
            {
                table.Rows.Add(
                    item.ItemSequence,
                    item.Classification,
                    item.SubClassification,
                    item.ItemCategory,
                    item.ItemDescription,
                    item.Quantity,
                    item.BaseValue,
                    item.GSTPercentage,
                    item.GSTAmount,
                    item.ValueInclGST,
                    DBNull.Value);
            }

            return table;
        }

        private static DataTable CreateQuotationsTable(
            IEnumerable<CreatePOQuotationRequest> quotations)
        {
            DataTable table = new();
            table.Columns.Add("QuotationSequence", typeof(int));
            table.Columns.Add("QuotationVendorName", typeof(string));
            table.Columns.Add("QuotationReference", typeof(string));
            table.Columns.Add("QuotationDate", typeof(DateTime));
            table.Columns.Add("QuotedAmount", typeof(decimal));
            table.Columns.Add("FileName", typeof(string));
            table.Columns.Add("IsSelectedQuotation", typeof(bool));

            foreach (CreatePOQuotationRequest quotation in quotations)
            {
                table.Rows.Add(
                    quotation.QuotationSequence,
                    (object?)quotation.QuotationVendorName ?? DBNull.Value,
                    (object?)quotation.QuotationReference ?? DBNull.Value,
                    (object?)quotation.QuotationDate ?? DBNull.Value,
                    (object?)quotation.QuotedAmount ?? DBNull.Value,
                    quotation.FileName,
                    quotation.IsSelectedQuotation);
            }

            return table;
        }

        private static DataTable CreateTermsTable(IEnumerable<CreatePOTermRequest> terms)
        {
            DataTable table = new();
            table.Columns.Add("TermSequence", typeof(int));
            table.Columns.Add("TermType", typeof(string));
            table.Columns.Add("TermCategory", typeof(string));
            table.Columns.Add("TermsAndConditionDetails", typeof(string));
            table.Columns.Add("IsCustomTerm", typeof(bool));

            foreach (CreatePOTermRequest term in terms)
            {
                table.Rows.Add(
                    term.TermSequence,
                    term.TermType,
                    term.TermCategory,
                    term.TermsAndConditionDetails,
                    term.IsCustomTerm);
            }

            return table;
        }

        private sealed class PurchaseOrderDocumentRecord
        {
            public byte[] Content { get; set; } = [];
            public string? ContentType { get; set; }
            public string? FileName { get; set; }
        }
    }
}
