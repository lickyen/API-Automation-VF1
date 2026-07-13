using Dapper;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class SARFAESIRepository
    {
        private readonly string _conStr;

        public SARFAESIRepository(IConfiguration config)
        {
            _conStr = config.GetConnectionString("DBConn");
        }

        public async Task<SARFAESIRecord?> GetByIdAsync(int id)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.QueryFirstOrDefaultAsync<SARFAESIRecord>(
                "sp_SARFAESI_GetById",
                new { Id = id },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<SARFAESIRecord>> GetByUserAsync(string userId, string? status, string? role)
        {
            using var conn = new SqlConnection(_conStr);

            return (await conn.QueryAsync<SARFAESIRecord>(
                "sp_SARFAESI_GetByUser",
                new
                {
                    UserId = userId,
                    Status = status,
                    Role = role
                },
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task<int> CreateAsync(SARFAESICreateDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new
            {
                dto.Lan,
                dto.CustomerName,
                dto.PropertyAddress,
                dto.ResidentialAddress,
                dto.Mobile,
                dto.AlternateMobile,
                dto.Pos,
                dto.Tos,
                dto.Dpd,
                dto.BranchCode,
                dto.BranchName,
                dto.Region,
                dto.Zone,
                dto.MegaZone,
                dto.IsPossessionTaken,
                dto.PossessionDate,
                dto.Is134HandService,
                dto.CustomerProfile,
                dto.PropertyOccupiedBy,
                dto.PropertyType,
                dto.PropertyCondition,
                dto.EstimatedPropertyValue,
                dto.CustomerIntentCapability,
                dto.NonPossessionReason,
                dto.ResponseStatus,
                dto.CreatedBy
            };

            return await conn.ExecuteScalarAsync<int>(
                "sp_SARFAESI_Create",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> UpdateAsync(int id, SARFAESICreateDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new
            {
                Id = id,
                dto.Lan,
                dto.CustomerName,
                dto.PropertyAddress,
                dto.ResidentialAddress,
                dto.Mobile,
                dto.AlternateMobile,
                dto.Pos,
                dto.Tos,
                dto.Dpd,
                dto.BranchCode,
                dto.BranchName,
                dto.Region,
                dto.Zone,
                dto.MegaZone,
                dto.IsPossessionTaken,
                dto.PossessionDate,
                dto.Is134HandService,
                dto.CustomerProfile,
                dto.PropertyOccupiedBy,
                dto.PropertyType,
                dto.PropertyCondition,
                dto.EstimatedPropertyValue,
                dto.CustomerIntentCapability,
                dto.NonPossessionReason,
                dto.ResponseStatus,
                UpdatedBy = dto.CreatedBy
            };

            return await conn.ExecuteScalarAsync<int>(
                "sp_SARFAESI_Update",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> AddFileAsync(int reqId, string fileType, string fileName, string uploadedBy)
        {
            using var conn = new SqlConnection(_conStr);

            return await conn.ExecuteScalarAsync<int>(
                "sp_SARFAESI_AddFile",
                new { ReqId = reqId, FileType = fileType, FileName = fileName, UploadedBy = uploadedBy },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<SARFAESIFile>> GetFilesByReqIdAsync(int reqId)
        {
            using var conn = new SqlConnection(_conStr);

            return (await conn.QueryAsync<SARFAESIFile>(
                "sp_SARFAESI_GetFiles",
                new { ReqId = reqId },
                commandType: CommandType.StoredProcedure
            )).ToList();
        }

        public async Task MarkCompletedAsync(int reqId, string updatedBy)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.ExecuteAsync(
                "sp_SARFAESI_MarkCompleted",
                new { ReqId = reqId, UpdatedBy = updatedBy },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<dynamic>> GetReportAsync(SARFAESIReportFilterDto dto)
        {
            using var conn = new SqlConnection(_conStr);

            var parameters = new
            {
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                Region = dto.Region,
                Zone = dto.Zone,
                MegaZone = dto.MegaZone
            };

            return await conn.QueryAsync(
                "sp_SARFAESI_Report",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> BulkInsertLanMappingsAsync(
    List<SARFAESILanMappingDto> mappings,
    string createdBy)
        {
            using var conn = new SqlConnection(_conStr);

            var table = new DataTable();

            table.Columns.Add("LAN", typeof(string));
            table.Columns.Add("EmpID", typeof(string));

            foreach (var item in mappings)
            {
                table.Rows.Add(item.Lan, item.EmpId);
            }

            var parameters = new DynamicParameters();

            parameters.Add(
                "@Mappings",
                table.AsTableValuedParameter("TVP_SARFAESI_LAN_Master"));

            parameters.Add("@CreatedBy", createdBy);

            parameters.Add(
                "@RowsAffected",
                dbType: DbType.Int32,
                direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "sp_SARFAESI_BulkUploadLAN",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@RowsAffected");
        }

        public async Task<List<string>> GetLansByEmpIdAsync(string empId)
        {
            using var conn = new SqlConnection(_conStr);

            return (await conn.QueryAsync<string>(
                @"SELECT LAN
          FROM SARFAESI_LAN_Master
          WHERE EmpID=@EmpID
          ORDER BY LAN",
                new { EmpID = empId }))
                .ToList();
        }
    }
}