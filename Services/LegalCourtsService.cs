using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class LegalCourtsService
    {
        private readonly LegalCourtsRepository _repository;

        public LegalCourtsService(
            LegalCourtsRepository repository)
        {
            _repository = repository;
        }

        public Task<List<LegalCourtsModel>> GetCourtsAsync(bool? isActive)
        {
            return _repository.GetCourtsAsync(isActive);
        }

        public async Task<(bool Success, int? Id, string Message)> AddCourtAsync(
            LegalCourtAddDto dto)
        {
            var id = await _repository.AddCourtAsync(
                dto.State.Trim(),
                dto.CourtName.Trim(),
                dto.CreatedBy.Trim());

            return id > 0
                ? (true, id, "Court added successfully.")
                : (false, null, "Court already exists.");
        }

        public async Task<(bool Success, string Message)> EditCourtAsync(
            LegalCourtEditDto dto)
        {
            var result = await _repository.EditCourtAsync(
                dto.Id,
                dto.State.Trim(),
                dto.CourtName.Trim(),
                dto.UpdatedBy.Trim());

            return result switch
            {
                > 0 => (true, "Court updated successfully."),
                -1 => (false, "Court already exists."),
                _ => (false, "Court not found.")
            };
        }

        public async Task<(bool Success, string Message)> UpdateCourtStatusAsync(
            LegalCourtStatusDto dto)
        {
            var updated = await _repository.UpdateCourtStatusAsync(
                dto.Id,
                dto.IsActive,
                dto.UpdatedBy.Trim());

            return updated
                ? (true, dto.IsActive
                    ? "Court enabled successfully."
                    : "Court disabled successfully.")
                : (false, "Court not found.");
        }
    }
}