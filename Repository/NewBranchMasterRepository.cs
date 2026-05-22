using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class NewBranchMasterRepository
    {
        private readonly string _conStr;

        public NewBranchMasterRepository(IConfiguration configuration)
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

        public List<NewBranchListItemDto> GetBranches(GetNewBranchListRequestDto model)
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

                    /* =========================================
                    FILTER BY BRANCH STATUS
                    ========================================= */
                    (
                        @Status IS NULL
                        OR {roleColumn} IS NULL
                        OR {roleColumn} = @Status
                    )

                    /* =========================================
                    FILTER BY ROLE
                    Admin includes DepartmentGroup
                    Others use Team
                    ========================================= */
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

                    /* =========================================
                    ONLY PENDING TASKS
                    ========================================= */
                    AND nt.CurrentStatus = 'Pending'

                    /* =========================================
                    WORKFLOW LOGIC
                    ========================================= */
                    AND
                    (

                        /* =====================================
                        NORMAL TASKS
                        ===================================== */
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

                                            /* IMPORTANT:
                                            dependency should use Team
                                            NOT DepartmentGroup
                                            */
                                            AND cm2.Team = cm.PrereqName

                                            AND nt2.CurrentStatus = 'Pending'
                                    )
                                )
                            )
                        )

                        OR

                        /* =====================================
                        CHECKER TASKS
                        ONLY depend on parent completion
                        ===================================== */
                        (
                            cm.ParentChecklistId IS NOT NULL

                            AND EXISTS
                            (
                                SELECT 1
                                FROM NewBranchTransaction ntParent

                                WHERE
                                    ntParent.Tid = nt.Tid

                                    AND ntParent.ChecklistId = cm.ParentChecklistId

                                    /* Parent task completed */
                                    AND ntParent.CurrentStatus <> 'Pending'
                            )
                        )
                    )

                ORDER BY nb.Id, nt.Id
            ";

            var branchDictionary = new Dictionary<int, NewBranchListItemDto>();

            var result = connection.Query<
                NewBranchListItemDto,
                PendingTransactionDto,
                NewBranchListItemDto>
            (
                query,
                (branch, transaction) =>
                {
                    if (!branchDictionary.TryGetValue(branch.Id, out var existingBranch))
                    {
                        existingBranch = branch;

                        existingBranch.PendingTransactions =
                            new List<PendingTransactionDto>();

                        branchDictionary.Add(existingBranch.Id, existingBranch);
                    }

                    if (transaction != null)
                    {
                        existingBranch.PendingTransactions.Add(transaction);
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
    }
}