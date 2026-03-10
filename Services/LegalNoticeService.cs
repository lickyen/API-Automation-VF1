using ProductivityApi.Models;
using ProductivityApi.Repositories;
using ProductivityApi.Repository;
using System.Text.RegularExpressions;

namespace ProductivityApi.Services
{
    public class LegalNoticeService
    {
        private readonly LegalNoticeRepository _repository;
        private readonly string _filePath;

        public LegalNoticeService(LegalNoticeRepository repository, AppSettings appSettings)
        {
            _repository = repository;
            _filePath = appSettings.FilePaths.LegalNoticeFilePath;
        }

        public async Task<List<LegalNoticeTypes>> GetLegalNoticeTypesAsync()
        {
            return await _repository.GetLegalNoticeTypesAsync();
        }

        public async Task<List<LegalNoticeModel>> GetLegalNoticesAsync()
        {
            return await _repository.GetLegalNoticesAsync();
        }

        public async Task<List<LegalNoticeUploads>> GetLegalNoticeByLanAsync(string lan)
        {
            return await _repository.GetLegalNoticeByLanAsync(lan);
        }

        //public async Task<string> UploadLegalNoticeDocumentAsync(LegalNoticeUploadDto dto)
        //{
        //    return await _repository.UploadLegalNoticeDocumentAsync(dto);
        //}

        public async Task<string> UploadDocumentAsync(LegalNoticeUploadDto dto)
        {
            if (!string.IsNullOrEmpty(dto.Base64))
            {
                var folderPath = _filePath;

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string noticeType = Regex.Replace(dto.NoticeType ?? "", @"[^a-zA-Z0-9]", "");
                string subType = Regex.Replace(dto.SubType ?? "", @"[^a-zA-Z0-9]", "");

                var fileName = $"{dto.Lan}_{noticeType}_{subType}_{DateTime.Now:yyyyMMddHHmmss}.{dto.FileType}";

                var fullPath = Path.Combine(folderPath, fileName);

                byte[] fileBytes = Convert.FromBase64String(dto.Base64);
                await File.WriteAllBytesAsync(fullPath, fileBytes);

                return await _repository.UploadLegalNoticeDocumentAsync(dto, fileName);
            }

            return "Base64 content is empty";
        }
        public async Task<LegalNoticeDownloadResponseDto> DownloadDocumentAsync(LegalNoticeDownloadRequestDto request)
        {
            var uploads = await _repository.GetLegalNoticeByLanAsync(request.Lan);

            if (uploads == null || !uploads.Any())
                throw new FileNotFoundException($"No legal notice files found for LAN {request.Lan}");

            // Get the latest matching file
            var file = uploads
                .Where(x => x.NoticeType == request.NoticeType &&
                           (string.IsNullOrWhiteSpace(request.SubType) || x.SubType == request.SubType))
                .OrderByDescending(x => x.UploadedAt)
                .FirstOrDefault();

            if (file == null)
                throw new FileNotFoundException("No matching legal notice file found");

            var fullFilePath = Path.Combine(_filePath, file.FilePath);

            if (!File.Exists(fullFilePath))
                throw new FileNotFoundException("No matching legal notice file found");

            byte[] fileBytes = await File.ReadAllBytesAsync(fullFilePath);

            var response = new LegalNoticeDownloadResponseDto
            {
                Lan = request.Lan,
                NoticeType = request.NoticeType,
                SubType = request.SubType,
                File = new LegalNoticeDownloadFileDto
                {
                    FileName = Path.GetFileName(file.FilePath),
                    Base64 = Convert.ToBase64String(fileBytes),
                    UploadedAt = file.UploadedAt
                }
            };

            return response;
        }
    }
}
