using Dapper;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repositories
{
    public class VOTRepository 
    {
        private readonly IDbConnection _connection;

        public VOTRepository(IDbConnection connection)
        {
            _connection = connection;
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

            await _connection.ExecuteAsync(
                "sp_VOT_BulkInsertLan",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
        public async Task<IEnumerable<VOTModel.MappedLan>> GetLansByUserAsync(
            VOTModel.GetLansByUserRequest request)
        {
            return await _connection.QueryAsync<VOTModel.MappedLan>(
                "sp_VOT_GetLansByUser",
                new
                {
                    request.EmpId
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertCaseAsync(VOTModel.CreateCaseRequest request)
        {
            return await _connection.ExecuteScalarAsync<int>(
                "sp_VOT_InsertCase",
                request,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<VOTModel.CaseDetails?> GetCaseByIdAsync(int id)
        {
            using var multi = await _connection.QueryMultipleAsync(
                "sp_VOT_GetCaseById",
                new { Id = id },
                commandType: CommandType.StoredProcedure);

            var caseDetails = await multi.ReadFirstOrDefaultAsync<VOTModel.Case>();

            var logs = (await multi.ReadAsync<VOTModel.Log>())
                .ToList();

            return new VOTModel.CaseDetails
            {
                Case = caseDetails,
                Logs = logs
            };
        }
        public async Task<IEnumerable<VOTModel.Case>> GetCasesByUserAsync(
    VOTModel.GetCasesByUserRequest request)
        {
            return await _connection.QueryAsync<VOTModel.Case>(
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
            await _connection.ExecuteAsync(
                "sp_VOT_ZdshReview",
                request,
                commandType: CommandType.StoredProcedure);
        }

        public async Task MzdshReviewAsync(VOTModel.MzdshReviewRequest request)
        {
            await _connection.ExecuteAsync(
                "sp_VOT_MzdshReview",
                request,
                commandType: CommandType.StoredProcedure);
        }

        public async Task AuditReviewAsync(VOTModel.AuditReviewRequest request)
        {
            await _connection.ExecuteAsync(
                "sp_VOT_AuditReview",
                request,
                commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateCaseFileAsync(int caseId, string fileType, string fileName)
        {
            await _connection.ExecuteAsync(
                "sp_VOT_UpdateCaseFile",
                new
                {
                    CaseId = caseId,
                    FileType = fileType,
                    FileName = fileName
                },
                commandType: CommandType.StoredProcedure);
        }
    }
}