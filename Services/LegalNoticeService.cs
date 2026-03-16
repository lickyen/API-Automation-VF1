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

        //public async Task<List<LegalNoticeModel>> GetLegalNoticesAsync()
        //{
        //    return await _repository.GetLegalNoticesAsync();
        //}

        public async Task<List<LegalNoticeModel>> GetLegalNoticesAsync(string? noticeType, string? subType, DateTime? fromDate, DateTime? toDate)
        {
            var notices = await _repository.GetLegalNoticesAsync();

            foreach (var notice in notices)
            {
                var uploads = await _repository.GetLegalNoticeByLanAsync(notice.Lan);

                // Apply filters on uploads
                if (!string.IsNullOrEmpty(noticeType))
                    uploads = uploads.Where(x => x.NoticeType == noticeType).ToList();

                if (!string.IsNullOrEmpty(subType))
                    uploads = uploads.Where(x => x.SubType == subType).ToList();

                if (fromDate.HasValue)
                    uploads = uploads.Where(x => x.UploadedAt.Date >= fromDate.Value.Date).ToList();

                if (toDate.HasValue)
                    uploads = uploads.Where(x => x.UploadedAt.Date <= toDate.Value.Date).ToList();

                notice.Uploads = uploads;
            }

            // remove notices that have no uploads after filtering
            notices = notices.Where(x => x.Uploads != null && x.Uploads.Any()).ToList();

            return notices;
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

            var file = new LegalNoticeUploads();

            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                // Get the latest file matching NoticeType and SubType
                file = uploads
                    .Where(x => x.NoticeType == request.NoticeType && x.SubType == request.SubType)
                    .OrderByDescending(x => x.UploadedAt)
                    .FirstOrDefault();
            }
            else
            {
                // Get file by exact FileName
                file = uploads.FirstOrDefault(x => x.FilePath == request.FileName);
            }

            if (string.IsNullOrWhiteSpace(file?.FilePath))
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
