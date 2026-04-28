using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class NOCService
    {
        private readonly NOCRepository _repository;

        public NOCService(NOCRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<object>> GetPendingLoans()
        {
            return await _repository.GetPendingLoans();
        }

        public async Task<IEnumerable<object>> GetLinkedLoans(string lan)
        {
            if (string.IsNullOrEmpty(lan))
                throw new ArgumentException("LAN cannot be empty");

            return await _repository.GetLinkedLoans(lan);
        }

        public async Task<IEnumerable<object>> GetRequests(string userId, string status)
        {
            return await _repository.GetRequests(userId, status);
        }

        public async Task<object> GetRequestById(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid request id");

            return await _repository.GetRequestById(id);
        }

        public async Task<FcRefundBulkUploadResult> AddLanAsync(List<NeolotexDisb> dtos)
          => await _repository.AddLanAsync(dtos);
    }
}