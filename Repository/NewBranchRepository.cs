using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;

namespace ProductivityApi.Repository
{
    public class NewBranchRepository
    {
        private readonly string _conStr;

        public NewBranchRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        #region ADD NEW BRANCH

        public int AddNewBranch(AddNewBranchRequestDto model)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var parameters = new DynamicParameters();

                parameters.Add("@Name", model.Name);
                parameters.Add("@State", model.State);
                parameters.Add("@Zone", model.Zone);
                parameters.Add("@Region", model.Region);
                parameters.Add("@Megazone", model.Megazone);
                parameters.Add("@CreatedBy", model.CreatedBy);

                int branchId = connection.ExecuteScalar<int>(
                    "SP_AddNewBranch",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return branchId;
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        #endregion

        // Repository

        #region GET ALL BRANCHES

        public List<NewBranchMasterModel> GetAllBranches(string? role, string? userId)
        {
            using IDbConnection connection = new SqlConnection(_conStr);

            return connection
                .Query<NewBranchMasterModel>(
                    "SP_GetAllNewBranches",
                    new { Role = role, UserId = userId },
                    commandType: CommandType.StoredProcedure
                )
                .ToList();
        }

        #endregion


        #region GET BRANCH LIST USING SP

        public List<NewBranchListItemDto> GetBranches(GetNewBranchListRequestDto model)
        {
            using var connection = new SqlConnection(_conStr);

            var branchDictionary = new Dictionary<int, NewBranchListItemDto>();

            var result = connection.Query<
                NewBranchListItemDto,
                PendingTransactionDto,
                NewBranchListItemDto
            >(
                "SP_NewBranchGetTransactionsByRole", // stored procedure name
                (branch, transaction) =>
                {
                    if (!branchDictionary.TryGetValue(branch.Id, out var existingBranch))
                    {
                        existingBranch = branch;
                        existingBranch.PendingTransactions = new List<PendingTransactionDto>();
                        branchDictionary.Add(existingBranch.Id, existingBranch);
                    }

                    if (transaction != null)
                    {
                        transaction.PossibleActions =
                            transaction
                                .ActionStatus?.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Trim())
                                .ToList()
                            ?? new List<string>();

                        existingBranch.PendingTransactions.Add(transaction);
                    }

                    return existingBranch;
                },
                param: new { Status = model.Status, Role = model.Role, UserId = model.UserId },
                commandType: CommandType.StoredProcedure,
                splitOn: "TransactionId"
            );

            return branchDictionary.Values.ToList();
        }

        #endregion


        #region GET BRANCH BY ID

        public NewBranchMasterModel? GetBranchById(int id)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var parameters = new DynamicParameters();
                parameters.Add("@Id", id);

