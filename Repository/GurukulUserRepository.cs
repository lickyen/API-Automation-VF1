using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class GurukulUserRepository
    {
        private readonly string _conStr;

        public GurukulUserRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("VLMDBConn");
        }

        public List<GurukulUserModel> GetAllUsers(string? status)
        {
            var users = new List<GurukulUserModel>();

            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_getUsersList", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue(
                    "@Status",
                    string.IsNullOrWhiteSpace(status)
                        ? DBNull.Value
                        : status
                );

                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(MapUser(reader));
                    }
                }
            }

            return users;
        }
        public GurukulUserModel? GetUser(string userId)
        {
            GurukulUserModel? user = null;

            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_getUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserID", userId);

                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = MapUser(reader);
                    }
                }
            }

            return user;
        }

        public int AddUser(AddGurukulUserDTO user)
        {
            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_insertUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@UserID", user.UserId);
                command.Parameters.AddWithValue("@UserName", user.UserName);
                command.Parameters.AddWithValue("@Role", user.Role);
                command.Parameters.AddWithValue("@BranchCode", (object?)user.BranchCode ?? DBNull.Value);
                command.Parameters.AddWithValue("@BranchName", (object?)user.BranchName ?? DBNull.Value);
                command.Parameters.AddWithValue("@ClusterName", (object?)user.ClusterName ?? DBNull.Value);
                command.Parameters.AddWithValue("@State", (object?)user.State ?? DBNull.Value);
                command.Parameters.AddWithValue("@Department", (object?)user.Department ?? DBNull.Value);
                command.Parameters.AddWithValue("@CreatedBy", user.CreatedBy);

                var outputParam = new SqlParameter("@Output", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(outputParam);

                connection.Open();
                command.ExecuteNonQuery();

                return outputParam.Value != DBNull.Value
                    ? Convert.ToInt32(outputParam.Value)
                    : 0;
            }
        }

        public int UpdateUser(UpdateGurukulUserDTO user)
        {
            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_updateUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@UserID", user.UserId);
                command.Parameters.AddWithValue("@UserName", (object?)user.UserName ?? DBNull.Value);
                command.Parameters.AddWithValue("@Role", (object?)user.Role ?? DBNull.Value);
                command.Parameters.AddWithValue("@BranchCode", (object?)user.BranchCode ?? DBNull.Value);
                command.Parameters.AddWithValue("@BranchName", (object?)user.BranchName ?? DBNull.Value);
                command.Parameters.AddWithValue("@ClusterName", (object?)user.ClusterName ?? DBNull.Value);
                command.Parameters.AddWithValue("@State", (object?)user.State ?? DBNull.Value);
                command.Parameters.AddWithValue("@Department", (object?)user.Department ?? DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedBy", user.UpdatedBy);

                var outputParam = new SqlParameter("@Output", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(outputParam);

                connection.Open();
                command.ExecuteNonQuery();

                return outputParam.Value != DBNull.Value
                    ? Convert.ToInt32(outputParam.Value)
                    : 0;
            }
        }

        public int DisableUser(DisableUserDTO user)
        {
            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_disableUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@UserID", user.UserId);
                command.Parameters.AddWithValue("@UpdatedBy", user.UpdatedBy);

                var outputParam = new SqlParameter("@Output", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(outputParam);

                connection.Open();
                command.ExecuteNonQuery();

                return outputParam.Value != DBNull.Value
                    ? Convert.ToInt32(outputParam.Value)
                    : 0;
            }
        }

        public int ActivateUser(ActivateUserDTO user)
        {
            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_activateUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@UserID", user.UserId);
                command.Parameters.AddWithValue("@UpdatedBy", user.UpdatedBy);

                var outputParam = new SqlParameter("@Output", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.Add(outputParam);

                connection.Open();
                command.ExecuteNonQuery();

                return outputParam.Value != DBNull.Value
                    ? Convert.ToInt32(outputParam.Value)
                    : 0;
            }
        }

        private static GurukulUserModel MapUser(SqlDataReader reader)
        {
            return new GurukulUserModel
            {
                Id = Convert.ToInt32(reader["Id"]),
                UserId = reader["UserId"]?.ToString(),
                UserName = reader["UserName"]?.ToString(),
                Password = reader["Password"]?.ToString(),
                Role = reader["Role"]?.ToString(),
                Designation = reader["Designation"]?.ToString(),
                Status = reader["Status"]?.ToString(),
                BranchCode = reader["Branch_Code"]?.ToString(),
                BranchName = reader["Branch_Name"]?.ToString(),
                ClusterName = reader["Cluster_Name"]?.ToString(),
                State = reader["State"]?.ToString(),
                PreferedLangID = reader["PreferedLangID"] != DBNull.Value
                    ? Convert.ToInt32(reader["PreferedLangID"])
                    : null,
                LoginDateTime = reader["LoginDateTime"] != DBNull.Value
                    ? Convert.ToDateTime(reader["LoginDateTime"])
                    : null,
                CreatedBy = reader["CreatedBy"]?.ToString(),
                CreatedDateTime = reader["CreatedDateTime"] != DBNull.Value
                    ? Convert.ToDateTime(reader["CreatedDateTime"])
                    : null,
                UpdatedBy = reader["UpdatedBy"]?.ToString(),
                UpdatedDateTime = reader["UpdatedDateTime"] != DBNull.Value
                    ? Convert.ToDateTime(reader["UpdatedDateTime"])
                    : null,
                Email = reader["Email"]?.ToString(),
                Department = reader["Department"]?.ToString()
            };
        }
    }
}