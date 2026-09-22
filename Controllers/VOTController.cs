using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VOTController : ControllerBase
    {
        private readonly VOTService _votService;

        public VOTController(VOTService votService)
        {
            _votService = votService;
        }

        #region Lan Mapping

        [HttpPost("uploadLans")]
        public async Task<IActionResult> UploadLans(
            [FromBody] VOTModel.BulkUploadLanRequest request)
        {
            return Ok(
                await _votService.BulkInsertLanAsync(request));
        }

        [HttpGet("getLans")]
        public async Task<IActionResult> GetLans()
        {
            return Ok(await _votService.GetLansAsync());
        }

        [HttpGet("getLansByUser")]
        public async Task<IActionResult> GetLansByUser(
    [FromQuery] VOTModel.GetLansByUserRequest request)
        {
            return Ok(await _votService.GetLansByUserAsync(request));
        }

        [HttpPost("updateLan")]
        public async Task<IActionResult> UpdateLan(
    [FromBody] VOTModel.UpdateLanRequest request)
        {
            return Ok(await _votService.UpdateLanAsync(request));
        }

        [HttpPost("updateLanStatus")]
        public async Task<IActionResult> UpdateLanStatus(
            [FromBody] VOTModel.UpdateLanStatusRequest request)
        {
            return Ok(await _votService.UpdateLanStatusAsync(request));
        }

        #endregion

        #region Case

        [HttpPost("createCase")]
        public async Task<IActionResult> CreateCase(
            [FromBody] VOTModel.CreateCaseRequest request)
        {
            return Ok(await _votService.CreateCaseAsync(request));
        }

        [HttpGet("getCaseById/{id}")]
        public async Task<IActionResult> GetCaseById(int id)
        {
            return Ok(await _votService.GetCaseByIdAsync(id));
        }


        [HttpGet("getCasesByUser")]
        public async Task<IActionResult> GetCasesByUser(
    [FromQuery] VOTModel.GetCasesByUserRequest request)
        {
            return Ok(await _votService.GetCasesByUserAsync(request));
        }

        #endregion

        #region Reviews

        [HttpPost("zdshReview")]
        public async Task<IActionResult> ZdshReview(
            [FromBody] VOTModel.ZdshReviewRequest request)
        {
            return Ok(await _votService.ZdshReviewAsync(request));
        }

        [HttpPost("mzdshReview")]
        public async Task<IActionResult> MzdshReview(
            [FromBody] VOTModel.MzdshReviewRequest request)
        {
            return Ok(await _votService.MzdshReviewAsync(request));
        }

        [HttpPost("auditReview")]
        public async Task<IActionResult> AuditReview(
            [FromBody] VOTModel.AuditReviewRequest request)
        {
            return Ok(await _votService.AuditReviewAsync(request));
        }

        #endregion

        #region File Upload/Download
        [HttpPost("uploadFile")]
        public async Task<IActionResult> UploadFile(
            [FromForm] VOTModel.FileUploadRequest request)
        {
            return Ok(await _votService.UploadFileAsync(request));
        }

        [HttpGet("downloadFile")]
        public async Task<IActionResult> DownloadFile(
            [FromQuery] VOTModel.DownloadFileRequest request)
        {
            return await _votService.DownloadFileAsync(request);
        }

        #endregion

        #region Report
        [HttpGet("getReport")]
        public async Task<IActionResult> GetReport(
            [FromQuery] VOTModel.ReportRequest request)
        {
            return Ok(await _votService.GetReportAsync(request));
        }
        #endregion
    }
}