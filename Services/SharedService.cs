using ProductivityApi.Models;
using ProductivityApi.Repositories;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class SharedService
    {
        private readonly SharedRepository _repository;

        public SharedService(SharedRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<string>?> GetVistaarBankAccountsAsync()
        {
            var result = await _repository.GetVistaarBankAccountsAsync();
            return result;
        }
    }
}
