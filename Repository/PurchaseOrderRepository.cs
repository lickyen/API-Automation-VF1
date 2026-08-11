using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class PurchaseOrderRepository
    {
        private readonly string _conStr;
        public PurchaseOrderRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }
        public async Task<CreatePOResponse> CreatePOAsync(CreatePORequest request,decimal totalBaseValue,decimal totalGSTAmount, decimal totalValueInclGST,bool quotationRequired,
        int requiredQuotationCount,decimal quotationThreshold,string createdBy)
        {
            DataTable items = CreateItemsTable(request.Items);
            DataTable quotations = CreateQuotationsTable(request.Quotations);
            DataTable terms = CreateTermsTable(request.Terms);

            await using SqlConnection connection =new(_conStr);

            await connection.OpenAsync();

            await using SqlCommand command =
                new("dbo.sp_PO_Management_Create", connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@Department", SqlDbType.VarChar, 100)
                .Value = request.Department;

            command.Parameters.Add("@PODate", SqlDbType.Date)
                .Value = request.PODate.Date;

            command.Parameters.Add("@BudgetYear", SqlDbType.VarChar, 9)
                .Value = request.BudgetYear;

            command.Parameters.Add("@VendorId", SqlDbType.BigInt)
                .Value = request.VendorId;

            command.Parameters.Add("@VendorAddressId", SqlDbType.BigInt)
                .Value = request.VendorAddressId;

            command.Parameters.Add("@VendorContactName",
                SqlDbType.NVarChar,200).Value =(object?)request.VendorContactName ?? DBNull.Value;

            command.Parameters.Add( "@VendorContactNumber",
                SqlDbType.VarChar,20).Value =(object?)request.VendorContactNumber ?? DBNull.Value;

            command.Parameters.Add("@VendorEmailId", SqlDbType.VarChar,
                254).Value = (object?)request.VendorEmailId ?? DBNull.Value;

            command.Parameters.Add( "@TotalBaseValue",SqlDbType.Decimal).Value = totalBaseValue;

            command.Parameters.Add( "@TotalGSTAmount",SqlDbType.Decimal).Value = totalGSTAmount;

            command.Parameters.Add("@TotalValueInclGST", SqlDbType.Decimal).Value = totalValueInclGST;

            command.Parameters.Add( "@IsQuotationRequired",SqlDbType.Bit).Value = quotationRequired;

            command.Parameters.Add("@RequiredQuotationCount", SqlDbType.Int).Value = requiredQuotationCount;

            command.Parameters.Add("@QuotationThresholdAmount",SqlDbType.Decimal).Value = quotationThreshold;

            command.Parameters.Add( "@CreatedBy",SqlDbType.VarChar,50).Value = createdBy;

            SqlParameter itemParameter =command.Parameters.AddWithValue("@Items", items);

            itemParameter.SqlDbType = SqlDbType.Structured;
            itemParameter.TypeName = "dbo.PO_ItemTableType";

            SqlParameter quotationParameter =
                command.Parameters.AddWithValue("@Quotations",quotations);

            quotationParameter.SqlDbType = SqlDbType.Structured;
            quotationParameter.TypeName ="dbo.PO_QuotationTableType";

            SqlParameter termParameter =command.Parameters.AddWithValue("@Terms", terms);

            termParameter.SqlDbType = SqlDbType.Structured;
            termParameter.TypeName = "dbo.PO_TermTableType";

            await using SqlDataReader reader =await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    "Create PO procedure returned no result.");
            }

            return new CreatePOResponse
            {
                POId = reader.GetInt64( reader.GetOrdinal("POId")),

                PONumber = reader.GetString(reader.GetOrdinal("PONumber")),

                POStatus = reader.GetString(reader.GetOrdinal("POStatus"))
            };
        }

        private static DataTable CreateItemsTable(
            IEnumerable<CreatePOItemRequest> items)
        {
            DataTable table = new();

            table.Columns.Add("ItemSequence", typeof(int));
            table.Columns.Add("Classification", typeof(string));
            table.Columns.Add("SubClassification", typeof(string));
            table.Columns.Add("ItemCategory", typeof(string));
        //    table.Columns.Add("ItemCode", typeof(string));
            table.Columns.Add("ItemDescription", typeof(string));
           // table.Columns.Add("UOM", typeof(string));
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
                    //  (object?)item.ItemCode ?? DBNull.Value,
                    item.ItemDescription,
                  //  item.UOM,
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
                    (object?)quotation.QuotationVendorName
                        ?? DBNull.Value,
                    (object?)quotation.QuotationReference
                        ?? DBNull.Value,
                    (object?)quotation.QuotationDate
                        ?? DBNull.Value,
                    (object?)quotation.QuotedAmount
                        ?? DBNull.Value,
                    quotation.FileName,
                    quotation.IsSelectedQuotation);
            }

            return table;
        }

        private static DataTable CreateTermsTable(
            IEnumerable<CreatePOTermRequest> terms)
        {
            DataTable table = new();

            table.Columns.Add("TermSequence", typeof(int));
            table.Columns.Add("TermType", typeof(string));
            table.Columns.Add("TermCategory", typeof(string));
            table.Columns.Add("TermsAndConditionDetails",typeof(string));
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
    }

}
