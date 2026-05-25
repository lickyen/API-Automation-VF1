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

        public async Task<int> InsertClosedLansAsync(IEnumerable<NocClosedLanInsertDto> lans)
        {
            return await _repository.InsertClosedLansAsync(lans);
        }
        public async Task<IEnumerable<NocClosedLan>> GetEligibleLansAsync()
        {
            return await _repository.GetEligibleLansAsync();
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
        public async Task<NocTransaction?> GetByIdAsync(int id)
        {
            return await _repository.GetById(id);
        }

        public async Task<NocApplicantDetails?> GetApplicantByLanAsync(string lan)
        {
            return await _repository.GetApplicantByLanAsync(lan);
        }

        public async Task<(IEnumerable<UserLanDto>? Data, string? Error)> GetLansByUserAsync(GetLanByUserRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return (null, "UserId is required.");

            if (string.IsNullOrWhiteSpace(dto.Role))
                return (null, "Role is required.");

            if (string.IsNullOrWhiteSpace(dto.Status))
                return (null, "Status is required.");

            return await _repository.GetLansByUserAsync(
                dto.UserId,
                dto.Role,
                dto.Status
            );
        }
        public async Task<bool> UpdateTransactionStatusAsync(UpdateNocStatusDto dto)
        {
            return await _repository.UpdateTransactionStatusAsync(dto);
        }

        public async Task<bool> UpdatePropertyOwnerAsync(UpdatePropertyOwnerDto dto)
        {
            return await _repository.UpdatePropertyOwnerAsync(dto);
        }

        public async Task<List<NeolotexDisb>> GetCustomersByLanAsync(string lan)
        {
            return await _plfRepository.GetCustomersByLanAsync(lan);
        }

        public async Task<bool> UploadAcknowledgementAndKycAsync(
            UploadAcknowledgementKycDto dto,
            IWebHostEnvironment env)
        {
            string uploadRoot = Path.Combine(env.WebRootPath, "uploads", "noc");

            if (!Directory.Exists(uploadRoot))
                Directory.CreateDirectory(uploadRoot);

            string? acknowledgementPath = null;
            string? kycPath = null;

            if (dto.AcknowledgementFile != null)
            {
                var ackFileName =
                    $"ACK_{dto.Id}_{DateTime.Now.Ticks}{Path.GetExtension(dto.AcknowledgementFile.FileName)}";

                var ackFullPath = Path.Combine(uploadRoot, ackFileName);

                using (var stream = new FileStream(ackFullPath, FileMode.Create))
                {
                    await dto.AcknowledgementFile.CopyToAsync(stream);
                }

                acknowledgementPath = $"/uploads/noc/{ackFileName}";
            }

            if (dto.KycFile != null)
            {
                var kycFileName =
                    $"KYC_{dto.Id}_{DateTime.Now.Ticks}{Path.GetExtension(dto.KycFile.FileName)}";

                var kycFullPath = Path.Combine(uploadRoot, kycFileName);

                using (var stream = new FileStream(kycFullPath, FileMode.Create))
                {
                    await dto.KycFile.CopyToAsync(stream);
                }

                kycPath = $"/uploads/noc/{kycFileName}";
            }

            return await _repository.UploadAcknowledgementAndKycAsync(
                dto.Id,
                acknowledgementPath,
                kycPath,
                dto.UpdatedBy
            );
        }
    }
}