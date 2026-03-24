using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
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

        public async Task InsertDataAsync(DataTable dataTable, UploadHrDataDto dto)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                using var conn = new SqlConnection(_conStr);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("MOD_InsertMonthlyData", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Month", dto.Month ?? (object)DBNull.Value);
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

                cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private object ParseDate(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return DBNull.Value;

            return DateTime.Parse(value.ToString());
        }

        public async Task<List<MODEligibleDto>> GetEligibleListAsync(string month)
        {
            var list = new List<MODEligibleDto>();

            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("MOD_GetDataForLetter", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Month", month ?? (object)DBNull.Value);


            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new MODEligibleDto
                {
                    EmpId = reader["EmpId"]?.ToString(),
                    Name = reader["Name"]?.ToString(),
                    AppointmentDate = reader["AppointmentDate"]?.ToString(),
                    Designation = reader["Designation"]?.ToString(),
                    BranchId = reader["BranchId"]?.ToString()
                });
            }

            return list;
        }


        public async Task<List<LetterHistoryDto>> GetLetterHistoryByEmpIdAsync(string empId)
        {
            var list = new List<LetterHistoryDto>();

            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("MOD_GetLetterHistoryByEmpID", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@EmpID", empId);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new LetterHistoryDto
                {
                    EmpId = reader["EmpId"]?.ToString(),
                    Name = reader["Name"]?.ToString(),
                    Designation = reader["Designation"]?.ToString(),
                    BranchId = reader["BranchId"]?.ToString(),
                    AppointmentDate = reader["AppointmentDate"]?.ToString(),
                    LetterIssueDate = reader["LetterIssueDate"]?.ToString(),
                    Month = reader["Month"]?.ToString()
                });
            }

            return list;
        }

        public async Task UpdateLetterIssuedAsync(string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("MOD_UpdateLetterIssued", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);

            await cmd.ExecuteNonQueryAsync();
        }

    }
}
