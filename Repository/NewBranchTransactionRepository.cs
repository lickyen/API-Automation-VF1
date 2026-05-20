using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class NewBranchTransactionRepository
    {
        private readonly string _conStr;

        public NewBranchTransactionRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        #region GET BRANCH TRANSACTIONS

        public List<ChecklistTransactionDto> GetBranchTransactions(int tid)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var parameters = new DynamicParameters();
                parameters.Add("@Tid", tid);

                var transactions = connection.Query<ChecklistTransactionDto>(
                    "SP_GetBranchTransactions",
                    parameters,
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return transactions;
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        #endregion


        #region UPDATE TRANSACTIONS

        public bool UpdateTransactions(UpdateBranchTransactionsRequestDto model)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                connection.Open();

                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var item in model.Transactions)
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@TransactionId", item.TransactionId);
                        parameters.Add("@ChecklistId", item.ChecklistId);
                        parameters.Add("@CurrentStatus", item.CurrentStatus);
                        parameters.Add("@OverallStatus", item.OverallStatus);
                        parameters.Add("@Remarks", item.Remarks);
                        parameters.Add("@UpdatedBy", model.UpdatedBy);

                        connection.Execute(
                            "SP_UpdateBranchTransaction",
                            parameters,
                            commandType: CommandType.StoredProcedure,
                            transaction: transaction
                        );
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        #endregion
    }
}