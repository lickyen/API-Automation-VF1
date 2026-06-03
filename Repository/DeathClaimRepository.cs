using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;
using System.Data.SqlTypes;

namespace ProductivityApi.Repository
{
    public class DeathClaimRepository
    {
        private readonly string _conStr;

        public DeathClaimRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
        }

        public async Task<DeathClaimMaster> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<DeathClaimMaster>(
                "dbo.sp_newDeathClaim_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<DeathClaimLog>> GetLogsAsync(int reqId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.QueryAsync<DeathClaimLog>(
                "dbo.sp_newDeathClaim_GetLogs",
                new { ReqId = reqId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task InsertLogAsync(InsertLogRequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.ReqId,
                dto.Status,
                dto.Remarks,
                dto.CreatedBy
            };
            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_InsertLogs",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> IntimationAsync(IntimationRequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@LAN", dto.Lan);
            parameters.Add("@Branch_Code", dto.BranchCode);
            parameters.Add("@Branch_Name", dto.BranchName);
            parameters.Add("@State", dto.State);
            parameters.Add("@Full_Name", dto.FullName);
            parameters.Add("@Mobile", dto.Mobile);
            parameters.Add("@Address", dto.Address);

            parameters.Add("@Nominee_Name", dto.NomineeName);
            parameters.Add("@Nominee_DOB", dto.NomineeDob);
            parameters.Add("@Nominee_Mobile", dto.NomineeMobile);
            parameters.Add("@Nominee_Relationship", dto.NomineeRelationship);
            parameters.Add("@Nominee_Death", dto.NomineeDeath);

            parameters.Add("@Disb_Date", dto.DisbDate);
            parameters.Add("@Death_Date", dto.DeathDate);
            parameters.Add("@Death_Cause", dto.DeathCause);
            parameters.Add("@Death_Place", dto.DeathPlace);

            parameters.Add("@DPD", dto.Dpd);
            parameters.Add("@Total_Outstanding", dto.TotalOutstanding);
            parameters.Add("@Loan_Amnt", dto.LoanAmnt);

            parameters.Add("@Policy_No", dto.PolicyNo);
            parameters.Add("@System_Generated_No", dto.SystemGeneratedNo);
            parameters.Add("@Member_Id", dto.MemberId);

            parameters.Add("@CreatedBy", dto.CreatedBy);
            parameters.Add("@Status", status);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Intimation",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public async Task<int> UpdateIntimationAsync(UpdateIntimationRequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();
            parameters.Add("@Id", dto.Id);
            parameters.Add("@LAN", dto.Lan);
            parameters.Add("@Branch_Code", dto.BranchCode);
            parameters.Add("@Branch_Name", dto.BranchName);
            parameters.Add("@State", dto.State);
            parameters.Add("@Full_Name", dto.FullName);
            parameters.Add("@Mobile", dto.Mobile);
            parameters.Add("@Address", dto.Address);
            parameters.Add("@Nominee_Name", dto.NomineeName);
            parameters.Add("@Nominee_DOB", dto.NomineeDob);
            parameters.Add("@Nominee_Mobile", dto.NomineeMobile);
            parameters.Add("@Nominee_Relationship", dto.NomineeRelationship);
            parameters.Add("@Nominee_Death", dto.NomineeDeath);
            parameters.Add("@Disb_Date", dto.DisbDate);
            parameters.Add("@Death_Date", dto.DeathDate);
            parameters.Add("@Death_Cause", dto.DeathCause);
            parameters.Add("@Death_Place", dto.DeathPlace);
            parameters.Add("@DPD", dto.Dpd);
            parameters.Add("@Total_Outstanding", dto.TotalOutstanding);
            parameters.Add("@Loan_Amnt", dto.LoanAmnt);
            parameters.Add("@Policy_No", dto.PolicyNo);
            parameters.Add("@System_Generated_No", dto.SystemGeneratedNo);
            parameters.Add("@Member_Id", dto.MemberId);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);
            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_UpdateIntimation",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return parameters.Get<int>("@Output");
        }
        public async Task<int> UpdateFileNameAsync(int reqId, string fileType, string fileName, string updatedBy)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();
            parameters.Add("@Id", reqId);
            parameters.Add("@FileName", fileName);
            parameters.Add("@FileType", fileType);
            parameters.Add("@UpdatedBy", updatedBy);
            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_UpdateFileName",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@Output");
        }
        public async Task<int> SubmitDocsAsync(SubmitDocsRequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@NomineeAcNo", dto.NomineeAcNo);
            parameters.Add("@NomineeAcName", dto.NomineeAcName);
            parameters.Add("@NomineeIfsc", dto.NomineeIfsc);
            parameters.Add("@NomineeBankName", dto.NomineeBankName);
            //parameters.Add("@BmVisitFormPath", dto.BmVisitFormPath);
            //parameters.Add("@KycPath", dto.KycPath);
            //parameters.Add("@PanchayatLetterPath", dto.PanchayatLetterPath);
            //parameters.Add("@DeathSummaryPath", dto.DeathSummaryPath);
            //parameters.Add("@MedicalQuestPath", dto.MedicalQuestPath);
            //parameters.Add("@FirPath", dto.FirPath);
            //parameters.Add("@PostMortemPath", dto.PostMortemPath);
            //parameters.Add("@NomineeBankProofPath", dto.NomineeBankProofPath);
            //parameters.Add("@DoghPath", dto.DoghPath);
            //parameters.Add("@DeathCertificatePath", dto.DeathCertificatePath);
            //parameters.Add("@NomineeDeathCertificatePath", dto.NomineeDeathCertificatePath);
            //parameters.Add("@NewNomineeKycPath", dto.NewNomineeKycPath);
            //parameters.Add("@NewNomineeBankProofPath", dto.NewNomineeBankProofPath);
            //parameters.Add("@FamilyTreeDeclarationPath", dto.FamilyTreeDeclarationPath);
            //parameters.Add("@AffidavitCumIndemnityPath", dto.AffidavitCumIndemnityPath);
            //parameters.Add("@NocFromLegalHeirsPath", dto.NocFromLegalHeirsPath);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);
            parameters.Add("@Status", status);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_UploadDocs",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }
        public async Task<int> SubmitBranch2Async(SubmitBranch2RequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@Remarks", dto.Remarks);
            //parameters.Add("@ClaimFormatPath", dto.ClaimFormatPath);
            //parameters.Add("@KotakClaimFormPath", dto.KotakClaimFormPath);
            //parameters.Add("@DischargeFormPath", dto.DischargeFormPath);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);
            parameters.Add("@Status", status);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Branch2",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }
        public async Task<int> SubmitBranch3Async(SubmitBranch3RequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            //parameters.Add("@AdditionalDoc1", dto.AdditionalDoc1);
            //parameters.Add("@AdditionalDoc2", dto.AdditionalDoc2);
            //parameters.Add("@AdditionalDoc3", dto.AdditionalDoc3);
            //parameters.Add("@AdditionalDoc4", dto.AdditionalDoc4);
            //parameters.Add("@AdditionalDoc5", dto.AdditionalDoc5);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);
            parameters.Add("@Status", status);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Branch3",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }
        public async Task<int> SubmitBranch5Async(SubmitBranch5RequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@TotalOutstanding", dto.TotalOutstanding);
            parameters.Add("@Deficit", dto.Deficit);
            parameters.Add("@DeficitCollectedRemarks", dto.DeficitCollectedRemarks);
            parameters.Add("@DeficitCollectedAmount", dto.DeficitCollectedAmount);
            //parameters.Add("@DeficitReceiptPath", dto.DeficitReceiptPath);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);
            parameters.Add("@Status", status);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Branch5",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }
        public async Task<int> SubmitCops1Async(SubmitCops1RequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@Status", status);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@ClaimAmountDod", dto.ClaimAmountDod);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Cops1",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }
        public async Task<int> SubmitCops2Async(SubmitCops2RequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@PortalUploadStatus", dto.PortalUploadStatus);
            parameters.Add("@ClaimSubmittedDt", dto.ClaimSubmittedDt);
            parameters.Add("@ClaimNo", dto.ClaimNo);
            parameters.Add("@AdditionalReq", dto.AdditionalReq);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@Status", status);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Cops2",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public async Task<int> SubmitCops3Async(SubmitCops3RequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@Status", status);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Cops3",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public async Task<int> SubmitCops4Async(SubmitCops4RequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@ClaimStatus", dto.ClaimStatus);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@ClaimReceivedAmount", dto.ClaimReceivedAmount);
            parameters.Add("@ClaimReceivedDate", dto.ClaimReceivedDate);
            parameters.Add("@Utr", dto.Utr);
            parameters.Add("@Dpd", dto.Dpd);
            parameters.Add("@TotalOutstanding", dto.TotalOutstanding);
            parameters.Add("@Deficit", dto.Deficit);
            parameters.Add("@Status", status);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Cops4",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public async Task<int> SubmitCops5Async(SubmitCops5RequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@Id", dto.Id);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@Status", status);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Cops5",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public async Task<ClaimFormDataDto?> GetClaimFormDataAsync(int id)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<ClaimFormDataDto>(
                "dbo.sp_newDeathClaim_GetClaimFormData",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<IEnumerable<dynamic>> GetReportAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.QueryAsync<dynamic>(
                "dbo.sp_newDeathClaim_GetReport",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<DeathClaimMaster>> GetByUserAsync(GetByUserRequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.QueryAsync<DeathClaimMaster>(
                "dbo.sp_newDeathClaim_GetByUser",
                new { dto.UserId, dto.Role, dto.Status },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}