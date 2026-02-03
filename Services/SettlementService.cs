using Novell.Directory.Ldap.Rfc2251;
using ProductivityApi.Models;
using ProductivityApi.Repositories;

namespace ProductivityApi.Services
{
    public class SettlementService
    {
        private readonly SettlementRepository _repository;

        public SettlementService(SettlementRepository repository)
        {
            _repository = repository;
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


    }
}
