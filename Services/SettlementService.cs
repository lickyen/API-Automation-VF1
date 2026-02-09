using Azure.Core;
using Novell.Directory.Ldap.Rfc2251;
using ProductivityApi.Models;
using ProductivityApi.Repositories;

namespace ProductivityApi.Services
{
    public class SettlementService
    {
        private readonly SettlementRepository _repository;
        private readonly PLFService _plfService;
        private readonly BranchServices _branchService;

        public SettlementService(SettlementRepository repository, PLFService plfService, BranchServices branchService)
        {
            _repository = repository;
            _plfService = plfService;
            _branchService = branchService;
        }

        // Create a new settlement request
        public async Task<int> CreateSettlementAsync(CreateSettlementDto dto)
        {
            var result = await _repository.CreateSettlementAsync(dto);

            return result;
        }

        // Submit settlement request to COPS
        public async Task<int> SubmitSettlementAsync(SubmitSettlementDto dto)
        {
            var result = await _repository.SubmitSettlementAsync(dto);
            return result;
        }

        // Add payment
        public async Task<int> AddPaymentAsync(SettlementAddPaymentDto dto)
        {
            var result =  await _repository.AddPaymentAsync(dto);
            return result;
        }

        // Get settlement details with logs and payments
        public async Task<SettlementTransaction?> GetSettlementByIdAsync(int id)
        {
            var result = await _repository.GetSettlementByIdAsync(id);
            return result;
        }

        public async Task<int> ApproveSettlementAsync(ApproveSettlementDto dto)
        {
            var result = await _repository.ApproveSettlementAsync(dto);
            return result;
        }

        public async Task<int> EditPaymentAsync(SettlementEditPaymentDto dto)
        {
            var result = await _repository.EditPaymentAsync(dto);
            return result;
        }

        public async Task<int> DeletePaymentAsync(int id)
        {
            var result = await _repository.DeletePaymentAsync(id);
            return result;
        }

        public async Task<IEnumerable<SettlementDashboard>> GetDashboardByUserAsync(SettlementDashboardRequestDto dto)
        {
            return await _repository.GetDashboardByUserAsync(dto);
        }

        public async Task<int> CancelSettlementAsync(CancelSettlementDto dto)
        {
            var result = await _repository.CancelSettlementAsync(dto);
            return result;
        }
        public async Task<LanDetailsPlf?> GetLanDetailsAsync(string lan)
        {
            var loan = await _plfService.GetFinance(lan);
            LanDetailsPlf data = new LanDetailsPlf();

            if (loan == null || loan?.returnStatus?.returnCode != "0000")
            {
                throw new Exception("LAN not found or InActive");
            }

            var cif = loan.customer?.cif;

            if (string.IsNullOrWhiteSpace(cif))
            {
                throw new Exception("CIF not found or InActive");
            }

            data.CustId = cif;
            data.CustName = loan.customer?.personalInfo?.shortName;
            data.DisbDate = loan.financeSchedule?.financeDetail?.finStartDate; 
            data.BranchCode = loan.financeSchedule?.financeDetail?.finBranch;
            if(!string.IsNullOrEmpty(data.BranchCode))
            {
                var branch = _branchService.GetBranchByID(data.BranchCode);
                data.BranchName = branch?.BranchName;
            }

            var customer = await _plfService.GetCustomerDetails(cif);

            if (customer == null || customer?.ReturnStatus?.returnCode != "0000")
            {
                throw new Exception("Customer not found or InActive");
            }

            data.Mobile = customer.Phones?
                .OrderByDescending(p => p.priority)
                .Select(p => p.phoneNumber)
                .FirstOrDefault();

            return data;
        }

    }
}
