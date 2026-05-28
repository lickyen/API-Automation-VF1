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

        public List<NewBranchMasterModel> GetAllBranches()
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var branches = connection
                    .Query<NewBranchMasterModel>(
                        "SP_GetAllNewBranches",
                        commandType: CommandType.StoredProcedure
                    )
                    .ToList();

                return branches;
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        #endregion


        #region GET BRANCH LIST

        public List<NewBranchListItemDto> GetBranches(GetNewBranchListRequestDto model)
        {
            using var connection = new SqlConnection(_conStr);

            string roleColumn = model.Role switch
            {
                "Admin" or "Admin1" or "Admin2" => "nb.AdminStatus",

                "HR" or "HR1" or "HR2" => "nb.HRStatus",

                "IT" or "IT1" or "IT2" => "nb.ITStatus",

                "Finance" or "Finance1" or "Finance2" => "nb.FinanceStatus",

                "Ops" or "Ops1" or "Ops2" => "nb.OPSStatus",

                "Marketing" or "Marketing1" or "Marketing2" => "nb.MarketingStatus",

                "RCHRBH" => "nb.RCHRCBStatus",

                // Checker-only role
                "BH" => "NULL",

                _ => "nb.Status",
            };

            string departmentGroupCase =
                @"
        CASE
            WHEN @Role LIKE 'Admin%' THEN 'Admin'
            WHEN @Role LIKE 'HR%' THEN 'HR'
            WHEN @Role LIKE 'IT%' THEN 'IT'
            WHEN @Role LIKE 'Finance%' THEN 'Finance'
            WHEN @Role LIKE 'Ops%' THEN 'Ops'
            WHEN @Role LIKE 'Marketing%' THEN 'Marketing'
            ELSE @Role
        END
    ";

            string query =
                $@"
        SELECT
            nb.Id,
            nb.Name,
            nb.State,
            nb.Zone,
            nb.Region,
            nb.Megazone,
            nb.Status,

            {roleColumn} AS TeamStatus,

            nt.Id AS TransactionId,
            nt.ChecklistId,
            cm.ChecklistName,
            nt.CurrentStatus,
            nt.Remarks,
            cm.ActionStatus

        FROM NewBranchMaster nb

        INNER JOIN NewBranchTransaction nt
            ON nb.Id = nt.Tid

        INNER JOIN NewBranchChecklistMaster cm
            ON nt.ChecklistId = cm.Id

        WHERE
        (
            @Status IS NULL
            OR {roleColumn} IS NULL
            OR {roleColumn} = @Status
        )

        AND
        (
            @Role IS NULL

            OR

            (
                cm.DepartmentGroup = ({departmentGroupCase})
            )

            OR

            (
                cm.Team = @Role
            )
        )

        AND
        (
            -- STAGE VIEW ROLES
            (
                cm.DepartmentGroup = ({departmentGroupCase})

                AND {roleColumn} = 'Pending'

                AND
                (
                    (
                        cm.ParentChecklistId IS NULL

                        AND
                        (
                            cm.PrereqStatus = 0

                            OR

                            (
                                cm.PrereqStatus = 1

                                AND NOT EXISTS
                                (
                                    SELECT 1
                                    FROM NewBranchTransaction nt2

                                    INNER JOIN NewBranchChecklistMaster cm2
                                        ON nt2.ChecklistId = cm2.Id

                                    WHERE
                                        nt2.Tid = nt.Tid

                                        AND cm2.Team = cm.PrereqName

                                        AND nt2.CurrentStatus = 'Pending'
                                )
                            )
                        )
                    )

                    OR

                    (
                        cm.ParentChecklistId IS NOT NULL

                        AND EXISTS
                        (
                            SELECT 1
                            FROM NewBranchTransaction ntParent

                            WHERE
                                ntParent.Tid = nt.Tid

                                AND ntParent.ChecklistId = cm.ParentChecklistId

                                AND ntParent.CurrentStatus <> 'Pending'
                        )
                    )
                )
            )

            OR

            -- INDIVIDUAL TEAM VIEW
            (
                cm.Team = @Role

                AND nt.CurrentStatus = 'Pending'

                AND
                (
                    (
                        cm.ParentChecklistId IS NULL

                        AND
                        (
                            cm.PrereqStatus = 0

                            OR

                            (
                                cm.PrereqStatus = 1

                                AND NOT EXISTS
                                (
                                    SELECT 1
                                    FROM NewBranchTransaction nt2

                                    INNER JOIN NewBranchChecklistMaster cm2
                                        ON nt2.ChecklistId = cm2.Id

                                    WHERE
                                        nt2.Tid = nt.Tid

                                        AND cm2.Team = cm.PrereqName

                                        AND nt2.CurrentStatus = 'Pending'
                                )
                            )
                        )
                    )

                    OR

                    (
                        cm.ParentChecklistId IS NOT NULL

                        AND EXISTS
                        (
                            SELECT 1
                            FROM NewBranchTransaction ntParent

                            WHERE
                                ntParent.Tid = nt.Tid

                                AND ntParent.ChecklistId = cm.ParentChecklistId

                                AND ntParent.CurrentStatus <> 'Pending'
                        )
                    )
                )
            )
        )

        ORDER BY
            nb.Id,
            nt.Id
    ";

            var branchDictionary = new Dictionary<int, NewBranchListItemDto>();

            var result = connection.Query<
                NewBranchListItemDto,
                PendingTransactionDto,
                NewBranchListItemDto
            >(
                query,
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
                new { Status = model.Status, Role = model.Role },
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
    }
}