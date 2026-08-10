using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class WabaWtsappHookService
    {
        private readonly WabaWtsappHookRepository _repository;
      

        public WabaWtsappHookService(WabaWtsappHookRepository repository)
        {
            _repository = repository;
            
        }
        public async Task<long> SaveMessageAsync(
           MessageModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var createdAtUtc = DateTimeOffset.UtcNow;

            var insertedId = await _repository.InsertAsync(
                request,
                createdAtUtc);

            return insertedId;
        }
    }


}

