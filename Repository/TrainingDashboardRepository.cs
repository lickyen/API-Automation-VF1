using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class TrainingDashboardRepository
    {
        private readonly string _conStr;

        public TrainingDashboardRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("VLMDBConn");
        }

        // =========================================
        // GET TRAININGS
        // =========================================

        public List<TrainingMasterModel> GetTrainingNames()
        {
            using IDbConnection db = new SqlConnection(_conStr);

            return db.Query<TrainingMasterModel>(
                "SP_GetEmployeeTrainings",
                commandType: CommandType.StoredProcedure
            ).ToList();
        }

        // =========================================
        // GET LOCATIONS
        // =========================================

        public List<TrainingLocationModel> GetLocations()
        {
            using IDbConnection db = new SqlConnection(_conStr);

            return db.Query<TrainingLocationModel>(
                "SP_GetTrainingCenters",
                commandType: CommandType.StoredProcedure
            ).ToList();
        }

        // =========================================
        // GET EMPLOYEE LIST
        // =========================================

        public List<TrainingEmployeeModel> GetEmployeeList(string userId)
        {
            using IDbConnection db = new SqlConnection(_conStr);

            DynamicParameters param = new();
            param.Add("@UserID", userId);

            return db.Query<TrainingEmployeeModel>(
                "SP_GetTrainingEmployeeList",
                param,
                commandType: CommandType.StoredProcedure
            ).ToList();
        }

        // =========================================
        // ASSIGN TRAINING
        // =========================================

        public int AssignTraining(AssignTrainingRequestModel model)
        {
            using IDbConnection db = new SqlConnection(_conStr);

            DynamicParameters param = new();

            param.Add("@EmpID", model.EmpID);
            param.Add("@TrainingID", model.TrainingID);
            param.Add("@Location", model.Location);
            param.Add("@Start_Date", model.Start_Date);
            param.Add("@Expiry_Date", model.Expiry_Date);
            param.Add("@CreatedBy", model.CreatedBy);

            param.Add("@Output", dbType: DbType.Int32,
                direction: ParameterDirection.Output);

            db.Execute(
                "SP_AssignTrainingEmployee",
                param,
                commandType: CommandType.StoredProcedure
            );

            return param.Get<int>("@Output");
        }

        // =========================================
        // DISABLE EMPLOYEE
        // =========================================

        public bool DisableEmployee(DisableEmployeeDto dto)
        {
            using IDbConnection db = new SqlConnection(_conStr);

            DynamicParameters param = new();
            param.Add("@Id", dto.Id);
            param.Add("@UpdatedBy", dto.UpdatedBy);

            int rows = db.Execute(
                "SP_DisableTrainingEmployee",
                param,
                commandType: CommandType.StoredProcedure
            );

            return rows > 0;
        }

        // =========================================
        // DOWNLOAD REPORT
        // =========================================

        public async Task<IEnumerable<dynamic>> DownloadReportAsync()
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryAsync(
                "SP_GetTrainingReport",
                commandType: CommandType.StoredProcedure
            );
        }
    }
}