using Dapper;
using ProductivityApi.Models;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ProductivityApi.Repository
{
    public class DeathClaimRepository
    {
        private readonly string _conStr;

        public DeathClaimRepository(string conStr)
        {
            _conStr = conStr;
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

        public async Task<int> IntimationAsync(IntimationRequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Lan,
                dto.BranchCode,
                dto.BranchName,
                dto.State,
                dto.FullName,
                dto.Mobile,
                dto.Address,
                dto.NomineeName,
                dto.NomineeDob,
                dto.NomineeMobile,
                dto.NomineeRelationship,
                dto.NomineeDeath,
                dto.DisbDate,
                dto.DeathDate,
                dto.DeathCause,
                dto.DeathPlace,
                dto.Dpd,
                dto.TotalOutstanding,
                dto.LoanAmnt,
                dto.PolicyNo,
                dto.SystemGeneratedNo,
                dto.MemberId,
                dto.CreatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Intimation",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> UploadDocsAsync(UploadDocsRequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.NomineeAcNo,
                dto.NomineeAcName,
                dto.NomineeIfsc,
                dto.NomineeBankName,
                dto.BmVisitFormPath,
                dto.KycPath,
                dto.PanchayatLetterPath,
                dto.DeathSummaryPath,
                dto.MedicalQuestPath,
                dto.FirPath,
                dto.PostMortemPath,
                dto.NomineeBankProofPath,
                dto.DoghPath,
                dto.DeathCertificatePath,
                dto.NomineeDeathCertificatePath,
                dto.NewNomineeKycPath,
                dto.NewNomineeBankProofPath,
                dto.FamilyTreeDeclarationPath,
                dto.AffidavitCumIndemnityPath,
                dto.NocFromLegalHeirsPath,
                dto.UpdatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_UploadDocs",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> SubmitBranch2Async(SubmitBranch2RequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.Status,
                dto.Remarks,
                dto.ClaimFormatPath,
                dto.KotakClaimFormPath,
                dto.DischargeFormPath,
                dto.UpdatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Branch2",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> SubmitBranch3Async(SubmitBranch3RequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.Status,
                dto.AdditionalDoc1,
                dto.AdditionalDoc2,
                dto.AdditionalDoc3,
                dto.AdditionalDoc4,
                dto.AdditionalDoc5,
                dto.UpdatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Branch3",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> SubmitBranch5Async(SubmitBranch5RequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.TotalOutstanding,
                dto.Deficit,
                dto.DeficitCollectedRemarks,
                dto.DeficitCollectedAmount,
                dto.DeficitReceiptPath,
                dto.Remarks,
                dto.Status,
                dto.UpdatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Branch5",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> SubmitCops1Async(SubmitCops1RequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.Status,
                dto.Remarks,
                dto.ClaimAmountDod,
                dto.UpdatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Cops1",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> SubmitCops2Async(SubmitCops2RequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.PortalUploadStatus,
                dto.ClaimSubmittedDt,
                dto.ClaimNo,
                dto.AdditionalReq,
                dto.Remarks,
                dto.Status,
                dto.UpdatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Cops2",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> SaveCops2Async(SaveCops2RequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.PortalUploadStatus,
                dto.ClaimSubmittedDt,
                dto.ClaimNo,
                dto.Remarks,
                dto.UpdatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Save_Cops2",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> SubmitCops3Async(SubmitCops3RequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.Remarks,
                dto.Status,
                dto.UpdatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Cops3",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> SubmitCops4Async(SubmitCops4RequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.ClaimStatus,
                dto.Remarks,
                dto.ClaimReceivedAmount,
                dto.ClaimReceivedDate,
                dto.Utr,
                dto.Dpd,
                dto.TotalOutstanding,
                dto.Deficit,
                dto.Status,
                dto.UpdatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Cops4",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> SubmitCops5Async(SubmitCops5RequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.Remarks,
                dto.Status,
                dto.UpdatedBy
            };
            return await connection.ExecuteAsync(
                "dbo.sp_newDeathClaim_Submit_Cops5",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<dynamic>> GetReportAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.QueryAsync<dynamic>(
                "sp_FcRefund_GetReport",
                commandType: CommandType.StoredProcedure
            );
        }
    }
}