using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class IndusArchivedService
    {
        private readonly IndusArchivedRepository _repository;

        public IndusArchivedService(IndusArchivedRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<IndusArchivedMaster>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IndusArchivedMaster?> GetByLanAsync(string lan)
        {
            return await _repository.GetByLanAsync(lan);
        }

        public async Task<List<IndusArchivedMaster>> GetLinkedLoansByLanAsync(string lan)
        {
            return await _repository.GetLinkedLoansByLanAsync(lan);
        }
    }
}