                var branch = connection.QueryFirstOrDefault<NewBranchMasterModel>(
                    "SP_GetNewBranchById",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return branch;
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        #endregion


        #region GET BRANCH TRANSACTIONS

        public List<ChecklistTransactionDto> GetBranchTransactions(int tid)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var parameters = new DynamicParameters();
                parameters.Add("@Tid", tid);

                var transactions = connection
                    .Query<ChecklistTransactionDto>(
                        "SP_GetBranchTransactions",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    )
                    .ToList();

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
                catch (SqlException ex)
                {
                    transaction.Rollback();

                    // Custom SQL error
                    if (ex.Number == 50001)
                    {
                        throw new Exception("One or more transactions do not exist.");
                    }

                    throw;
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


        #region CHECK BRANCH COMPLETED

        public bool IsBranchCompleted(int tid)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var parameters = new DynamicParameters();
                parameters.Add("@Tid", tid);

                bool isCompleted = connection.ExecuteScalar<bool>(
                    "SP_CheckBranchCompleted",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return isCompleted;
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        #endregion

        public async Task<(
            IEnumerable<dynamic> Master,
            IEnumerable<dynamic> Transactions
        )> GetNewBranchReportAsync()
        {
            using var conn = new SqlConnection(_conStr);

            await conn.OpenAsync();

            using var multi = await conn.QueryMultipleAsync(
                "SP_GetNewBranchReport",
                commandType: CommandType.StoredProcedure
            );

            var master = await multi.ReadAsync();
            var transactions = await multi.ReadAsync();

            return (master, transactions);
        }

        #region UPDATE BRANCH CODE

        public bool UpdateBranchCode(UpdateBranchCodeRequestDto model)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var parameters = new DynamicParameters();

                parameters.Add("@Id", model.Id);
                parameters.Add("@BranchCode", model.BranchCode);
                parameters.Add("@UpdatedBy", model.UpdatedBy);

                connection.Execute(
                    "SP_UpdateBranchCode",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        #endregion
        #region ADD BRANCH USER

        public int AddBranchUser(AddBranchUserRequestDto model)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var parameters = new DynamicParameters();

                parameters.Add("@Bid", model.Bid);
                parameters.Add("@BranchName", model.BranchName);
                parameters.Add("@Role", model.Role);
                parameters.Add("@UserId", model.UserId);

                int id = connection.ExecuteScalar<int>(
                    "SP_AddBranchUser",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return id;
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        #endregion

        #region GET BRANCH USERS BY BID

        public List<BranchUserDto> GetBranchUsersByBid(int bid)
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var parameters = new DynamicParameters();
                parameters.Add("@Bid", bid);

                return connection
                    .Query<BranchUserDto>(
                        "SP_GetBranchUsersByBid",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    )
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        #endregion

        public async Task<(List<NewBranchEmailDeptTriggerDto> Grouped, List<NewBranchEmailLogItemDto> Individual)>
    GetEmailTriggersAsync(int tid, IEnumerable<int> updatedChecklistIds)
        {
            using IDbConnection connection = new SqlConnection(_conStr);

            var csv = string.Join(",", updatedChecklistIds);

            using var multi = await connection.QueryMultipleAsync(
                "SP_GetNewBranchEmailTriggers",
                new { Tid = tid, UpdatedChecklistIds = csv },
                commandType: CommandType.StoredProcedure
            );

            var grouped = (await multi.ReadAsync<NewBranchEmailDeptTriggerDto>()).AsList();
            var individual = (await multi.ReadAsync<NewBranchEmailLogItemDto>()).AsList();

            return (grouped, individual);
        }

        public async Task<List<NewBranchEmailRecipientDto>> GetEmailRecipientsAsync(
            int tid, string departmentGroup)
        {
            using IDbConnection connection = new SqlConnection(_conStr);

            var result = await connection.QueryAsync<NewBranchEmailRecipientDto>(
                "SP_GetNewBranchEmailRecipients",
                new { Tid = tid, DepartmentGroup = departmentGroup },
                commandType: CommandType.StoredProcedure
            );

            return result.AsList();
        }

        public async Task<NewBranchMasterInfoDto?> GetBranchMasterByTidAsync(int tid)
        {
            using IDbConnection connection = new SqlConnection(_conStr);

            return await connection.QueryFirstOrDefaultAsync<NewBranchMasterInfoDto>(
                "SELECT Id, Name, Region, ISNULL(BranchCode,'') AS BranchCode FROM NewBranchMaster WHERE Id = @Tid",
                new { Tid = tid }
            );
        }

        public async Task LogEmailSentAsync(
            int tid, int triggerChecklistId, int notifiedChecklistId, string departmentGroup)
        {
            using IDbConnection connection = new SqlConnection(_conStr);

            await connection.ExecuteAsync(
                "SP_LogNewBranchEmail",
                new
                {
                    Tid = tid,
                    TriggerChecklistId = triggerChecklistId,
                    NotifiedChecklistId = notifiedChecklistId,
                    DepartmentGroup = departmentGroup
                },
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task<(List<NewBranchEmailDeptTriggerDto> Grouped, List<NewBranchEmailLogItemDto> Individual)>
    GetCreationEmailTriggersAsync(int tid)
        {
            using IDbConnection connection = new SqlConnection(_conStr);

            using var multi = await connection.QueryMultipleAsync(
                "SP_GetNewBranchCreationEmailTriggers",
                new { Tid = tid },
                commandType: CommandType.StoredProcedure
            );

            var grouped = (await multi.ReadAsync<NewBranchEmailDeptTriggerDto>()).AsList();
            var individual = (await multi.ReadAsync<NewBranchEmailLogItemDto>()).AsList();

            return (grouped, individual);
        }
    }
}
