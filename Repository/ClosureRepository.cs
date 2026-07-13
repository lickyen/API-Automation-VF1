using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class ClosureRepository
    {
        private readonly string _conStr;

        public ClosureRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn")!;
        }
        public async Task<int> AddRequestAsync(AddClosureRequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                dto.Lan,
                dto.BranchCode,
                dto.BranchName,
                dto.Region,
                dto.Zone,
                dto.MegaZone,
                dto.DPD,
                dto.LoanAmount,
                dto.Criticality,
                dto.Allocation,
                dto.Bucket,
                dto.IsAmountCollected,
                dto.ReasonAmountCollected,
                dto.TotalPaidAmount,
                dto.IsWaiver,
                dto.LetterId,
                dto.FinalApprover,
                dto.Remarks,
                dto.CreatedBy,
                status,

                dto.TotalOutstanding,
                dto.ExcessAmount,
                dto.POS,
                dto.Interest,
                dto.Charges,
                dto.AmountProposed,
                dto.POSProposed,
                dto.InterestProposed,
                dto.ChargesProposed,
                dto.POSWaiverPer,
                dto.InterestWaiverPer,
                dto.ChargesWaiverPer
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_AddRequest",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<int> ReinitiateRequestAsync(ReinitiateClosureRequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                dto.Id,
                dto.DPD,
                dto.LoanAmount,
                dto.Criticality,
                dto.Allocation,
                dto.Bucket,
                dto.IsAmountCollected,
                dto.ReasonAmountCollected,
                dto.TotalPaidAmount,
                dto.IsWaiver,
                dto.FinalApprover,
                dto.Remarks,
                dto.UpdatedBy,
                status,

                dto.TotalOutstanding,
                dto.ExcessAmount,
                dto.POS,
                dto.Interest,
                dto.Charges,
                dto.AmountProposed,
                dto.POSProposed,
                dto.InterestProposed,
                dto.ChargesProposed,
                dto.POSWaiverPer,
                dto.InterestWaiverPer,
                dto.ChargesWaiverPer
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_ReInitiateRequest",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<int> UpdateAmountCollectedAsync(UpdateAmountCollectedDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                dto.Id,
                dto.Remarks,
                dto.UpdatedBy,
                dto.DiffAmount,
                dto.UpdatedTotal,
                dto.DiffAmountIncWaiver,
                dto.UpdatedTotalIncWaiver,
                dto.TotalPaidAmount,
                Status = status
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_UpdateAmountCollected",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<int> ResubmitRequestAsync(ResubmitClosureRequestDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                dto.Id,
                dto.Remarks,
                dto.UpdatedBy,
                dto.DiffAmount,
                dto.UpdatedTotal,
                dto.DiffAmountIncWaiver,
                dto.UpdatedTotalIncWaiver,
                dto.TotalPaidAmount,
                Status = status
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_ReSubmitRequest",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<int> AddLogAsync(AddClosureLogDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                dto.ReqId,
                dto.Role,
                dto.Action,
                dto.Status,
                dto.Remarks,
                dto.TotalOutstanding,
                dto.ExcessAmount,
                dto.POS,
                dto.Interest,
                dto.Charges,
                dto.AmountProposed,
                dto.POSProposed,
                dto.InterestProposed,
                dto.ChargesProposed,
                dto.POSWaiverPer,
                dto.InterestWaiverPer,
                dto.ChargesWaiverPer,
                dto.CreatedBy
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_AddLog",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<int> ApproveAsync(ClosureApprovalDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                dto.Id,
                dto.Role,
                dto.ApprovalType,
                dto.Status,
                dto.Remarks,
                dto.UserId,

                dto.TotalOutstanding,
                dto.POS,
                dto.Interest,
                dto.Charges,
                dto.AmountProposed,
                dto.POSProposed,
                dto.InterestProposed,
                dto.ChargesProposed,
                dto.POSWaiverPer,
                dto.InterestWaiverPer,
                dto.ChargesWaiverPer
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_Approval",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<ClosureTransaction?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<ClosureTransaction>(
                "sp_newClosure_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);
        }
        public async Task<List<ClosureTransaction>> GetByLanAsync(string lan)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var rows = await connection.QueryAsync<ClosureTransaction>(
                "sp_newClosure_GetByLan",
                new { Lan = lan },
                commandType: CommandType.StoredProcedure);
            return rows.ToList();
        }
        public async Task<List<ClosureTransaction>> GetByUserAsync(string userId, string role, string? status)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var rows = await connection.QueryAsync<ClosureTransaction>(
                "sp_newClosure_GetByUser",
                new
                {
                    UserId = userId,
                    Role = role,
                    Status = status
                },
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }
        public async Task<List<ClosurePayment>> GetPaymentsAsync(int reqId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var rows = await connection.QueryAsync<ClosurePayment>(
                "sp_newClosure_GetPayments",
                new { ReqId = reqId },
                commandType: CommandType.StoredProcedure);
            return rows.ToList();
        }
        public async Task<List<ClosureLog>> GetLogsAsync(int reqId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var rows = await connection.QueryAsync<ClosureLog>(
                "sp_newClosure_GetLogs",
                new { ReqId = reqId },
                commandType: CommandType.StoredProcedure);
            return rows.ToList();
        }
        public async Task<List<ClosureQuestionnaireResponse>> GetQuestionnaireAsync(int reqId, string reqSource)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var rows = await connection.QueryAsync<ClosureQuestionnaireResponse>(
                "sp_newClosure_GetQuestionnaire",
                new { ReqId = reqId, ReqSource = reqSource },
                commandType: CommandType.StoredProcedure);
            return rows.ToList();
        }
        public async Task<int> AddPaymentAsync(AddClosurePaymentDto dto, string? proofPath)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.ReqId,
                dto.PaymentMode,
                dto.Amount,
                dto.PaymentDetails,
                dto.BankDetails,
                ProofPath = proofPath,
                dto.TransactionRef,
                dto.PaymentDate,
                dto.CreatedBy
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_AddPayment",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<int> UpdatePaymentAsync(UpdateClosurePaymentDto dto, string? proofPath)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var parameters = new
            {
                dto.Id,
                dto.PaymentMode,
                dto.Amount,
                dto.PaymentDetails,
                dto.BankDetails,
                ProofPath = proofPath,
                dto.TransactionRef,
                dto.PaymentDate,
                dto.UpdatedBy
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_UpdatePayment",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<int> DeletePaymentAsync(int id, string deletedBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_DeletePayment",
                new { Id = id, DeletedBy = deletedBy },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<List<ClosureQuestionnaireMaster>> GetQuestionnaireMasterAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var rows = await connection.QueryAsync<ClosureQuestionnaireMaster>(
                "sp_newClosure_GetQuestionnaireMaster",
                commandType: CommandType.StoredProcedure);
            return rows.ToList();
        }
        public async Task<int> AddQuestionnaireResponseAsync(
            int reqId,
            string reqSource,
            List<AddQuestionnaireResponseDto> responses,
            string createdBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var table = new DataTable();

            table.Columns.Add("QuestionId", typeof(int));
            table.Columns.Add("QuestionCode", typeof(string));
            table.Columns.Add("ResponseValue", typeof(string));

            foreach (var response in responses)
            {
                table.Rows.Add(
                    response.QuestionId,
                    response.QuestionCode,
                    response.ResponseValue
                );
            }

            var parameters = new DynamicParameters();

            parameters.Add("@ReqId", reqId);
            parameters.Add("@ReqSource", reqSource);
            parameters.Add("@Responses", table.AsTableValuedParameter("dbo.QuestionnaireResponseType"));
            parameters.Add("@CreatedBy", createdBy);

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_AddQuestionnaireResponse",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<int> SubmitQuestionnaireAsync(SubmitQuestionnaireDto dto, string status)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_SubmitQuestionnaire",
                new { Id = dto.Id, Source = dto.Source, CreatedBy = dto.CreatedBy, status },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<string?> GetWaiverApproverAsync(GetWaiverApproverDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<string>(
                "SP_newClosure_GetWaiverApprover",
                new
                {
                    dto.Allocation,
                    dto.Criticality,
                    dto.Bucket,
                    dto.POSAmount,
                    dto.POSWaiverAmount,
                    dto.POSWaiverPerc,
                    dto.InterestWaiverAmount,
                    dto.InterestWaiverPer,
                    dto.ChargesWaiverAmount,
                    dto.ChargesWaiverPer
                },
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<int> UpdateFilePathAsync(int reqId, string fileType, string? filePath)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                Id = reqId,
                FileType = fileType,
                FilePath = filePath
            };

            return await connection.ExecuteAsync(
                "sp_newClosure_UpdateFilePath",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<int> CancelRequestAsync(CancelClosureRequestDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_CancelRequest",
                new
                {
                    dto.Id,
                    dto.Remarks,
                    dto.CancelledBy
                },
                commandType: CommandType.StoredProcedure);
        }

    }
}
