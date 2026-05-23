using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

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


        #region GET BRANCH LIST

        public List<NewBranchListItemDto> GetBranches(
            GetNewBranchListRequestDto model)
        {
            using var connection = new SqlConnection(_conStr);

            string roleColumn = model.Role switch
            {
                "Admin" => "nb.AdminStatus",
                "HR" => "nb.HRStatus",
                "IT" => "nb.ITStatus",
                "Finance & Compliance" => "nb.FinanceStatus",
                "Ops" => "nb.OPSStatus",
                "Marketing" => "nb.MarketingStatus",
                "RCHRBH" => "nb.RCHRCBStatus",

                // Checker-only role
                "BH" => "NULL",

                _ => "nb.Status"
            };

            string query = $@"
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
                    nt.Remarks

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
                            @Role = 'Admin'
                            AND cm.DepartmentGroup = 'Admin'
                        )

                        OR

                        (
                            @Role <> 'Admin'
                            AND cm.Team = @Role
                        )
                    )

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

                ORDER BY nb.Id, nt.Id
            ";

            var branchDictionary =
                new Dictionary<int, NewBranchListItemDto>();

            var result = connection.Query<
                NewBranchListItemDto,
                PendingTransactionDto,
                NewBranchListItemDto>
            (
                query,
                (branch, transaction) =>
                {
                    if (!branchDictionary.TryGetValue(
                        branch.Id,
                        out var existingBranch))
                    {
                        existingBranch = branch;

                        existingBranch.PendingTransactions =
                            new List<PendingTransactionDto>();

                        branchDictionary.Add(
                            existingBranch.Id,
                            existingBranch);
                    }

                    if (transaction != null)
                    {
                        existingBranch.PendingTransactions
                            .Add(transaction);
                    }

                    return existingBranch;
                },
                new
                {
                    Status = model.Status,
                    Role = model.Role
                },
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
                using IDbConnection connection =
                    new SqlConnection(_conStr);

                var parameters = new DynamicParameters();
                parameters.Add("@Id", id);

                var branch =
                    connection.QueryFirstOrDefault<NewBranchMasterModel>(
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
                using IDbConnection connection =
                    new SqlConnection(_conStr);

                var parameters = new DynamicParameters();
                parameters.Add("@Tid", tid);

                var transactions =
                    connection.Query<ChecklistTransactionDto>(
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

        public bool UpdateTransactions(
            UpdateBranchTransactionsRequestDto model)
        {
            try
            {
                using IDbConnection connection =
                    new SqlConnection(_conStr);

                connection.Open();

                using var transaction =
                    connection.BeginTransaction();

                try
                {
                    foreach (var item in model.Transactions)
                    {
                        var parameters = new DynamicParameters();

                        parameters.Add(
                            "@TransactionId",
                            item.TransactionId);

                        parameters.Add(
                            "@ChecklistId",
                            item.ChecklistId);

                        parameters.Add(
                            "@CurrentStatus",
                            item.CurrentStatus);

                        parameters.Add(
                            "@Remarks",
                            item.Remarks);

                        parameters.Add(
                            "@UpdatedBy",
                            model.UpdatedBy);

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


        #region CHECK BRANCH COMPLETED

        public bool IsBranchCompleted(int tid)
        {
            try
            {
                using IDbConnection connection =
                    new SqlConnection(_conStr);

                var parameters = new DynamicParameters();
                parameters.Add("@Tid", tid);

                bool isCompleted =
                    connection.ExecuteScalar<bool>(
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
    }
}