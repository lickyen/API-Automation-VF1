using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
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
        private readonly string _filePath;


        public LegalBillingService(LegalBillingRepository repository, AppSettings appSettings)
        {
            _repository = repository;
            _filePath = appSettings.FilePaths.LegalBillingFilePath;
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
            var result =  await _repository.CreateLegalBillingAsync(dto);

            if (result > 0)
            {
                if (dto.CancelledChequeFile != null)
                {
                    await UploadBase64FileAsync(result, dto.CancelledChequeFile, "CancelledCheque");
                }
            }

            return result;
        }

        public async Task<int> ResubmitLegalBillingAsync(LegalBillingResubmitDto dto)
        {
            var current = await _repository.GetLegalBillingByIdAsync(dto.Id);
            if (current is null)
                return -1;

            if (!current.Transaction.Status.EndsWith("SentBack", StringComparison.OrdinalIgnoreCase))
                return -2;

            var result = await _repository.ResubmitLegalBillingAsync(dto);

            if (result > 0)
            {
                if (dto.CancelledChequeFile != null)
                {
                    await UploadBase64FileAsync(dto.Id, dto.CancelledChequeFile, "CancelledCheque");
                }
            }

            return result;  
        }

        public async Task<int> UpdateLegalBillingStatusAsync(LegalBillingActionDto dto)

        {
            var data = await _repository.GetLegalBillingByIdAsync(dto.Id);
            if (data is null)
                return -1;

            var nextStatus = ResolveNextStatus(dto.Role, dto.Action, data);
            if (nextStatus is null)
                return -2;

            return await _repository.UpdateLegalBillingStatusAsync(dto, nextStatus);
        }

        public async Task<int> UpdateLegalBillingPaymentAsync(LegalBillingUpdatePaymentDto dto)
        {
            var data = await _repository.GetLegalBillingByIdAsync(dto.Id);
            if (data is null)
                return -1;

            var nextStatus = ResolveNextStatus(LegalBillingRoles.Legal, dto.PaymentStatus, data, dto.InvoiceAmount);
            if (nextStatus is null)
                return -2;

            var result =  await _repository.UpdateLegalBillingPaymentAsync(dto, nextStatus);

            if (result > 0)
            {
                if (dto.InvoiceFile != null)
                {
                    await UploadBase64FileAsync(dto.Id, dto.InvoiceFile, "Invoice");
                }
            }
            return result;
        }

        public async Task<int> UpdateLegalBillingFinanceAsync(LegalBillingUpdateFinanceDto dto)
        {
            var data = await _repository.GetLegalBillingByIdAsync(dto.Id);
            if (data is null)
                return -1;

            var nextStatus = ResolveNextStatus(LegalBillingRoles.Finance, "", data);
            if (nextStatus is null)
                return -2;

            return await _repository.UpdateLegalBillingFinanceAsync(dto, nextStatus);
        }

        private static string? ResolveNextStatus(string role, string action, LegalBillingDetailsDto data, decimal? dtoInvoiceAmount = null)
        {
            role = role.Trim().ToUpperInvariant();
            action = action.Trim().ToLowerInvariant();
            string? currentStatus = data?.Transaction?.Status;
            string? paymentType = data?.Transaction?.PaymentType;

            bool isBeforeService = string.Equals(paymentType, "Before Service", StringComparison.OrdinalIgnoreCase);

            var paymentAmount = data?.Transaction?.PaymentAmount;
            var invoiceAmount = dtoInvoiceAmount;

            //return (role, action) switch
            //{
            //    // ZDSH
            //    ("ZDSH", "approved") when currentStatus == LegalBillingStatus.Initiated => LegalBillingStatus.ZDSHApproved,
            //    ("ZDSH", "rejected") when currentStatus == LegalBillingStatus.Initiated => LegalBillingStatus.ZDSHRejected,
            //    ("ZDSH", "sentback") when currentStatus == LegalBillingStatus.Initiated => LegalBillingStatus.ZDSHSentBack,

            //    // MZDSH can act only after ZDSH approved
            //    ("MZDSH", "approved") when currentStatus == LegalBillingStatus.ZDSHApproved => LegalBillingStatus.MZDSHApproved,
            //    ("MZDSH", "rejected") when currentStatus == LegalBillingStatus.ZDSHApproved => LegalBillingStatus.MZDSHRejected,
            //    ("MZDSH", "sentback") when currentStatus == LegalBillingStatus.ZDSHApproved => LegalBillingStatus.MZDSHSentBack,

            //    // CDSO can act only after MZDSH approved
            //    ("CDSO", "approved") when currentStatus == LegalBillingStatus.MZDSHApproved => LegalBillingStatus.CDSOApproved,
            //    ("CDSO", "rejected") when currentStatus == LegalBillingStatus.MZDSHApproved => LegalBillingStatus.CDSORejected,
            //    ("CDSO", "sentback") when currentStatus == LegalBillingStatus.MZDSHApproved => LegalBillingStatus.CDSOSentBack,

            //    //FINANCE (BEFORE SERVICE)
            //    ("FINANCE", "") when currentStatus == LegalBillingStatus.CDSOApproved && isBeforeService
            //            => LegalBillingStatus.FinanceApproved,

            //    ("LEGAL", "payment completed")
            //        when (currentStatus == LegalBillingStatus.CDSOApproved && !isBeforeService)
            //        || (currentStatus == LegalBillingStatus.FinanceApproved && isBeforeService)
            //        || (currentStatus == LegalBillingStatus.Pending)
            //        => LegalBillingStatus.Completed,

            //    ("LEGAL", "payment on hold")
            //        when (currentStatus == LegalBillingStatus.CDSOApproved && !isBeforeService)
            //          || (currentStatus == LegalBillingStatus.FinanceApproved && isBeforeService)
            //          || (currentStatus == LegalBillingStatus.Pending)
            //            => LegalBillingStatus.Pending,

            //    ("LEGAL", "others")
            //        when (currentStatus == LegalBillingStatus.CDSOApproved && !isBeforeService)
            //          || (currentStatus == LegalBillingStatus.FinanceApproved && isBeforeService)
            //          || (currentStatus == LegalBillingStatus.Pending)
            //            => LegalBillingStatus.Pending,

            //    ("LEGAL", "cancelled")
            //        when (currentStatus == LegalBillingStatus.CDSOApproved && !isBeforeService)
            //          || (currentStatus == LegalBillingStatus.FinanceApproved && isBeforeService)
            //          || (currentStatus == LegalBillingStatus.Pending)
            //            => LegalBillingStatus.Cancelled,

            //    _ => null
            //};

            return (role, action) switch
            {
                // ZDSH
                ("ZDSH", "approved") when currentStatus == LegalBillingStatus.Initiated => LegalBillingStatus.ZDSHApproved,
                ("ZDSH", "rejected") when currentStatus == LegalBillingStatus.Initiated => LegalBillingStatus.ZDSHRejected,
                ("ZDSH", "sentback") when currentStatus == LegalBillingStatus.Initiated => LegalBillingStatus.ZDSHSentBack,

                // MZDSH
                ("MZDSH", "approved")
                    when (currentStatus == LegalBillingStatus.Initiated
                       || currentStatus == LegalBillingStatus.ZDSHApproved)
                    => LegalBillingStatus.MZDSHApproved,

                ("MZDSH", "rejected")
                    when (currentStatus == LegalBillingStatus.Initiated
                       || currentStatus == LegalBillingStatus.ZDSHApproved)
                    => LegalBillingStatus.MZDSHRejected,

                ("MZDSH", "sentback")
                    when (currentStatus == LegalBillingStatus.Initiated
                       || currentStatus == LegalBillingStatus.ZDSHApproved)
                    => LegalBillingStatus.MZDSHSentBack,

                // CDSO after MZDSH approval
                ("CDSO", "approved") when currentStatus == LegalBillingStatus.MZDSHApproved
                    => LegalBillingStatus.CDSOApproved,

                ("CDSO", "rejected") when currentStatus == LegalBillingStatus.MZDSHApproved
                    => LegalBillingStatus.CDSORejected,

                ("CDSO", "sentback") when currentStatus == LegalBillingStatus.MZDSHApproved
                    => LegalBillingStatus.CDSOSentBack,

                // FINANCE (BEFORE SERVICE)
                ("FINANCE", "") when currentStatus == LegalBillingStatus.CDSOApproved && isBeforeService
                    => LegalBillingStatus.FinanceApproved,

                // LEGAL
                ("LEGAL", "payment completed")
                    when (currentStatus == LegalBillingStatus.CDSOApproved && !isBeforeService)
                      || (currentStatus == LegalBillingStatus.FinanceApproved && isBeforeService)
                      || (currentStatus == LegalBillingStatus.Pending)
                    => LegalBillingStatus.Completed,

                ("LEGAL", "payment on hold")
                    when (currentStatus == LegalBillingStatus.CDSOApproved && !isBeforeService)
                      || (currentStatus == LegalBillingStatus.FinanceApproved && isBeforeService)
                      || (currentStatus == LegalBillingStatus.Pending)
                    => LegalBillingStatus.Pending,

                ("LEGAL", "others")
                    when (currentStatus == LegalBillingStatus.CDSOApproved && !isBeforeService)
                      || (currentStatus == LegalBillingStatus.FinanceApproved && isBeforeService)
                      || (currentStatus == LegalBillingStatus.Pending)
                    => LegalBillingStatus.Pending,

                ("LEGAL", "cancelled")
                    when (currentStatus == LegalBillingStatus.CDSOApproved && !isBeforeService)
                      || (currentStatus == LegalBillingStatus.FinanceApproved && isBeforeService)
                      || (currentStatus == LegalBillingStatus.Pending)
                    => LegalBillingStatus.Cancelled,

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
        public async Task<int> UploadBase64FileAsync(int reqId, FileUploadBase64 fileDto, string fileType)
        {
            if (fileDto == null || string.IsNullOrWhiteSpace(fileDto.Base64))
                throw new ArgumentException("File is empty");

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".zip" };

            var extension = Path.GetExtension(fileDto.FileName);

            var fileName = $"{reqId}_{fileType}{extension}";
            var fullPathFileName = fileName;

            var result = await FileHelper.SaveBase64FileAsync(
                _filePath,
                fullPathFileName,
                fileDto.Base64,
                allowedExtensions
            );

            if (!result.Success)
                throw new InvalidOperationException(result.Error);

            var dbResult = await _repository.UpdateFilePathAsync(reqId, fileType, fileName);

            if (dbResult <= 0)
                throw new InvalidOperationException("Failed to update file path in DB");

            return dbResult;
        }

        public async Task<byte[]?> DownloadFileAsync(string fileName)
        {
            return await FileHelper.ReadFileAsync(
                _filePath,
                fileName
            );
        }

        public async Task<IEnumerable<CollectionUserMappingDto>> GetAllocationAsync()
        {
            return await _repository.GetAllocationAsync();
        }
        public async Task<int> UpdateAllocationBulkAsync(List<CollectionUserMappingUpdateDto> dtos)
            => await _repository.UpdateAllocationAsync(dtos);

    }

}