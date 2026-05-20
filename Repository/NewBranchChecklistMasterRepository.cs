using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class NewBranchChecklistMasterRepository
    {
        private readonly string _conStr;

        public NewBranchChecklistMasterRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        #region GET CHECKLIST MASTER

        public List<ChecklistMasterDto> GetChecklistMaster()
        {
            try
            {
                using IDbConnection connection = new SqlConnection(_conStr);

                var data = connection.Query(
                    "SP_GetChecklistMaster",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                List<ChecklistMasterDto> checklists = data.Select(x =>
                    new ChecklistMasterDto
                    {
                        Id = (int)x.Id,
                        Team = (string)x.Team,
                        ChecklistName = (string)x.ChecklistName,

                        Status = ((string)x.Status)
                                    .Split('/')
                                    .Select(s => s.Trim())
                                    .ToList()
                    }
                ).ToList();

                return checklists;
            }
            catch (Exception ex)
            {
                throw new Exception("DB Error.", ex);
            }
        }

        #endregion
    }
}