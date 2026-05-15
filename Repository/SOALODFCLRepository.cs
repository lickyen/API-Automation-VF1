using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using ProductivityApi.Models;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models.PLF;
using DocumentFormat.OpenXml.Office.Word;
using Azure;

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
                    Role = dto.Role,
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
                soa.Charges = (await multi.ReadAsync<SOALODCharges>()).ToList();
            }

            return soa;
        }
        //public async Task<SOACategoryResponseDto?> GetLoanCategoryAsync(SOACategoryRequestDto dto)
        //{
        //    using var conn = new SqlConnection(_conStr);
        //    await conn.OpenAsync();

        //    using var multi = await conn.QueryMultipleAsync(
        //        "sp_Closure_GetLanCategory",
        //        new 
        //        {
        //            lan = dto.Lan,
        //            dpd=dto.DPD


        //        },
        //        commandType: CommandType.StoredProcedure
        //    );

        //    var soa = await multi.ReadFirstOrDefaultAsync<SOACategoryResponseDto>();



        //    return soa;
        //}
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
        public async Task<int> CheckIfApproved(int id)
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

        public async Task<int> NOCSubmitAsync(SOALODNOCSubmitDto dto, string? LODFilePath)
        {

            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();
            parameters.Add("@ID", dto.Id);
            parameters.Add("@Status", dto.Status);
            parameters.Add("@UserId", dto.UserId);
            parameters.Add("@LODFileName", LODFilePath);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);
            try
            {
                await conn.ExecuteAsync(
                    "sp_SOALOD_updateNOC",
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

        public async Task<int> SaveSOALODFCLDocsAsync(int Id, String Doctype, String DocName)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", Id);
            parameters.Add("@DocType", Doctype);
            parameters.Add("@DocName", DocName);



            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync(
                    "sp_SOALOD_SaveDoc",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            int TransactionId = parameters.Get<int>("@Output");

            return TransactionId;

        }

        public async Task<string> GetPaymentRefID()
        {
            string RefId = "";
            using var conn = new SqlConnection(_conStr);
            using (SqlCommand cmd = new SqlCommand("sp_generateReferenceNo", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                await conn.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    RefId = Convert.ToString(reader["RefID"]);
                }
            }
            return RefId;
        }
        //public async Task<PaymentLinkResponseDto> GeneratePaymentLink(string RefId)
        //{
        //    //string Refid= await

        //    return 1;
        //}

        public async Task<int> AddPayment_Online(PaymentLinkResponseDto response, int Id, string Lan,string Createdby)
        {
            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();

            parameters.Add("@TId", Id);
            parameters.Add("@Lan", Lan);
            parameters.Add("@amount", response.amount / 100);
            parameters.Add("@expire_by", response.expire_by);
            parameters.Add("@reference_id", response.reference_id);
            parameters.Add("@name", response.customer?.Name);
            parameters.Add("@contact", response.customer?.Contact);
            parameters.Add("@email", response.customer?.Email);
            parameters.Add("@created_at", response.created_at);
            parameters.Add("@paymentLinkid", response.id);
            parameters.Add("@short_url", response.short_url);
            parameters.Add("@createdBy", Createdby);


            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync(
                    "SP_AddPaymentLink",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            int TransactionId = parameters.Get<int>("@Output");

            return TransactionId;

        }

        public async Task<int> AddPayment_cash(SOALODCashDto dto)
        {
            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();

            parameters.Add("@TId", dto.Id);
            parameters.Add("@Lan", dto.Lan);
            parameters.Add("@amount", dto.Amount);
            parameters.Add("@RefID", dto.RefId);
            parameters.Add("@createdBy", dto.CreatedBy);


            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync(
                    "sp_AddPayment_Cash",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            int TransactionId = parameters.Get<int>("@Output");

            return TransactionId;

        }


        public async Task<IEnumerable<PaymentLinkTransaction>> GetPaylinkdetails(int id)
        {
            PaymentLinkTransaction pt = new PaymentLinkTransaction();
            using var conn = new SqlConnection(_conStr);
            try {
                return await conn.QueryAsync<PaymentLinkTransaction>(
                 "sp_SOALOD_getPaymentStatus",
                 new
                 {
                     ID = id,

                 },
                 commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Enumerable.Empty<PaymentLinkTransaction>();
            }
        }

        public async Task<IEnumerable<ChargesResponseDto>> GetChargesById(int id)
        {
            ChargesResponseDto pt = new ChargesResponseDto();
            using var conn = new SqlConnection(_conStr);
            try
            {
                return await conn.QueryAsync<ChargesResponseDto>(
                 "sp_SOALOD_getChargesbyID",
                 new
                 {
                     ID = id,

                 },
                 commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Enumerable.Empty<ChargesResponseDto>();
            }
        }

        public async Task<int> UpdateAdviseID(int Tid, string ChargeCode, string AdviseID, string ReturnstatusCode, string Userid)
        {
            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();

            parameters.Add("@ID", Tid);
            parameters.Add("@ChargeType", ChargeCode);
            parameters.Add("@ChargeID", AdviseID);
            parameters.Add("@ReturnStatus", ReturnstatusCode);
            parameters.Add("@UpdatedBy", Userid);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);
            try
            {
                await conn.ExecuteAsync(
                    "sp_SOALOD_UpdateChargeID",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            int TransactionId = parameters.Get<int>("@Output");

            return TransactionId;

        }


        public async Task<int> UpdatePLFPaymentResponse(int Tid, PaymentResponse PR , string RefId, string Userid)
        {

            string status = "";
            string receipt = "";

            if (PR.returnStatus.returnCode == "0000")
            {
                status = "success";
                receipt = PR.receiptId.ToString();
            }
            else
            {
                status = PR.returnStatus.returnCode + "-" + PR.returnStatus.returnText;
            }

            using var conn = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();

            parameters.Add("@ID", Tid);
            parameters.Add("@ReferenceID", RefId);
            parameters.Add("@Status", status);
            parameters.Add("@ReceiptId", receipt);
            parameters.Add("@UpdatedBy", Userid);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);
            try
            {
                await conn.ExecuteAsync(
                    "sp_UpdatePaymentPushStatus",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            int TransactionId = parameters.Get<int>("@Output");

            return TransactionId;

        }

    }
}
