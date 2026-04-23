using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class WabaService
    {
        private readonly WabaRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly AppSettings _appSettings;
        private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024; // 10MB

        public WabaService(WabaRepository repository, EmailService emailService, TemplateService templateService, AppSettings appSettings)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _appSettings = appSettings;
        }
        public async Task<int> SaveWabaLogAsync(WabaModel dto)
        {

            return await _repository.SavaWabaLogAsync(dto);
        }

    }
}
