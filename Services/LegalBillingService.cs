using ClosedXML.Excel;
using ProductivityApi.Constants;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Data;

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

        public async Task<int> DeleteAdvocateAsync(int advocateId, string? modifiedBy)
            => await _repository.DeleteAdvocateAsync(advocateId, modifiedBy);

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
        //  Legal Billing 
        // ─────────────────────────────────────────────
        public async Task<LegalBillingDetailsDto?> GetLegalBillingByIdAsync(int id)
             => await _repository.GetLegalBillingByIdAsync(id);

        public async Task<List<LegalBillingListDto>> GetLegalBillingByUserAsync(LegalBillingGetByUserDto dto)
            => await _repository.GetLegalBillingByUserAsync(dto);

        public async Task<int> CreateLegalBillingAsync(LegalBillingCreateDto dto)
        {
            return await _repository.CreateLegalBillingAsync(dto);
        }

        public async Task<int> ResubmitLegalBillingAsync(LegalBillingResubmitDto dto)
        {
            var current = await _repository.GetLegalBillingByIdAsync(dto.Id);
            if (current is null)
                return -1;

            if (!current.Transaction.Status.EndsWith("SentBack", StringComparison.OrdinalIgnoreCase))
                return -2;

            return await _repository.ResubmitLegalBillingAsync(dto);
        }

        public async Task<int> UpdateLegalBillingStatusAsync(LegalBillingActionDto dto)

        {
            var billingDetails = await _repository.GetLegalBillingByIdAsync(dto.Id);
            if (billingDetails is null)
                return -1;

            var nextStatus = ResolveNextStatus(dto.Role, dto.Action, billingDetails.Transaction.Status);
            if (nextStatus is null)
                return -2;

            return await _repository.UpdateLegalBillingStatusAsync(dto, nextStatus);
        }

        public async Task<int> UpdateLegalBillingPaymentAsync(LegalBillingUpdatePaymentDto dto)
        {
            var billingDetails = await _repository.GetLegalBillingByIdAsync(dto.Id);
            if (billingDetails is null)
                return -1;

            var nextStatus = ResolveNextStatus(LegalBillingRoles.Legal, dto.PaymentStatus, billingDetails.Transaction.Status);
            if (nextStatus is null)
                return -2;

            return await _repository.UpdateLegalBillingPaymentAsync(dto, nextStatus);
        }
        private static string? ResolveNextStatus(string role, string action, string? currentStatus)
        {
            role = role.Trim().ToUpperInvariant();
            action = action.Trim().ToLowerInvariant();
            currentStatus ??= string.Empty;

            return (role, action) switch
            {
                // ZDSH
                ("ZDSH", "approved") when currentStatus == LegalBillingStatus.Initiated => LegalBillingStatus.ZDSHApproved,
                ("ZDSH", "rejected") when currentStatus == LegalBillingStatus.Initiated => LegalBillingStatus.ZDSHRejected,
                ("ZDSH", "sentback") when currentStatus == LegalBillingStatus.Initiated => LegalBillingStatus.ZDSHSentBack,

                // MZDSH can act only after ZDSH approved
                ("MZDSH", "approved") when currentStatus == LegalBillingStatus.ZDSHApproved => LegalBillingStatus.MZDSHApproved,
                ("MZDSH", "rejected") when currentStatus == LegalBillingStatus.ZDSHApproved => LegalBillingStatus.MZDSHRejected,
                ("MZDSH", "sentback") when currentStatus == LegalBillingStatus.ZDSHApproved => LegalBillingStatus.MZDSHSentBack,

                // CDSO can act only after MZDSH approved
                ("CDSO", "approved") when currentStatus == LegalBillingStatus.MZDSHApproved => LegalBillingStatus.CDSOApproved,
                ("CDSO", "rejected") when currentStatus == LegalBillingStatus.MZDSHApproved => LegalBillingStatus.CDSORejected,
                ("CDSO", "sentback") when currentStatus == LegalBillingStatus.MZDSHApproved => LegalBillingStatus.CDSOSentBack,

                // Legal can complete only after CDSO approved
                ("LEGAL", _) when currentStatus == LegalBillingStatus.CDSOApproved => LegalBillingStatus.Completed,

                _ => null
            };
        }

        public async Task<IEnumerable<dynamic>> GetReportDataAsync(LegalBillingReportFilterDto dto)
        {
            return await _repository.GetLegalBillingReportAsync(dto);
        }

        public async Task<byte[]?> GetReportExcelAsync(LegalBillingReportFilterDto dto)
        {
            var data = await _repository.GetLegalBillingReportAsync(dto);

            return ExcelHelper.GenerateExcel(data, "Data");
        }
    }

}