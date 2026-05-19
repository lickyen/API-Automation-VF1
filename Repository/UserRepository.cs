using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class UserRepository
    {
        private readonly string _conStr;

        public UserRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public List<UserModel> GetAllUsers()
        {
            using var connection = new SqlConnection(_conStr);

            var users = connection.Query<UserModel>(
                "sp_getUsersList",
                commandType: CommandType.StoredProcedure
            ).ToList();

            return users;
        }

        public UserModel? GetUser(string userId)
        {
            using var connection = new SqlConnection(_conStr);

            var user = connection.QueryFirstOrDefault<UserModel>(
                "sp_getUser",
                new { UserID = userId },
                commandType: CommandType.StoredProcedure
            );

            return user;
        }

        public int AddUser(AddUserDTO addUserDTO)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@UserID", addUserDTO.UserId);
            parameters.Add("@Name", addUserDTO.Name);
            parameters.Add("@BranchID", addUserDTO.BranchId);
            parameters.Add("@BranchName", addUserDTO.BranchName);
            parameters.Add("@Region", addUserDTO.Region);
            parameters.Add("@Zone", addUserDTO.Zone);
            parameters.Add("@Megazone", addUserDTO.MegaZone);
            parameters.Add("@Role", addUserDTO.Role);
            parameters.Add("@EmailID", addUserDTO.Email);
            parameters.Add("@Mobile", addUserDTO.Mobile);
            parameters.Add("@CreatedBy", addUserDTO.CreatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            connection.Execute(
                "sp_insertUser",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public int UpdateUser(UpdateUserDTO user)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@UserID", user.UserId);
            parameters.Add("@Name", user.Name);
            parameters.Add("@Role", user.Role);
            parameters.Add("@BranchID", user.BranchId);
            parameters.Add("@BranchName", user.BranchName);
            parameters.Add("@Region", user.Region);
            parameters.Add("@Zone", user.Zone);
            parameters.Add("@Megazone", user.MegaZone);
            parameters.Add("@EmailID", user.Email);
            parameters.Add("@Mobile", user.Mobile);
            parameters.Add("@UpdatedBy", user.UpdatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            connection.Execute(
                "sp_updateUser",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public int DisableUser(DisableUserDTO disableUserDTO)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@UserID", disableUserDTO.UserId);
            parameters.Add("@UpdatedBy", disableUserDTO.UpdatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            connection.Execute(
                "sp_disableUser",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public int ActivateUser(ActivateUserDTO activateUserDTO)
        {
            using var connection = new SqlConnection(_conStr);

            var parameters = new DynamicParameters();

            parameters.Add("@UserID", activateUserDTO.UserId);
            parameters.Add("@UpdatedBy", activateUserDTO.UpdatedBy);

            parameters.Add("@Output", dbType: DbType.Int32, direction: ParameterDirection.Output);

            connection.Execute(
                "sp_activateUser",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("@Output");
        }

        public List<ModuleDto> GetModulesByUser(string userId)
        {
            using var connection = new SqlConnection(_conStr);

            var modules = connection.Query<ModuleDto>(
                "sp_GetModulesByUser",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure
            ).ToList();

            return modules;
        }

        public List<string> GetRoles()
        {
            using var connection = new SqlConnection(_conStr);

            var roles = connection.Query<string>(
                "sp_getRoles",
                commandType: CommandType.StoredProcedure
            ).ToList();

            return roles;
        }
    }
}