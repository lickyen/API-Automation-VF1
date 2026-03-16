using Dapper;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using System.Data;

namespace ProductivityApi.Repository
{
    public class LegalNoticeRepository
    {
        private readonly string _conStr;

        public LegalNoticeRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }
        public async Task<List<LegalNoticeTypes>> GetLegalNoticeTypesAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var rows = await connection.QueryAsync<LegalNoticeTypesFlat>(
                "sp_GetActiveLegalNoticeTypes",
                commandType: CommandType.StoredProcedure
            );

            var result = rows
                .GroupBy(x => x.NoticeType)
                .Select(g => new LegalNoticeTypes
                {
                    NoticeType = g.Key,
                    SubTypes = g.Select(x => x.SubType).ToList()
                })
                .ToList();

            return result;
        }
        public async Task<List<LegalNoticeModel>> GetLegalNoticeMasterAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var data = await connection.QueryAsync<LegalNoticeModel>(
                "sp_GetAllLegalNoticeMaster",  
                commandType: CommandType.StoredProcedure
            );

            return data.ToList();
        }
        public async Task<List<LegalNoticeUploads>> GetLegalNoticeUploadsAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var uploads = await connection.QueryAsync<LegalNoticeUploads>(
                "sp_GetAllLegalNoticeUploads",
                commandType: CommandType.StoredProcedure
            );

            return uploads.ToList();
        }
        public async Task<List<LegalNoticeUploads>> GetLegalNoticeByLanAsync(string lan)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var data = await connection.QueryAsync<LegalNoticeUploads>(
                "sp_GetNoticesByLan",
                new { Lan = lan },
                commandType: CommandType.StoredProcedure
            );

            return data.ToList();
        }
        public async Task<string> UploadLegalNoticeDocumentAsync(LegalNoticeUploadDto dto, string fileName)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Lan", dto.Lan);
            parameters.Add("@CustomerName", dto.CustomerName);
            parameters.Add("@BranchCode", dto.BranchCode);
            parameters.Add("@BranchName", dto.BranchName);
            parameters.Add("@Region", dto.Region);
            parameters.Add("@Zone", dto.Zone);
            parameters.Add("@MegaZone", dto.MegaZone);
            parameters.Add("@POS", dto.POS);
            parameters.Add("@NoticeType", dto.NoticeType);
            parameters.Add("@SubType", dto.SubType);
            parameters.Add("@FilePath", fileName);
            parameters.Add("@UploadedBy", dto.UploadedBy);
            parameters.Add("@Source", dto.Source);
            parameters.Add("@StatusMessage", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "sp_UploadLegalNoticeDocument",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var statusMessage = parameters.Get<string>("@StatusMessage");
            return statusMessage;
        }
    }
}




