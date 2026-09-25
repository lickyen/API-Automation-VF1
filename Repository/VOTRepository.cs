using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ProductivityApi.Repositories
{
    public class VOTRepository 
    {
        private readonly string _conStr;

        public VOTRepository(IConfiguration config)
        {
            _conStr = config.GetConnectionString("DBConn");
        }
        private IDbConnection GetConnection()
        {
            return new SqlConnection(_conStr);
        }

        public async Task BulkInsertLanAsync(
            VOTModel.BulkUploadLanRequest request)
        {
            var dt = new DataTable();

            dt.Columns.Add("Lan");
            dt.Columns.Add("CustName");
            dt.Columns.Add("EmpId");

            foreach (var item in request.Lans)
            {
                dt.Rows.Add(
                    item.Lan,
                    item.CustName,
                    item.EmpId);
            }

            var parameters = new DynamicParameters();

            parameters.Add(
                "@LanData",
                dt.AsTableValuedParameter("VOT_Lan_Type"));

            parameters.Add(
                "@CreatedBy",
                request.CreatedBy);

            using var connection = GetConnection();

            await connection.ExecuteAsync(
                "sp_VOT_BulkInsertLan",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<VOTModel.MappedLan>> GetLansAsync()
        {
            using var connection = GetConnection();

            return await connection.QueryAsync<VOTModel.MappedLan>(
                "sp_VOT_GetLans",
                commandType: CommandType.StoredProcedure);
        }


        public async Task<IEnumerable<VOTModel.MappedLan>> GetLansByUserAsync(
            VOTModel.GetLansByUserRequest request)
        {
            using var connection = GetConnection();

            return await connection.QueryAsync<VOTModel.MappedLan>(
                "sp_VOT_GetLansByUser",
                new
                {
                    request.EmpId
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateLanAsync(
    VOTModel.UpdateLanRequest request)
        {
            using var connection = GetConnection();

            await connection.ExecuteAsync(
                "sp_VOT_UpdateLan",
                request,
                commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateLanStatusAsync(
            VOTModel.UpdateLanStatusRequest request)
        {
            using var connection = GetConnection();

            await connection.ExecuteAsync(
                "sp_VOT_UpdateLanStatus",
                request,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertCaseAsync(VOTModel.CreateCaseRequest request)
        {
            using var connection = GetConnection();

            return await connection.ExecuteScalarAsync<int>(
                "sp_VOT_InsertCase",
                request,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<VOTModel.CaseDetails?> GetCaseByIdAsync(int id)
        {
            using var connection = GetConnection();

            using var multi = await connection.QueryMultipleAsync(
                "sp_VOT_GetCaseById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);

            var caseDetails = await multi.ReadFirstOrDefaultAsync<VOTModel.Case>();

            var logs = (await multi.ReadAsync<VOTModel.Log>())
                .ToList();

            var uploads =(await multi.ReadAsync<VOTModel.Upload>())
                .ToList();


            return new VOTModel.CaseDetails
            {
                Case = caseDetails,
                Logs = logs,
                Uploads = uploads
            };
        }
        public async Task<IEnumerable<VOTModel.Case>> GetCasesByUserAsync(
    VOTModel.GetCasesByUserRequest request)
        {
            using var connection = GetConnection();

            return await connection.QueryAsync<VOTModel.Case>(
                "sp_VOT_GetCasesByUser",
                new
                {
                    request.EmpId,
                    request.Role,
                    request.Status
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task ZdshReviewAsync(VOTModel.ZdshReviewRequest request)
        {
            using var connection = GetConnection();

            await connection.ExecuteAsync(
                "sp_VOT_ZdshReview",
                request,
                commandType: CommandType.StoredProcedure);
        }

        public async Task MzdshReviewAsync(VOTModel.MzdshReviewRequest request)
        {
            using var connection = GetConnection();

            await connection.ExecuteAsync(
                "sp_VOT_MzdshReview",
                request,
                commandType: CommandType.StoredProcedure);
        }

        public async Task AuditReviewAsync(VOTModel.AuditReviewRequest request)
        {
            using var connection = GetConnection();

            await connection.ExecuteAsync(
                "sp_VOT_AuditReview",
                request,
                commandType: CommandType.StoredProcedure);
        }

        //public async Task UpdateCaseFileAsync(int caseId, string fileType, string fileName)
        //{
        //    using var connection = GetConnection();

        //    await connection.ExecuteAsync(
        //        "sp_VOT_UpdateCaseFile",
        //        new
        //        {
        //            CaseId = caseId,
        //            FileType = fileType,
        //            FileName = fileName
        //        },
        //        commandType: CommandType.StoredProcedure);
        //}

        public async Task UpdateCaseFileAsync(int caseId, string fileType, string fileName)
        {
            using var connection = GetConnection();

            await connection.ExecuteAsync(
                "sp_VOT_SaveUpload",
                new
                {
                    CaseId = caseId,
                    FileName = fileType,
                    FilePath = @fileName
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<VOTModel.ReportData>> GetReportAsync(
            VOTModel.ReportRequest request)
        {
            using var connection = GetConnection();

            return await connection.QueryAsync<VOTModel.ReportData>(
                "sp_VOT_GetReport",
                request,
                commandType: CommandType.StoredProcedure);
        }
    }

}