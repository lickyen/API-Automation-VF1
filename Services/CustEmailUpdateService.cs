using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class CustEmailUpdateService
    {

        private readonly CustEmailUpdateRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly AppSettings _appSettings;
        private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024; // 10MB

        public CustEmailUpdateService(CustEmailUpdateRepository repository, EmailService emailService, TemplateService templateService, AppSettings appSettings)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _appSettings = appSettings;
        }

        public async Task<List<CustomerEmailDto>> GetCustEmailsByStatusAsync(string status)
       => await _repository.GetCustEmailsAsync(status);

        public async Task<CustomerEmailDto?> GetByIdAsync(int id)
          => await _repository.GetByIdAsync(id);

        public async Task<int> BranchSubmitAsync(UpdateDto dto)
        {
           

            return await _repository.SubmitAsync(dto);
        }

        public async Task<byte[]?> GetReportDataAsync()
        {
            var data = await _repository.GetReportDataAsync();
            return ExcelHelper.GenerateExcel(data, "Data");
        }

    }
}
