using ProductivityApi.Models;
using ProductivityApi.Models.PLF;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class NOCService
    {
        private readonly NOCRepository _repository;
        private readonly PLFRepository _plfRepository;

        public NOCService(NOCRepository repository, PLFRepository plfRepository)
        {
            _repository = repository;
            _plfRepository = plfRepository;
        }

        public async Task<NocTransaction?> GetByIdAsync(int id)
        {
            return await _repository.GetById(id);
        }

        public async Task<IEnumerable<NocTransaction>> GetByUserAsync(string userId, string status)
        {
            return await _repository.GetByUser(userId, status);
        }

        public async Task<int> InsertClosedLansAsync(IEnumerable<NocClosedLanInsertDto> lans)
        {
            return await _repository.InsertClosedLansAsync(lans);
        }

        public async Task<IEnumerable<NocClosedLan>> GetEligibleClosedLansAsync()
        {
            return await _repository.GetEligibleClosedLansAsync();
        }
        public async Task<IEnumerable<NeolotexDisb>> GetLoansByCustIdAsync(string custId)
        {
            return await _plfRepository.GetLoansByCustIdAsync(custId);
        }

        public async Task<object> GetLinkedLoansByLanAsync(string lan)
        {
            // Step 1: Get all cust ids for the lan (applicant + co-applicants)
            var lanLoans = await _plfRepository.GetLinkedLoansByLanAsync(lan);

            var custIds = lanLoans
                .Select(x => x.CustomerId)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .ToList();

            // Step 2: Get all loans for each cust id
            var result = new List<object>();
            foreach (var custId in custIds)
            {
                var loans = await _plfRepository.GetLoansByCustIdAsync(custId!);
                result.Add(new
                {
                    CustId = custId,
                    Loans = loans
                });
            }

            return result;
        }
        public async Task<bool> InsertTransactionAsync(NocTransactionInsertDto dto)
        {
            await _repository.InsertTransactionAsync(dto);
            return true;
        }
    }
}