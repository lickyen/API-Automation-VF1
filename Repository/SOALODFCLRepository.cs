using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using ProductivityApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace ProductivityApi.Repositories
{
    public class SOALODFCLRepository
    {
        private readonly string _conStr;

        public SOALODFCLRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }
        public async Task<IEnumerable<SOADashboard>> GetDashboardByUserAsync(SOADashboardRequestDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryAsync<SOADashboard>(
                "sp_SOALOD_GetLansByUserID",
                new
                {
                    UserID = dto.UserId,
                    Role= dto.Role,
                    Status = dto.status
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<SOADashboard?> GetSettlementByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var multi = await conn.QueryMultipleAsync(
                "sp_SOALOD_GetByID",
                new { ID = id },
                commandType: CommandType.StoredProcedure
            );

            var soa = await multi.ReadFirstOrDefaultAsync<SOADashboard>();

            if (soa != null)
            {
                soa.Payments = (await multi.ReadFirstOrDefaultAsync<SOALODFCLPayment>());
                soa.Logs = (await multi.ReadAsync<SOALog>()).ToList();
                soa.Charges= (await multi.ReadAsync<SOALODCharges>()).ToList();
            }

            return soa;
        }
        public async Task<SOACategoryResponseDto?> GetLoanCategoryAsync(SOACategoryRequestDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var multi = await conn.QueryMultipleAsync(
                "sp_Closure_GetLanCategory",
                new 
                {
                    lan = dto.Lan,
                    dpd=dto.DPD


                },
                commandType: CommandType.StoredProcedure
            );

            var soa = await multi.ReadFirstOrDefaultAsync<SOACategoryResponseDto>();

           

            return soa;
        }
        public async Task<SOABaseChargeResponseDto?> GetBaseChargesAsync(string type)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            using var multi = await conn.QueryMultipleAsync(
                "sp_SOALOD_GetBaseCharges",
                new { ReqType = type },
                commandType: CommandType.StoredProcedure
            );

            var charges = await multi.ReadFirstOrDefaultAsync<SOABaseChargeResponseDto>();


            return charges;
        }
        public async Task<IEnumerable<ActiverequestsResponseDto>> GetActiveRequestsByLANAsync(string Lan)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryAsync<ActiverequestsResponseDto>(
                "sp_SOALOD_GetActiveRequests",
                new
                {
                    LAN = Lan,
                  
                },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> CancelExistingRequestAsync(CancelrequestDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", dto.Id);
            parameters.Add("@RequestType", dto.RequestType);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("sp_CancelExistingRequest", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Output");
        }
        public async Task<int> CreateSOALODFCLAsync(CreateSOALODDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@LAN", dto.LAN);
            parameters.Add("@CustID", dto.CustID);
            parameters.Add("@CustName", dto.CustName);
            parameters.Add("@Mobile", dto.Mobile);
            parameters.Add("@Email", dto.Email);
            parameters.Add("@BranchCode", dto.BranchCode);
            parameters.Add("@BranchName", dto.BranchName);
            parameters.Add("@DPD", dto.DPD);
            parameters.Add("@DisbDate", dto.DisbDate);

            parameters.Add("@Allocation", dto.Allocation);
            parameters.Add("@Category", dto.Category);
            parameters.Add("@Bucket", dto.Bucket);
            parameters.Add("@SOA", dto.SOA);
            parameters.Add("@FCL", dto.FCL);
            parameters.Add("@LOD", dto.LOD);
            parameters.Add("@TotalCharges", dto.TotalCharges);
            parameters.Add("@closeLAN", dto.closeLAN);
            parameters.Add("@status", dto.status);
            parameters.Add("@remarks", dto.remarks);
            parameters.Add("@CreatedBy", dto.CreatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            try
            {

          

            await conn.ExecuteAsync(
                "SOALOD_NewRequest",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //return parameters.Get<int>("@Output");
            int TransactionId = parameters.Get<int>("@Output");
            // Call AddCharges SP after successful creation
            if (TransactionId > 0 && dto.charges != null && dto.charges.Any())
            {
                foreach (var charge in dto.charges)
                {
                    charge.TID = TransactionId;

                    await AddChargesAsync(charge);
                }
            }
            return TransactionId;

        }
        public async Task<int> AddChargesAsync(AddChargesDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
           
            parameters.Add("@ID", dto.TID);
            parameters.Add("@ChargeType", dto.ChargeType);
            parameters.Add("@IsWaiver", dto.IsWaiver);
     
            parameters.Add("@CreatedBy", dto.CreatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_SOALOD_Addcharges",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }
        public async Task<int> ApproveSOALODAsync(ApproveSOALODDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@ID", dto.Id);
            parameters.Add("@Status", dto.Status);
            parameters.Add("@role", dto.Role);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@UserId", dto.Updated_By);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);
            try
            {
            await conn.ExecuteAsync(
                "sp_SOALOD_Approval",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return parameters.Get<int>("@Output");
        }
        public async Task<int> CheckIfApproved(string id)
        {
            int isApproved = 0;

            using var conn = new SqlConnection(_conStr);

           
                using (SqlCommand cmd = new SqlCommand("sp_SOALOD_CheckIfApproved", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ID", id);

                    await conn.OpenAsync();

                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        isApproved = Convert.ToInt32(reader["IsApproved"]);
                    }
                }
            

            return isApproved;
        }
    }
}
