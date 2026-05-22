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
            // Step 1: Get all loans for the lan (borrower only)
            var loans = await _plfRepository.GetLinkedLoansByLanAsync(lan);

            // Step 2: Get all customers for the lan
            var customers = await _plfRepository.GetCustomersByLanAsync(lan);

            var custList = customers
                .Where(x => !string.IsNullOrEmpty(x.CustomerId))
                .Select(x => new
                {
                    CustId = x.CustomerId,
                    CustName = x.CustomerName,
                    ApplicantType = x.ApplicantType                
                })
                .ToList();

            // Step 3: Attach customer list to each loan
            var result = loans.Select(loan => new
            {
                LoanAccountNo = loan.LoanAccountNo,
                LoanStatus = loan.LoanStatus,
                NocStatus = loan.NocStatus,
                DisbDate = loan.DisbursementDate,
                ClosureDate = loan.LoanClosureDate,
                Customers = custList
            });

            return result;
        }
        public async Task<bool> InsertTransactionAsync(NocTransactionInsertDto dto)
        {
            await _repository.InsertTransactionAsync(dto);
            return true;
        }
    }
}