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
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var parameters = new DynamicParameters();

                parameters.Add("@Role", model.Role);
                parameters.Add("@Status", model.Status);

                var branches = connection.Query<NewBranchListItemDto>(
                    "SP_GetNewBranches",
                    parameters,
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return branches;
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
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