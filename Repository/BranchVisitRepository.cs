using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using ProductivityApi.Models;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ProductivityApi.Repository
{
    public class BranchVisitRepository
    {
        private readonly string _conStr;

        public BranchVisitRepository(IConfiguration configuration)
        {
            _conStr = configuration.GetConnectionString("DBConn");
        }

        public async Task<List<BranchVisit>> GetVisitsByUserAsync(string userId, string month, string status = null)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var visits = await connection.QueryAsync<BranchVisit>(
                "sp_BranchVisit_GetVisitsByUser",
                new
                {
                    UserId = userId,
                    Month = month,
                    Status = status
                },
                commandType: CommandType.StoredProcedure
            );

            return visits.ToList();
        }

        public async Task<BranchVisitDto> GetVisitByIdAsync(int visitId)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            using var multi = await connection.QueryMultipleAsync(
                "sp_BranchVisit_GetById",
                new { Id = visitId },
                commandType: CommandType.StoredProcedure
            );

            var visit = await multi.ReadFirstOrDefaultAsync<BranchVisit>();
            if (visit == null)
                return null;

            var ChecklistResponses = (await multi.ReadAsync<BranchVisitChecklistResponseDto>())
                                        .ToList();

            return new BranchVisitDto
            {
                VisitDetails = visit,
                ChecklistResponse = ChecklistResponses
            };
        }

        public async Task<List<BranchVisitChecklist>> GetChecklistAsync(string section)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var Checklist = await connection.QueryAsync<BranchVisitChecklist>(
                "sp_BranchVisit_GetChecklist",
                new { Section = section },
                commandType: CommandType.StoredProcedure
            );

            return Checklist.ToList();
        }

        public async Task<int> UpdateBranchVisitAsync(BranchVisitSubmitDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            var visitParams = new
            {
                Id = dto.Id,
                Remarks = dto.Remarks,
                GeoTag = dto.GeoTag,
                Location = dto.Location,
                PhotoPath = dto.PhotoPath,
                FinalObservation = dto.FinalObservation,
                UpdatedBy = dto.UpdatedBy
            };

            var affectedRows = await connection.ExecuteScalarAsync<int>(
                "sp_BranchVisit_UpdateVisit",
                visitParams,
                transaction,
                commandType: CommandType.StoredProcedure);

            // ✅ Handle "record not found"
            if (affectedRows == -1)
            {
                transaction.Rollback();
                return -1;
            }

            // (Optional) Handle unexpected case
            if (affectedRows == 0)
            {
                transaction.Rollback();
                return 0;
            }

            // ✅ Insert checklist only if update succeeded
            if (dto.ChecklistResponse?.Any() == true)
            {
                foreach (var item in dto.ChecklistResponse)
                {
                    var checklistParams = new
                    {
                        VisitId = dto.Id,
                        ChecklistId = item.ChecklistId,
                        Response = item.Response,
                        Remarks = item.ChecklistRemarks,
                        CreatedBy = dto.UpdatedBy
                    };

                    await connection.ExecuteAsync(
                        "sp_BranchVisit_InsertChecklist",
                        checklistParams,
                        transaction,
                        commandType: CommandType.StoredProcedure);
                }
            }

            transaction.Commit();
            return 1;

        }
        public async Task<IEnumerable<dynamic>> GetReportDataAsync(string month)
        {
            using var conn = new SqlConnection(_conStr);
            await conn.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@Month", month);

            return await conn.QueryAsync(
                "sp_BranchVisit_GetReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        //Job methods

        public async Task<List<BranchVisit>> GetPendingVisitsForMonthAsync(string month)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var visits = await connection.QueryAsync<BranchVisit>(
                "sp_BranchVisit_GetPendingForMonth",
                new { Month = month },
                commandType: CommandType.StoredProcedure);

            return visits.ToList();
        }

        public async Task<int> UpdateEscalationStatusAsync(int visitId, string escalationType)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.ExecuteAsync(
                "sp_BranchVisit_UpdateEscalationStatus",
                new { Id = visitId, EscalationType = escalationType },
                commandType: CommandType.StoredProcedure);

            return result;
        }

        public async Task<int> InsertMonthlyVisitsAsync()
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.ExecuteAsync(
                "sp_BranchVisit_MonthlyInsert",
                commandType: CommandType.StoredProcedure);

            return result;
        }

        public async Task<List<UserModel>> GetEscalationUsersAsync(int visitId, string escLevel)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var users = await connection.QueryAsync<UserModel>(
                "sp_BranchVisit_GetEscalationUser",
                new { VisitId = visitId, EscLevel = escLevel },
                commandType: CommandType.StoredProcedure
            );

            return users.ToList();
        }

    }

}

