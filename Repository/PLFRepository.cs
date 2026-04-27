using Npgsql;
using ProductivityApi.Models;
using ProductivityApi.Models.PLF;

namespace ProductivityApi.Repository
{
    public class PLFRepository
    {
        private readonly string _plfConStr;

        public PLFRepository(IConfiguration configuration)
        {
            _plfConStr = configuration.GetConnectionString("PlfDBConn");
        }
        public async Task<List<PLFPayment>> GetPaymentsByLan(string lan)
        {
            var pastPayments = new List<PLFPayment>();

            var sqlQuery = @"SELECT 
	                               HDR.RECEIPTID,
	                               HDR.RECEIPTMODE,
	                               HDR.RECEIPTAMOUNT,
	                               HDR.RECEIPTDATE
                            FROM PLF.FINRECEIPTHEADER HDR
                            INNER JOIN PLF.FINRECEIPTDETAIL DTL ON HDR.RECEIPTID = DTL.RECEIPTID
                            WHERE HDR.REFERENCE = @reference
                            ORDER BY HDR.RECEIPTDATE DESC;";

            await using var connection = new Npgsql.NpgsqlConnection(_plfConStr);
            await using var command = new Npgsql.NpgsqlCommand(sqlQuery, connection);
            command.Parameters.AddWithValue("@reference", lan);

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var payment = new PLFPayment
                {
                    ReceiptId = reader["RECEIPTID"]?.ToString() ?? string.Empty,
                    ReceiptMode = reader["RECEIPTMODE"]?.ToString() ?? string.Empty,
                    ReceiptAmount = reader["RECEIPTAMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["RECEIPTAMOUNT"]) : 0,
                    ReceiptDate = reader["RECEIPTDATE"] != DBNull.Value ? Convert.ToDateTime(reader["RECEIPTDATE"]) : DateTime.MinValue
                };

                pastPayments.Add(payment);
            }

            return pastPayments;
        }
    }
}




