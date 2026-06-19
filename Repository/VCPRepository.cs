using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class VCPRepository
    {
        private readonly string _conStr;

        public VCPRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("VCPDBConn");
        }

        private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

        public async Task<VCPModel?> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();

            return await conn.QuerySingleOrDefaultAsync<VCPModel>(
                "SP_HL_VCP_GetVCP_ByID",
                new { ID = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<VCPListItem>> GetByUserAsync(VCPGetByUserRequestDto dto)
        {
            var spName = dto.Status == "Pending"
                ? "SP_HL_GetVCP_ByUser"
                : "SP_HL_GetVCPHistory_ByUser";

            using var conn = CreateConnection();

            var items = await conn.QueryAsync<VCPListItem>(
                spName,
                new { UserID = dto.UserId, Role = dto.Role },
                commandType: CommandType.StoredProcedure
            );

            return items.ToList();
        }

        public async Task<IEnumerable<VCPLog>> GetLogsAsync(int reqId)
        {
            using var conn = CreateConnection();

            return await conn.QueryAsync<VCPLog>(
                "SP_HL_VCP_GetLogs",
                new { ReqId = reqId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> AddLogAsync(VCPAddLogRequest request)
        {
            using var conn = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@ReqId", request.ReqId);
            parameters.Add("@Status", request.Status);
            parameters.Add("@Remarks", request.Remarks);
            parameters.Add("@CreatedBy", request.CreatedBy);
            parameters.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("SP_HL_VCP_AddLog", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@NewId");
        }

        public async Task<int> SubmitRCUAsync(VCPSubmitRCURequest request, string finalStatus, string? filePath)
        {
            using var conn = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@ID", request.Id);
            parameters.Add("@Status", request.Status);
            parameters.Add("@FinalStatus", finalStatus);
            parameters.Add("@Remarks", request.Remarks);
            parameters.Add("@FilePath", filePath);
            parameters.Add("@UpdatedBy", request.UpdatedBy);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("SP_HL_UpdateVCPStatus_RCU", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Output");
        }

        public async Task<int> SubmitBHAsync(VCPSubmitBHRequest request, string finalStatus)
        {
            using var conn = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@ID", request.Id);
            parameters.Add("@Status", request.Status);
            parameters.Add("@FinalStatus", finalStatus);
            parameters.Add("@Remarks", request.Remarks);
            parameters.Add("@Role", request.Role.ToUpper());
            parameters.Add("@UpdatedBy", request.UpdatedBy);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("SP_HL_UpdateVCPStatus_RBH_ZBH_MZBH", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Output");
        }

        public async Task<int> SubmitCOPSAsync(VCPSubmitCOPSRequest request, string finalStatus)
        {
            using var conn = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@VCP_ID", request.Id);
            parameters.Add("@Status", request.Status);
            parameters.Add("@FinalStatus", finalStatus);
            parameters.Add("@Remarks", request.Remarks);
            parameters.Add("@UpdatedBy", request.UpdatedBy);
            parameters.Add("@PanCopyAvailable", request.PANCopyAvailable);
            parameters.Add("@AddressProofAvailable", request.AddressProofAvailable);
            parameters.Add("@AddressMatching", request.AddressMatching);
            parameters.Add("@BankAccountMatchingWithPennyPing", request.BankAccountMatchingWithPennyPing);
            parameters.Add("@DsAProof", request.DSAProof);
            parameters.Add("@VcpAgreement", request.VCPAgreement);
            parameters.Add("@UrcCertificate", request.URCCertificate);
            parameters.Add("@AdhaarMasked", request.AadhaarMasked);
            parameters.Add("@PanFirm", request.PANFirm);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("SP_HL_UpdateVCPStatus_COPS", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Output");
        }

        public async Task<int> SubmitChannelSPOCAsync(VCPSubmitChannelSPOCRequest request, string finalStatus)
        {
            using var conn = CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@ID", request.Id);
            parameters.Add("@Status", request.Status);
            parameters.Add("@FinalStatus", finalStatus);
            parameters.Add("@Remarks", request.Remarks);
            parameters.Add("@UpdatedBy", request.UpdatedBy);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("SP_HL_UpdateVCPStatus_ChannelSpoc", parameters, commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Output");
        }

        public async Task<VCPCheckList?> GetCheckListAsync(int vcpId)
        {
            using var conn = CreateConnection();

            return await conn.QuerySingleOrDefaultAsync<VCPCheckList>(
                "SP_HL_VCP_GetCheckList",
                new { VCP_ID = vcpId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<VCPPartnerDetail>> GetPartnerDetailsAsync(int vcpId)
        {
            using var conn = CreateConnection();

            return await conn.QueryAsync<VCPPartnerDetail>(
                "SP_HL_VCP_GetPartnerDetails",
                new { ID = vcpId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<VCPEmployeeRelation>> GetEmployeeRelationsAsync(int vcpId)
        {
            using var conn = CreateConnection();

            return await conn.QueryAsync<VCPEmployeeRelation>(
                "SP_HL_VCP_GeEmployeeRelations",
                new { ID = vcpId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<string?> GetRCUStatusAsync(int id)
        {
            using var conn = CreateConnection();

            return await conn.QuerySingleOrDefaultAsync<string>(
                "SELECT RCU_Status FROM HL_VCP_Master WHERE ID = @ID",
                new { ID = id }
            );
        }

        public async Task<IEnumerable<dynamic>> GetReportBranchWiseAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.QueryAsync<dynamic>(
                "dbo.SP_HL_VCP_GetReport_BranchWise",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<dynamic>> GetReportOnboardingAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.QueryAsync<dynamic>(
                "dbo.SP_HL_VCP_GetReport_Onboarding",
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<IEnumerable<dynamic>> GetReportVisitsAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.QueryAsync<dynamic>(
                "dbo.SP_HL_VCP_GetReport_Visits",
                commandType: CommandType.StoredProcedure
            );
        }

    }
}