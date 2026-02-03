using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class BranchRepository
    {
        private readonly string _conStr;

        public BranchRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public List<BranchModel> GetBranches()
        {
            var branches = new List<BranchModel>();

            try
            {
                using (var connection = new SqlConnection(_conStr))
                using (var command = new SqlCommand("GetBranchMaster", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var branch = new BranchModel
                            {
                               
                                BranchId = reader["BranchId"]?.ToString(),
                                BranchName = reader["BranchName"]?.ToString(),
                                Region = reader["Cluster"]?.ToString(),
                                Zone = reader["State"]?.ToString(),
                                MegaZone = reader["Region"]?.ToString(),
                               
                            };

                            branches.Add(branch);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }

            return branches;
        }


    }
}
