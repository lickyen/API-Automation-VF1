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
            var users = new List<UserModel>();

            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_getUsersList", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var user = new UserModel
                        {
                            UserId = reader["EmpId"]?.ToString(),
                            Name = reader["Name"]?.ToString(),
                            Password = reader["Password"]?.ToString(),
                            Role = reader["Role"]?.ToString(),
                            BranchId = reader["BranchID"]?.ToString(),
                            BranchName = reader["BranchName"]?.ToString(),
                            Region = reader["ClusterName"]?.ToString(),
                            Zone = reader["Zone"]?.ToString(),
                            MegaZone = reader["Megazone"]?.ToString(),
                            Status = reader["Status"]?.ToString(),
                            Email = reader["EmailID"] != DBNull.Value ? reader["EmailID"].ToString() : null,
                            Mobile = reader["MobileNo"] != DBNull.Value ? reader["MobileNo"].ToString() : null,
                            CreatedBy = reader["CreatedBy"] != DBNull.Value ? reader["CreatedBy"].ToString() : null,
                            CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : (DateTime?)null,
                            UpdatedBy = reader["UpdatedBy"] != DBNull.Value ? reader["UpdatedBy"].ToString() : null,
                            UpdatedDate = reader["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedDate"]) : (DateTime?)null,
                        };

                        users.Add(user);
                    }
                }
            }

            return users;
        }

        public UserModel? GetUser(string userId)
        {
            UserModel? user = null;

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
                        user = new UserModel
                        {
                            UserId = reader["EmpId"]?.ToString(),
                            Name = reader["Name"]?.ToString(),
                            Password = reader["Password"]?.ToString(),
                            Role = reader["Role"]?.ToString(),
                            BranchId = reader["BranchID"]?.ToString(),
                            BranchName = reader["BranchName"]?.ToString(),
                            Region = reader["ClusterName"]?.ToString(),
                            Zone = reader["Zone"]?.ToString(),
                            MegaZone = reader["Megazone"]?.ToString(),
                            Status = reader["Status"]?.ToString(),
                            Email = reader["EmailID"] != DBNull.Value ? reader["EmailID"].ToString() : null,
                            Mobile = reader["MobileNo"] != DBNull.Value ? reader["MobileNo"].ToString() : null,
                            CreatedBy = reader["CreatedBy"] != DBNull.Value ? reader["CreatedBy"].ToString() : null,
                            CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : (DateTime?)null,
                            UpdatedBy = reader["UpdatedBy"] != DBNull.Value ? reader["UpdatedBy"].ToString() : null,
                            UpdatedDate = reader["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedDate"]) : (DateTime?)null
                        };
                    }
                }
            }
            return user;
        }

        public int AddUser(AddUserDTO addUserDTO)
        {
            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_insertUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@UserID", addUserDTO.UserId);
                command.Parameters.AddWithValue("@Name", addUserDTO.Name);
                command.Parameters.AddWithValue("@BranchID", addUserDTO.BranchId);
                command.Parameters.AddWithValue("@BranchName", addUserDTO.BranchName);
                command.Parameters.AddWithValue("@Region", addUserDTO.Region);
                command.Parameters.AddWithValue("@Zone", addUserDTO.Zone);
                command.Parameters.AddWithValue("@Megazone", addUserDTO.MegaZone);
                command.Parameters.AddWithValue("@Role", addUserDTO.Role);
                command.Parameters.AddWithValue("@EmailID", addUserDTO.Email);
                command.Parameters.AddWithValue("@Mobile", addUserDTO.Mobile);
                command.Parameters.AddWithValue("@CreatedBy", addUserDTO.CreatedBy);

                var outputParam = new SqlParameter("@Output", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(outputParam);

                connection.Open();
                command.ExecuteNonQuery();

                int result = outputParam.Value != null && outputParam.Value != DBNull.Value
                    ? Convert.ToInt32(outputParam.Value)
                    : 0;
                return result;
            }
        }
        public int UpdateUser(UpdateUserDTO user)
        {
            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_updateUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@UserID", user.UserId);
                command.Parameters.AddWithValue("@Name", user.Name ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Role", user.Role ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@BranchID", user.BranchId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@BranchName", user.BranchName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Region", user.Region ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Zone", user.Zone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Megazone", user.MegaZone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@EmailID", user.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Mobile", user.Mobile ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedBy", user.UpdatedBy ?? (object)DBNull.Value);

                var outputParam = new SqlParameter("@Output", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(outputParam);

                connection.Open();
                command.ExecuteNonQuery();

                int result = outputParam.Value != null && outputParam.Value != DBNull.Value
                    ? Convert.ToInt32(outputParam.Value)
                    : 0;
                return result;
            }
        }
        public int DisableUser(DisableUserDTO disableUserDTO)
        {
            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_disableUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@UserID", disableUserDTO.UserId);
                command.Parameters.AddWithValue("@UpdatedBy", disableUserDTO.UpdatedBy);

                var outputParam = new SqlParameter("@Output", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(outputParam);

                connection.Open();
                command.ExecuteNonQuery();

                int result = outputParam.Value != null && outputParam.Value != DBNull.Value
                    ? Convert.ToInt32(outputParam.Value)
                    : 0; 

                return result;
            }
        }
        public int ActivateUser(ActivateUserDTO activateUserDTO)
        {
            using (var connection = new SqlConnection(_conStr))
            using (var command = new SqlCommand("sp_activateUser", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@UserID", activateUserDTO.UserId);
                command.Parameters.AddWithValue("@UpdatedBy", activateUserDTO.UpdatedBy);

                var outputParam = new SqlParameter("@Output", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(outputParam);

                connection.Open();
                command.ExecuteNonQuery();

                int result = outputParam.Value != null && outputParam.Value != DBNull.Value
                    ? Convert.ToInt32(outputParam.Value)
                    : 0;

                return result;
            }
        }
        public List<string> GetModulesByUser(string UserId)
        {
            List<string> modules = new List<string>();

            using (var connection = new SqlConnection(_conStr))
            {
                using (var command = new SqlCommand("sp_GetModulesByUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", UserId);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string? module = reader.IsDBNull(reader.GetOrdinal("Module")) ? null : reader.GetString(reader.GetOrdinal("Module"));
                                if (module != null)
                                {
                                    modules.Add(module);
                                }

                            }
                        }

                    }
                }
            }

            return modules;
        }
        public List<string> GetRoles()
        {
            List<string> roles = new List<string>();

            using (var connection = new SqlConnection(_conStr))
            {
                using (var command = new SqlCommand("sp_getRoles", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string? role = reader.IsDBNull(reader.GetOrdinal("Role")) ? null : reader.GetString(reader.GetOrdinal("Role"));
                                if (role != null)
                                {
                                    roles.Add(role);
                                }

                            }
                        }

                    }
                }
            }

            return roles;
        }

    }

}


