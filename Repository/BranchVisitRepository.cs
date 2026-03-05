using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using ProductivityApi.Models;

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

            var checklistResponses = (await multi.ReadAsync<BranchVisitCheckListResponseDto>())
                                        .ToList();

            return new BranchVisitDto
            {
                VisitDetails = visit,
                ChecklistResponse = checklistResponses
            };
        }

        public async Task<List<BranchVisitChecklist>> GetChecklistAsync(string section)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var checklist = await connection.QueryAsync<BranchVisitChecklist>(
                "sp_BranchVisit_GetChecklist",
                new { Section = section },
                commandType: CommandType.StoredProcedure
            );

            return checklist.ToList();
        }

        public async Task<int> UpdateBranchVisitAsync(BranchVisitSubmitDto dto)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Update BranchVisit basic info
                var visitParams = new
                {
                    Id = dto.Id,
                    Remarks = dto.Remarks,
                    GeoTag = dto.GeoTag,
                    PhotoPath = dto.PhotoPath,
                    RevisitRequired = dto.RevisitRequired,
                    UpdatedBy = dto.UpdatedBy
                };

                await connection.ExecuteAsync(
                    "sp_BranchVisit_UpdateVisit",
                    visitParams,
                    transaction,
                    commandType: CommandType.StoredProcedure);

                // Save Checklist Responses
                if (dto.CheckListReponse != null && dto.CheckListReponse.Any())
                {
                    foreach (var item in dto.CheckListReponse)
                    {
                        var checklistParams = new
                        {
                            VisitId = dto.Id,
                            ChecklistId = item.ChecklistId,
                            Response = item.Response,
                            CreatedBy = dto.UpdatedBy
                        };

                        await connection.ExecuteAsync(
                            "sp_BranchVisit_InsertCheckList",
                            checklistParams,
                            transaction,
                            commandType: CommandType.StoredProcedure);
                    }
                }

                transaction.Commit();
                return 1;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Inserts / seeds branch visit rows for the specified month.
        public async Task<int> InsertMonthlyVisitsAsync(string month, string createdBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.ExecuteAsync(
                "sp_BranchVisit_InsertMonthlyVisits",
                new { Month = month, CreatedBy = createdBy },
                commandType: CommandType.StoredProcedure);

            return result;
        }

        // Fetch visits for the given month whose status is not 'CMP' (pending/escalation candidates)
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

        // Update escalation status (e.g. 10 = EL1, 15 = EL2)
        public async Task<int> UpdateEscalationStatusAsync(int visitId, int escalationCode, string updatedBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.ExecuteAsync(
                "sp_BranchVisit_UpdateEscalationStatus",
                new { Id = visitId, EscalationCode = escalationCode, UpdatedBy = updatedBy },
                commandType: CommandType.StoredProcedure);

            return result;
        }

        // Update caution letter status and optionally store path to generated caution letter
        public async Task<int> UpdateCautionLetterAsync(int visitId, int cautionCode, string cautionPath, string updatedBy)
        {
            using var connection = new SqlConnection(_conStr);
            await connection.OpenAsync();

            var result = await connection.ExecuteAsync(
                "sp_BranchVisit_UpdateCautionLetter",
                new { Id = visitId, CautionCode = cautionCode, CautionPath = cautionPath, UpdatedBy = updatedBy },
                commandType: CommandType.StoredProcedure);

            return result;
        }
    }
}

