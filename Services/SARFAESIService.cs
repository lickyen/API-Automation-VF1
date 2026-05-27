using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class SARFAESIService
    {
        private readonly SARFAESIRepository _repository;
        private readonly string _filePath;

        private static readonly HashSet<string> AllowedExtensions = new()
        {
            ".jpg", ".jpeg", ".png", ".pdf",
            ".mp4", ".avi", ".mov", ".mkv",
            ".zip"
        };

        public static class SARFAESIFileSizeLimits
        {
            public const long Image = 2 * 1024 * 1024;   // 2 MB
            public const long Pdf = 10 * 1024 * 1024;     // 10 MB
            public const long Video = 50 * 1024 * 1024;   // 50 MB
        }

        public SARFAESIService(SARFAESIRepository repository, AppSettings appSettings)
        {
            _repository = repository;
            _filePath = appSettings.FilePaths.SARFAESIFilePath;
        }

        public Task<int> CreateAsync(SARFAESICreateDto dto)
        {
            return _repository.CreateAsync(dto);
        }

        public async Task<SARFAESIRecord?> GetByIdAsync(int id)
        {
            var record = await _repository.GetByIdAsync(id);

            if (record is null)
                return null;

            record.Files = await _repository.GetFilesByReqIdAsync(id);

            return record;
        }

        public Task<List<SARFAESIRecord>> GetByUserAsync(string userId, string? status, string? role)
        {
            return _repository.GetByUserAsync(userId, status, role);
        }

        public Task<int> UpdateAsync(int id, SARFAESICreateDto dto)
        {
            return _repository.UpdateAsync(id, dto);
        }

        public async Task<int> UploadFileAsync(SARFAESIFileUploadDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                throw new ArgumentException("File is empty");

            var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException($"Extension not allowed: {extension}");

            var category = GetFileCategory(extension);

            if (category == "UNKNOWN")
                throw new InvalidOperationException("Unsupported file type");

            var maxSize = GetMaxSize(category);

            if (dto.File.Length > maxSize)
                throw new InvalidOperationException(
                    $"File exceeds {category} limit of {maxSize / (1024 * 1024)} MB"
                );

            var record = await _repository.GetByIdAsync(dto.ReqId);

            if (record == null)
                throw new InvalidOperationException("Record not found");

            var fileName = $"{record.Lan}_{dto.ReqId}_{dto.FileType}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";
            var fullPath = Path.Combine(_filePath, fileName);

            if (!Directory.Exists(_filePath))
                Directory.CreateDirectory(_filePath);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await dto.File.CopyToAsync(stream);
            }

            var dbResult = await _repository.AddFileAsync(
                dto.ReqId,
                dto.FileType,
                fileName,
                dto.CreatedBy
            );

            if (dbResult <= 0)
                throw new InvalidOperationException("Failed to save file record in DB");

            return dbResult;
        }
        public async Task<byte[]?> DownloadFileAsync(string fileName)
        {
            return await FileHelper.ReadFileAsync(
                _filePath,
                fileName
            );
        }

        public async Task SubmitAsync(SARFAESISubmitDto dto)
        {
            var record = await _repository.GetByIdAsync(dto.ReqId);
            if (record == null)
                throw new InvalidOperationException("Record not found");

            var files = await _repository.GetFilesByReqIdAsync(dto.ReqId);
            if (files == null || files.Count == 0)
                throw new InvalidOperationException("Cannot submit — no files uploaded yet");

            await _repository.MarkCompletedAsync(dto.ReqId, dto.UpdatedBy);
        }

        public async Task<IEnumerable<dynamic>> GetReportDataAsync(SARFAESIReportFilterDto dto)
        {
            return await _repository.GetReportAsync(dto);
        }

        public async Task<byte[]?> GetReportExcelAsync(SARFAESIReportFilterDto dto)
        {
            var data = await _repository.GetReportAsync(dto);

            return ExcelHelper.GenerateExcel(data, "Data");
        }

        private string GetFileCategory(string extension)
        {
            extension = extension.ToLowerInvariant();

            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" => "IMAGE",
                ".pdf" => "PDF",
                ".mp4" or ".avi" or ".mov" or ".mkv" => "VIDEO",
                _ => "UNKNOWN"
            };
        }

        private long GetMaxSize(string category)
        {
            return category switch
            {
                "IMAGE" => SARFAESIFileSizeLimits.Image,
                "PDF" => SARFAESIFileSizeLimits.Pdf,
                "VIDEO" => SARFAESIFileSizeLimits.Video,
                _ => throw new InvalidOperationException("Unsupported file type")
            };
        }
    }
}