using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class LegalBillingService
    {
        private readonly LegalBillingRepository _repository;

        public LegalBillingService(LegalBillingRepository repository)
        {
            _repository = repository;
        }

        // ─────────────────────────────────────────────
        //  Advocate Master
        // ─────────────────────────────────────────────

        public async Task<List<AdvocateMaster>> GetAllAdvocatesAsync()
            => await _repository.GetAllAdvocatesAsync();

        public async Task<AdvocateMaster?> GetAdvocateByIdAsync(int advocateId)
            => await _repository.GetAdvocateByIdAsync(advocateId);

        public async Task<int> CreateAdvocateAsync(AdvocateMasterCreateDto dto)
            => await _repository.CreateAdvocateAsync(dto);

        public async Task<int> UpdateAdvocateAsync(AdvocateMasterUpdateDto dto)
            => await _repository.UpdateAdvocateAsync(dto);

        public async Task<int> DeleteAdvocateAsync(int advocateId, string? deletedBy)
            => await _repository.DeleteAdvocateAsync(advocateId, deletedBy);

        // ─────────────────────────────────────────────
        //  Advocate Category
        // ─────────────────────────────────────────────

        public async Task<List<AdvocateCategory>> GetAllAdvocateCategoriesAsync()
            => await _repository.GetAllAdvocateCategoriesAsync();
        public async Task<int> CreateAdvocateCategoryAsync(AdvocateCategoryCreateDto dto)
            => await _repository.CreateAdvocateCategoryAsync(dto);

        public async Task<int> UpdateAdvocateCategoryAsync(AdvocateCategoryUpdateDto dto)
            => await _repository.UpdateAdvocateCategoryAsync(dto);

        public async Task<int> DeleteAdvocateCategoryAsync(int id, string? modifiedBy)
            => await _repository.DeleteAdvocateCategoryAsync(id, modifiedBy);

        // ─────────────────────────────────────────────
        //  Legal Billing - Create
        // ─────────────────────────────────────────────

        public async Task<int> CreateLegalBillingAsync(LegalBillingCreateDto dto)
        {
            return await _repository.CreateLegalBillingAsync(dto);
        }

        public async Task<int> UpdateLegalBillingStatusAsync(int id, string role, string action, string modifiedBy, string? remarks = null)
        {
            return await _repository.UpdateLegalBillingStatusAsync(id, role, action, modifiedBy, remarks);
        }

        public async Task<int> UpdateLegalBillingPaymentAsync(LegalBillingUpdatePaymentDto dto)
        {
            return await _repository.UpdateLegalBillingPaymentAsync(dto);
        }

    }

}