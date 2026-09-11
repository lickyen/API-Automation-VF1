// Service

using ProductivityApi.Models;
using ProductivityApi.Repositories;

namespace ProductivityApi.Services
{
    public class AnnouncementService
    {
        private readonly AnnouncementRepository _repository;

        public AnnouncementService(AnnouncementRepository repository)
        {
            _repository = repository;
        }

        public async Task<(bool Success, object Data, string Message)> CreateAsync(AnnouncementRequest request)
        {
            request.Title = request.Title.Trim();
            request.Message = request.Message.Trim();
            request.Priority = request.Priority.Trim();
            request.CreatedBy = request.CreatedBy.Trim();

            if (request.EndDate.HasValue && request.EndDate < request.StartDate)
                return (false, null, "Invalid request.");

            var id = await _repository.CreateAsync(request);

            return (true, new { Id = id }, "Record created successfully.");
        }

        public async Task<(bool Success, object Data, string Message)> GetAsync()
        {
            var data = await _repository.GetAsync();
            return (true, data, "Records retrieved successfully.");
        }

        public async Task<(bool Success, object Data, string Message)> GetByIdAsync(int id)
        {
            if (id <= 0)
                return (false, null, "Invalid request.");

            var data = await _repository.GetByIdAsync(id);

            return data == null
                ? (false, null, "Record not found.")
                : (true, data, "Record retrieved successfully.");
        }

        public async Task<(bool Success, object Data, string Message)> GetActiveAsync()
        {
            var data = await _repository.GetActiveAsync();
            return (true, data, "Records retrieved successfully.");
        }

        public async Task<(bool Success, object Data, string Message)> UpdateAsync(int id, AnnouncementUpdateRequest request)
        {
            if (id <= 0)
                return (false, null, "Invalid request.");

            if (await _repository.GetByIdAsync(id) == null)
                return (false, null, "Record not found.");

            if (request.EndDate.HasValue && request.EndDate < request.StartDate)
                return (false, null, "Invalid request.");

            request.Title = request.Title.Trim();
            request.Message = request.Message.Trim();
            request.Priority = request.Priority.Trim();
            request.UpdatedBy = request.UpdatedBy.Trim();

            var updated = await _repository.UpdateAsync(id, request);

            return updated
                ? (true, null, "Record updated successfully.")
                : (false, null, "Unable to update record.");
        }

        public async Task<(bool Success, object Data, string Message)> UpdateStatusAsync(int id, AnnouncementStatusRequest request)
        {
            if (id <= 0)
                return (false, null, "Invalid request.");

            if (await _repository.GetByIdAsync(id) == null)
                return (false, null, "Record not found.");

            request.UpdatedBy = request.UpdatedBy.Trim();

            var updated = await _repository.UpdateStatusAsync(id, request);

            return updated
                ? (true, null, "Record updated successfully.")
                : (false, null, "Unable to update record.");
        }

        public async Task<(bool Success, object Data, string Message)> DeleteAsync(int id, AnnouncementDeleteRequest request)
        {
            if (id <= 0)
                return (false, null, "Invalid request.");

            if (await _repository.GetByIdAsync(id) == null)
                return (false, null, "Record not found.");

            request.UpdatedBy = request.UpdatedBy.Trim();

            var deleted = await _repository.DeleteAsync(id, request);

            return deleted
                ? (true, null, "Record deleted successfully.")
                : (false, null, "Unable to delete record.");
        }
    }
}