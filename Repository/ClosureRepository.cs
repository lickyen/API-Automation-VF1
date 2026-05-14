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
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();

                var parameters = new
                {
                    dto.Lan,
                    dto.IsMainLan,
                    dto.ParentLoanId,
                    dto.ParallelLoan,
                    dto.ParallelLoanClosure,
                    dto.BranchCode,
                    dto.BranchName,
                    dto.Region,
                    dto.Zone,
                    dto.MegaZone,
                    dto.DPD,
                    dto.LoanAmount,
                    dto.Action,
                    dto.Allocation,
                    dto.Bucket,
                    dto.IsAmountCollected,
                    dto.ReasonAmountCollected,
                    dto.IsWaiver,
                    dto.AckPath,
                    dto.ChecklistPath,
                    dto.FCLetterPath,
                    dto.LetterId,
                    dto.FinalApprover,
                    dto.Remarks,
                    dto.CreatedBy,
                    status,

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
                    "sp_AddClosureRequest",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }
        public async Task<int> ReinitiateRequestAsync(ReinitiateClosureRequestDto dto, string status)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();

                var parameters = new
                {
                    dto.Id,
                    dto.DPD,
                    dto.LoanAmount,
                    dto.Action,
                    dto.Allocation,
                    dto.Bucket,
                    dto.IsAmountCollected,
                    dto.ReasonAmountCollected,
                    dto.IsWaiver,
                    dto.AckPath,
                    dto.ChecklistPath,
                    dto.FCLetterPath,
                    dto.FinalApprover,
                    dto.Remarks,
                    dto.UpdatedBy,
                    status,

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

                return await connection.ExecuteAsync(
                    "sp_ReinitiateClosureRequest",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }
        public async Task<int> UpdateAmountCollectedAsync(UpdateAmountCollectedDto dto, string status)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();

                var parameters = new
                {
                    dto.Id,
                    dto.AckPath,
                    dto.ChecklistPath,
                    dto.FCLetterPath,
                    dto.Remarks,
                    dto.UpdatedBy,
                    status
                };

                return await connection.ExecuteAsync(
                    "sp_UpdateClosureAmountCollected",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }
        public async Task<int> ResubmitRequestAsync(ResubmitClosureRequestDto dto, string status)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();

                var parameters = new
                {
                    dto.Id,
                    dto.AckPath,
                    dto.ChecklistPath,
                    dto.FCLetterPath,
                    dto.Remarks,
                    dto.UpdatedBy,
                    status
                };

                return await connection.ExecuteAsync(
                    "sp_ResubmitClosureRequest",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<int> AddLogAsync(AddClosureLogDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            var parameters = new DynamicParameters();
            parameters.Add("@ReqId", dto.ReqId);
            parameters.Add("@Role", dto.Role);
            parameters.Add("@Action", dto.Action);
            parameters.Add("@Status", dto.Status);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@Total_Outstanding", dto.TotalOutstanding);
            parameters.Add("@Excess_Amount", dto.ExcessAmount);
            parameters.Add("@POS", dto.POS);
            parameters.Add("@Interest", dto.Interest);
            parameters.Add("@Charges", dto.Charges);
            parameters.Add("@Amount_Proposed", dto.AmountProposed);
            parameters.Add("@POS_Proposed", dto.POSProposed);
            parameters.Add("@Interest_Proposed", dto.InterestProposed);
            parameters.Add("@Charges_Proposed", dto.ChargesProposed);
            parameters.Add("@POS_WaiverPer", dto.POSWaiverPer);
            parameters.Add("@Interest_WaiverPer", dto.InterestWaiverPer);
            parameters.Add("@Charges_WaiverPer", dto.ChargesWaiverPer);
            parameters.Add("@CreatedBy", dto.CreatedBy);

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_AddLog",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> ApproveAsync(ClosureApprovalDto dto)
        {
            try
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

                // stored proc returns int status (1 success, -1 not found, -2 invalid role etc.)
                return await connection.ExecuteScalarAsync<int>(
                    "sp_ApproveClosureRequest",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<ClosureTransaction?> GetByIdAsync(int id)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<ClosureTransaction>(
                    "sp_GetClosureById",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<List<ClosurePayment>> GetPaymentsAsync(int reqId)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();
                var rows = await connection.QueryAsync<ClosurePayment>(
                    "sp_GetClosurePayments",
                    new { ReqId = reqId },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<List<ClosureLog>> GetLogsAsync(int reqId)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();
                var rows = await connection.QueryAsync<ClosureLog>(
                    "sp_GetClosureLogs",
                    new { ReqId = reqId },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<List<ClosureQuestionnaireResponse>> GetQuestionnaireAsync(int reqId, string reqSource)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();
                var rows = await connection.QueryAsync<ClosureQuestionnaireResponse>(
                    "sp_GetClosureQuestionnaire",
                    new { ReqId = reqId, ReqSource = reqSource },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<List<ClosureTransaction>> GetListByUserAsync(string userId, string role)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();
                var rows = await connection.QueryAsync<ClosureTransaction>(
                    "sp_GetClosureListByUser",
                    new { UserId = userId, Role = role },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<List<ClosureTransaction>> GetCompletedByUserAsync(string userId)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();
                var rows = await connection.QueryAsync<ClosureTransaction>(
                    "sp_GetCompletedClosureByUser",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<List<ClosureTransaction>> SearchByLanAsync(string lan)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();
                var rows = await connection.QueryAsync<ClosureTransaction>(
                    "sp_SearchClosureByLan",
                    new { Lan = lan },
                    commandType: CommandType.StoredProcedure);
                return rows.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<int> AddPaymentAsync(AddClosurePaymentDto dto)
        {
            try
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
                    dto.ProofPath,
                    dto.TransactionRef,
                    dto.PaymentDate,
                    dto.CreatedBy
                };

                return await connection.ExecuteScalarAsync<int>(
                    "sp_AddClosurePayment",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<int> UpdatePaymentAsync(UpdateClosurePaymentDto dto)
        {
            try
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
                    dto.ProofPath,
                    dto.TransactionRef,
                    dto.PaymentDate,
                    dto.UpdatedBy
                };

                return await connection.ExecuteAsync(
                    "sp_UpdateClosurePayment",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<int> DeletePaymentAsync(int id, string deletedBy)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();
                return await connection.ExecuteAsync(
                    "sp_DeleteClosurePayment",
                    new { Id = id, DeletedBy = deletedBy },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<int> AddQuestionnaireResponseAsync(AddQuestionnaireResponseDto dto)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();
                var parameters = new
                {
                    dto.ReqId,
                    dto.ReqSource,
                    dto.QuestionId,
                    dto.QuestionCode,
                    dto.ResponseValue,
                    dto.CreatedBy
                };

                return await connection.ExecuteScalarAsync<int>(
                    "sp_AddClosureQuestionnaireResponse",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        public async Task<int> SubmitQuestionnaireAsync(SubmitQuestionnaireDto dto, string status)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    "sp_SubmitClosureQuestionnaire",
                    new { Id = dto.Id, Source = dto.Source, CreatedBy = dto.CreatedBy, status },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }
    }
}
