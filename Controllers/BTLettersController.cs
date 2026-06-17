using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BTLetterController : ControllerBase
    {
        private readonly BTLettersService _service;

        public BTLetterController(BTLettersService service)
        {
            _service = service;
        }

        [HttpPost("createFormat")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateFormat([FromForm] BTFormatUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new Response(false, null, "Please upload a file."));

            var (fileBytes, error) = await _service.CreateBtFormatAsync(request.File);

            if (error != null)
                return BadRequest(new Response(false, null, error));

            return File(
                fileBytes!,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BTUploadFormat.xlsx"
            );
        }

        // ── Step 2 : Upload Filled BT File ────────────────────

        [HttpPost("uploadBTFile")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadBTFile([FromForm] BTFormatUploadRequest request, [FromQuery] string uploadedBy)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new Response(false, null, "Please upload a file."));

            var (rowsUploaded, error) = await _service.UploadFilledBtFileAsync(request.File, uploadedBy);

            return error is not null
                ? BadRequest(new Response(false, null, error))
                : Ok(new Response(true, new { RowsUploaded = rowsUploaded }, $"{rowsUploaded} rows uploaded. Proceed to generate BT Letters."));
        }

        // ── Step 3 : Get Letter Data ──────────────────────────

        [HttpGet("getLetterData")]
        public async Task<IActionResult> GetLetterData([FromQuery] string letterType)
        {
            if (!IsValidLetterType(letterType))
                return BadRequest(new Response(false, null, "Invalid letter type. Use BT-1, BT-2, or BT-3."));

            var data = await _service.GetLetterDataAsync(letterType);

            return Ok(new Response(true, data, "Success"));
        }

        // ── Step 4 : Send Letters ─────────────────────────────

        [HttpPost("sendLetters")]
        public async Task<IActionResult> SendLetters([FromBody] BTSendLetterRequest dto)
        {
            if (!IsValidLetterType(dto.LetterType))
                return BadRequest(new Response(false, null, "Invalid letter type. Use BT-1, BT-2, or BT-3."));

            var result = await _service.SendLettersAsync(dto.LetterType, dto.SentBy);

            return result.MailsSent == result.TotalRows
                ? Ok(new Response(true, result, "Mail sent successfully for all cases."))
                : StatusCode(207, new Response(false, result, $"Mail sent for {result.MailsSent}/{result.TotalRows} cases only."));
        }

        // ── Report ────────────────────────────────────────────

        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport()
        {
            var bytes = await _service.GetReportExcelAsync();

            if (bytes is null)
                return NotFound(new Response(false, null, "No data found for report."));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BT_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

        // ── Email Master ──────────────────────────────────────

        [HttpGet("getEmailMaster")]
        public async Task<IActionResult> GetEmailMaster()
        {
            var data = await _service.GetEmailMasterAsync();

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("downloadEmailMaster")]
        public async Task<IActionResult> DownloadEmailMaster()
        {
            var bytes = await _service.DownloadEmailMasterAsync();

            if (bytes is null)
                return NotFound(new Response(false, null, "No data found."));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BT_EmailMaster_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

        [HttpPut("updateEmailMaster")]
        public async Task<IActionResult> UpdateEmailMaster([FromBody] BTUpdateEmailMasterRequest request)
        {
            var (result, error) = await _service.UpdateEmailMasterAsync(request);

            return result > 0
                ? Ok(new Response(true, null, "Updated successfully."))
                : BadRequest(new Response(false, null, error ?? "Update failed."));
        }

        [HttpPost("uploadEmailMaster")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadEmailMaster([FromForm] BTFormatUploadRequest request, [FromQuery] string uploadedBy)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new Response(false, null, "Please upload a file."));

            var (rowsUploaded, error) = await _service.uploadEmailMasterAsync(request.File, uploadedBy);

            return error is not null
                ? BadRequest(new Response(false, null, error))
                : Ok(new Response(true, new { RowsUploaded = rowsUploaded }, $"{rowsUploaded} rows uploaded successfully."));
        }

        // ── Private helpers ───────────────────────────────────

        private static bool IsValidLetterType(string lt) =>
            lt is "BT-1" or "BT-2" or "BT-3";
    }
}