using ProductivityApi.Models;
using ProductivityApi.Repositories;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class LegalNoticeService
    {
        private readonly LegalNoticeRepository _repository;

        public LegalNoticeService(LegalNoticeRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<LegalNoticeTypes>> GetLegalNoticeTypesAsync()
        {
            return await _repository.GetLegalNoticeTypesAsync();
        }

        public async Task<List<LegalNoticeModel>> GetLegalNoticesAsync()
        {
            return await _repository.GetLegalNoticesAsync();
        }

        public async Task<LegalNoticeUploads?> GetLegalNoticeByLanAsync(string lan)
        {
            return await _repository.GetLegalNoticeByLanAsync(lan);
        }

        public async Task<string> UploadLegalNoticeDocumentAsync(LegalNoticeUploadDto dto)
        {
            return await _repository.UploadLegalNoticeDocumentAsync(dto);
        }
    }
}
