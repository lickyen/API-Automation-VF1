using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Repositories;

namespace ProductivityApi.Services
{
    public class VOTService 
    {
        private readonly VOTRepository _repository;
        private readonly AppSettings _appSettings;

        public VOTService(VOTRepository repository, AppSettings appSettings)
        {
            _repository = repository;
            _appSettings = appSettings;
        }

        public async Task<Response> BulkInsertLanAsync(
            VOTModel.BulkUploadLanRequest request)
        {
            await _repository.BulkInsertLanAsync(request);

            return new Response(
                true,
                null,
                "LANs uploaded successfully.");
        }

        public async Task<Response> GetLansAsync()
        {
            var result = await _repository.GetLansAsync();

            return new Response(
                true,
                result);
        }

        public async Task<Response> GetLansByUserAsync(
            VOTModel.GetLansByUserRequest request)
        {
            var result = await _repository.GetLansByUserAsync(request);

            return new Response(
                true,
                result);
        }

        public async Task<Response> UpdateLanAsync(
    VOTModel.UpdateLanRequest request)
        {
            await _repository.UpdateLanAsync(request);

            return new Response(
                true,
                null,
                "LAN mapping updated successfully.");
        }

        public async Task<Response> UpdateLanStatusAsync(
            VOTModel.UpdateLanStatusRequest request)
        {
            await _repository.UpdateLanStatusAsync(request);

            return new Response(
                true,
                null,
                "LAN status updated successfully.");
        }

        public async Task<Response> CreateCaseAsync(
            VOTModel.CreateCaseRequest request)
        {
            var caseId = await _repository.InsertCaseAsync(request);

            return new Response(
                true,
                caseId,
                "Case created successfully.");
        }

        public async Task<Response> GetCaseByIdAsync(int id)
        {
            var result = await _repository.GetCaseByIdAsync(id);

            return new Response(
                true,
                result);
        }

        public async Task<Response> GetCasesByUserAsync(
    VOTModel.GetCasesByUserRequest request)
        {
            var result = await _repository.GetCasesByUserAsync(request);

            return new Response(
                true,
                result);
        }

        public async Task<Response> ZdshReviewAsync(
            VOTModel.ZdshReviewRequest request)
        {
            await _repository.ZdshReviewAsync(request);

            return new Response(
                true,
                null,
                "ZDSH review submitted successfully.");
        }

        public async Task<Response> MzdshReviewAsync(
            VOTModel.MzdshReviewRequest request)
        {
            await _repository.MzdshReviewAsync(request);

            return new Response(
                true,
                null,
                "MZDSH review submitted successfully.");
        }

        public async Task<Response> AuditReviewAsync(
            VOTModel.AuditReviewRequest request)
        {
            await _repository.AuditReviewAsync(request);

            return new Response(
                true,
                null,
                "Audit review submitted successfully.");
        }

        public async Task<Response> UploadFileAsync(
            VOTModel.FileUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return new Response(false, null, "File not found.");
            }

            var extension = Path.GetExtension(request.File.FileName);

            var fileName =
                $"{request.CaseId}_{request.FileType}_{DateTime.Now:yyyyMMddHHmmss}{extension}";

            var filePath = Path.Combine(
                _appSettings.FilePaths.VOTFilePath,
                fileName);

            Directory.CreateDirectory(
                _appSettings.FilePaths.VOTFilePath);

            using var stream = new FileStream(
                filePath,
                FileMode.Create);

            await request.File.CopyToAsync(stream);

            await _repository.UpdateCaseFileAsync(
                request.CaseId,
                request.FileType,
                fileName);

            return new Response(
                true,
                new VOTModel.FileUploadResponse
                {
                    FileName = fileName
                },
                "File uploaded successfully.");
        }

        public async Task<FileContentResult> DownloadFileAsync(
            VOTModel.DownloadFileRequest request)
        {
            var filePath = Path.Combine(
                _appSettings.FilePaths.VOTFilePath,
                request.FileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.");
            }

            var fileBytes = await File.ReadAllBytesAsync(filePath);

            return new FileContentResult(
                fileBytes,
                "application/octet-stream")
            {
                FileDownloadName = request.FileName
            };
        }

        public async Task<Response> GetReportAsync(
    VOTModel.ReportRequest request)
        {
            var result = await _repository.GetReportAsync(request);

            return new Response(
                true,
                result);
        }

    }
}