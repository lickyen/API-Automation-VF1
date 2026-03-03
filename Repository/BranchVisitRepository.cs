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
    }
}

