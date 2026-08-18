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

        public async Task<T> ExecuteInTransactionAsync<T>(Func<SqlConnection, SqlTransaction, Task<T>> action)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var result = await action(connection, transaction);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> GetOrCreateRetentionIdAsync(string lan, string source, int? reqId, string createdBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            const string sql = @"
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @RetentionId INT;

SELECT TOP 1 @RetentionId = Id
FROM dbo.Closure_Retention WITH (UPDLOCK, HOLDLOCK)
WHERE LAN = @Lan
  AND
  (
      ApprovedDt IS NULL
      OR ExpiredDt >= GETDATE()
  )
ORDER BY
    CASE WHEN ApprovedDt IS NOT NULL THEN 0 ELSE 1 END,
    ApprovedDt DESC,
    Id DESC;

IF @RetentionId IS NULL
BEGIN
    INSERT INTO dbo.Closure_Retention
    (
        LAN,
        Source,
        ReqId,
        CreatedBy,
        CreatedDt
    )
    VALUES
    (
        @Lan,
        @Source,
        @ReqId,
        @CreatedBy,
        GETDATE()
    );

    SET @RetentionId = CAST(SCOPE_IDENTITY() AS INT);
END
ELSE
BEGIN
    UPDATE dbo.Closure_Retention
    SET
        ReqId = CASE
            WHEN @ReqId IS NULL THEN ReqId
            ELSE ISNULL(ReqId, @ReqId)
        END,
        UpdatedBy = @CreatedBy,
        UpdatedDt = GETDATE()
    WHERE Id = @RetentionId
      AND Source = @Source;
END

COMMIT TRANSACTION;

SELECT @RetentionId;";

            return await connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    Lan = lan,
                    Source = source,
                    ReqId = reqId,
                    CreatedBy = createdBy
                });
        }

        public async Task<bool> IsRetentionApprovedActiveAsync(int retentionId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            const string sql = @"
SELECT CASE
    WHEN EXISTS
    (
        SELECT 1
        FROM dbo.Closure_Retention
        WHERE Id = @RetentionId
          AND ApprovedDt IS NOT NULL
          AND ExpiredDt >= GETDATE()
    )
    THEN 1
    ELSE 0
END;";

            return await connection.ExecuteScalarAsync<bool>(
                sql,
                new { RetentionId = retentionId });
        }

        public async Task<int> ApplyExistingRetentionApprovalAsync(int id, int retentionId, string updatedBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_ApplyExistingRetentionApproval",
                new { Id = id, RetentionId = retentionId, UpdatedBy = updatedBy },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> ApplyExistingRetentionApprovalAsync(int id, int retentionId, string updatedBy, SqlConnection connection, SqlTransaction transaction)
        {
            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_ApplyExistingRetentionApproval",
                new { Id = id, RetentionId = retentionId, UpdatedBy = updatedBy },
                transaction,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> AddRequestAsync(AddClosureRequestDto dto, string status, bool isRetention, int? retentionId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                dto.Lan,
                dto.CustId,
                dto.CustName,
                dto.CustMobile,
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
                IsRetention = isRetention,
                RetentionId = retentionId,
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
                dto.ChargesWaiverPer,

                dto.BusinessProfile,
                dto.CurrentPOS,
                dto.CurrentMOB,
                dto.ProductDesc,
                dto.PromotionDesc,
                dto.DisbursedAmt,
                dto.TranchePendingAmount,
                dto.CurrentLTVPer,
                dto.CurrentLTVAmt,
                dto.CurrentDSR,
                dto.Income,
                dto.CIBILScore,
                dto.Obligation,
                dto.Tenure,
                dto.Valuation,
                dto.EndUseDesc,
                dto.TypeOfProperty,
                dto.CurrentROI,
                dto.LastEMIPaid,
                dto.EMIPending
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_AddRequest",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> AddRequestAsync(AddClosureRequestDto dto, string status, bool isRetention, int? retentionId, SqlConnection connection, SqlTransaction transaction)
        {
            var parameters = new
            {
                dto.Lan,
                dto.CustId,
                dto.CustName,
                dto.CustMobile,
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
                IsRetention = isRetention,
                RetentionId = retentionId,
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
                dto.ChargesWaiverPer,

                dto.BusinessProfile,
                dto.CurrentPOS,
                dto.CurrentMOB,
                dto.ProductDesc,
                dto.PromotionDesc,
                dto.DisbursedAmt,
                dto.TranchePendingAmount,
                dto.CurrentLTVPer,
                dto.CurrentLTVAmt,
                dto.CurrentDSR,
                dto.Income,
                dto.CIBILScore,
                dto.Obligation,
                dto.Tenure,
                dto.Valuation,
                dto.EndUseDesc,
                dto.TypeOfProperty,
                dto.CurrentROI,
                dto.LastEMIPaid,
                dto.EMIPending
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_AddRequest",
                parameters,
                transaction,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> ReinitiateRequestAsync(ReinitiateClosureRequestDto dto, string status, bool isRetention, int? retentionId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new
            {
                dto.Id,
                dto.CustId,
                dto.CustName,
                dto.CustMobile,
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
                IsRetention = isRetention,
                RetentionId = retentionId,
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
                dto.ChargesWaiverPer,

                dto.BusinessProfile,
                dto.CurrentPOS,
                dto.CurrentMOB,
                dto.ProductDesc,
                dto.PromotionDesc,
                dto.DisbursedAmt,
                dto.TranchePendingAmount,
                dto.CurrentLTVPer,
                dto.CurrentLTVAmt,
                dto.CurrentDSR,
                dto.Income,
                dto.CIBILScore,
                dto.Obligation,
                dto.Tenure,
                dto.Valuation,
                dto.EndUseDesc,
                dto.TypeOfProperty,
                dto.CurrentROI,
                dto.LastEMIPaid,
                dto.EMIPending
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_ReInitiateRequest",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> ReinitiateRequestAsync(ReinitiateClosureRequestDto dto, string status, bool isRetention, int? retentionId, SqlConnection connection, SqlTransaction transaction)
        {
            var parameters = new
            {
                dto.Id,
                dto.CustId,
                dto.CustName,
                dto.CustMobile,
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
                IsRetention = isRetention,
                RetentionId = retentionId,
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
                dto.ChargesWaiverPer,

                dto.BusinessProfile,
                dto.CurrentPOS,
                dto.CurrentMOB,
                dto.ProductDesc,
                dto.PromotionDesc,
                dto.DisbursedAmt,
                dto.TranchePendingAmount,
                dto.CurrentLTVPer,
                dto.CurrentLTVAmt,
                dto.CurrentDSR,
                dto.Income,
                dto.CIBILScore,
                dto.Obligation,
                dto.Tenure,
                dto.Valuation,
                dto.EndUseDesc,
                dto.TypeOfProperty,
                dto.CurrentROI,
                dto.LastEMIPaid,
                dto.EMIPending
            };

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_ReInitiateRequest",
                parameters,
                transaction,
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

        public async Task<int> UpdateAmountCollectedAsync(UpdateAmountCollectedDto dto, string status, SqlConnection connection, SqlTransaction transaction)
        {
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
                transaction,
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

        public async Task<int> ResubmitRequestAsync(ResubmitClosureRequestDto dto, string status, SqlConnection connection, SqlTransaction transaction)
        {
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
                transaction,
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

        public async Task<int> AddLogAsync(AddClosureLogDto dto, SqlConnection connection, SqlTransaction transaction)
        {
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
                transaction,
                commandType: CommandType.StoredProcedure);
        }


        public async Task<int> RetentionApprovalAsync(ClosureRetentionApprovalDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_RetentionApproval",
                new
                {
                    dto.RetentionId,
                    dto.Role,
                    dto.Status,
                    dto.Remarks,
                    dto.UserId
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int?> GetClosureRequestIdByRetentionIdAsync(int retentionId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            const string sql = @"
SELECT TOP 1 Id
FROM dbo.Closure_Transactions
WHERE RetentionId = @RetentionId
  AND IsActive = 1
ORDER BY Id DESC;";

            return await connection.ExecuteScalarAsync<int?>(
                sql,
                new { RetentionId = retentionId });
        }

        public async Task<int> StartRetentionApprovalAsync(int retentionId, string updatedBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_StartRetentionApproval",
                new { RetentionId = retentionId, UpdatedBy = updatedBy },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> StartRetentionApprovalAsync(int retentionId, string updatedBy, SqlConnection connection, SqlTransaction transaction)
        {
            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_StartRetentionApproval",
                new { RetentionId = retentionId, UpdatedBy = updatedBy },
                transaction,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CopsApprovalAsync(ClosureCopsApprovalDto dto)
        {
            try
            {
                using var connection = new SqlConnection(_conStr);
                await connection.OpenAsync();

                var parameters = new
                {
                    dto.Id,
                    dto.Status,
                    dto.Remarks,
                    dto.UserId
                };

                return await connection.ExecuteScalarAsync<int>(
                    "sp_newClosure_CopsApproval",
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
        public async Task<List<ClosureQuestionnaireResponse>> GetQuestionnaireAsync(int? reqId, string? reqSource, int? retentionId = null)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();
            var rows = await connection.QueryAsync<ClosureQuestionnaireResponse>(
                "sp_newClosure_GetQuestionnaire",
                new { RetentionId = retentionId, ReqId = reqId, ReqSource = reqSource },
                commandType: CommandType.StoredProcedure);
            return rows.ToList();
        }
        public async Task<List<ClosureCamResponse>> GetCAMAsync(int retentionId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var rows = await connection.QueryAsync<ClosureCamResponse>(
                "sp_newClosure_GetCAM",
                new { RetentionId = retentionId },
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }
        public async Task<List<ClosureCamMaster>> GetCAMMasterAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var rows = await connection.QueryAsync<ClosureCamMaster>(
                "sp_newClosure_GetCAMMaster",
                commandType: CommandType.StoredProcedure);

            return rows.ToList();
        }
        public async Task<int> SubmitCAMAsync(SubmitClosureCamDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var table = new DataTable();

            table.Columns.Add("FieldId", typeof(int));
            table.Columns.Add("FieldCode", typeof(string));
            table.Columns.Add("ResponseValue", typeof(string));

            foreach (var response in dto.Responses)
            {
                table.Rows.Add(
                    response.FieldId,
                    response.FieldCode,
                    response.ResponseValue
                );
            }

            var parameters = new DynamicParameters();
            parameters.Add("@RetentionId", dto.RetentionId);
            parameters.Add("@Responses", table.AsTableValuedParameter("dbo.CAMResponseType"));
            parameters.Add("@CreatedBy", dto.CreatedBy);
            parameters.Add("@CreatedRole", dto.CreatedRole);

            return await connection.ExecuteScalarAsync<int>(
                "sp_newClosure_SubmitCAM",
                parameters,
                commandType: CommandType.StoredProcedure);
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
            int? reqId,
            string? reqSource,
            int? retentionId,
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

            parameters.Add("@RetentionId", retentionId);
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
                new { Id = dto.Id, RetentionId = dto.RetentionId, Source = dto.Source, CreatedBy = dto.CreatedBy, status },
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

        public async Task<int> UpdateFilePathAsync(int reqId, string fileType, string? filePath, SqlConnection connection, SqlTransaction transaction)
        {
            var parameters = new
            {
                Id = reqId,
                FileType = fileType,
                FilePath = filePath
            };

            return await connection.ExecuteAsync(
                "sp_newClosure_UpdateFilePath",
                parameters,
                transaction,
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

        public async Task<PagedResult<ClosureTransaction>> GetAllAsync(PaginationRequestDto request)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            using var multi = await connection.QueryMultipleAsync(
                "dbo.sp_newClosure_GetAll",
                new
                {
                    request.PageNumber,
                    request.PageSize,
                    request.Search
                },
                commandType: CommandType.StoredProcedure
            );

            var totalRecords = await multi.ReadSingleAsync<int>();
            var items = (await multi.ReadAsync<ClosureTransaction>()).ToList();

            return new PagedResult<ClosureTransaction>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalRecords = totalRecords
            };
        }

    }
}
