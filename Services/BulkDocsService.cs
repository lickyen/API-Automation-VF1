using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class BulkDocsService
    {
        private readonly BulkDocsRepository _repository;

        public BulkDocsService(BulkDocsRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> BulkInsertAsync(BulkDocInsertRequest dto)
        {
            var result = await _repository.BulkInsertAsync(dto.Lans, dto.CreatedBy);
            return result;
        }

        public async Task<IEnumerable<BulkDocsModel>> GetLanListAsync(BulkDocListFilter filter)
        {
            return await _repository.GetLanListAsync(filter);
        }
    }
}