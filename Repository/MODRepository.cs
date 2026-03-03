using Microsoft.Data.SqlClient;
using ProductivityApi.Repository;
using System.Data;

namespace ProductivityApi.Repository
{
    public class ModRepository
    {
        private readonly string _conStr;
        public ModRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task DeleteExistingDataAsync(string month)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("MOD_DeleteExistingData", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Month", month);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task InsertDataAsync(DataTable dataTable, string month)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                using var conn = new SqlConnection(_conStr);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("MOD_InsertMonthlyData", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Branch_Code", row["Branch Code"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Branch_Name", row["Branch Name"] ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@CM_Id", row["CM EMPID"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CM_Name", row["CM NAME"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CM_Designation", row["CM DESIGNATION"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CM_Appt_Date", ParseDate(row["CM APPOINTMENT DATE"]));

                cmd.Parameters.AddWithValue("@BM_Id", row["BM EMPID"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BM_Name", row["BM NAME"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BM_Designation", row["BM DESIGNATION"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BM_Appt_Date", ParseDate(row["BM APPOINTMENT DATE"]));

                cmd.Parameters.AddWithValue("@OPS_Id", row["OPS EMPID"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OPS_Name", row["OPS NAME"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OPS_Designation", row["OPS DESIGNATION"] ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OPS_Appt_Date", ParseDate(row["OPS APPOINTMENT DATE"]));

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private object ParseDate(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return DBNull.Value;

            return DateTime.Parse(value.ToString());
        }


    }
}
