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
        private readonly BTService _service;

        public BTLetterController(BTService service)
        {
            _service = service;
        }

        // ── Step 1 : Prepare BT Format ────────────────────────

        [HttpPost("uploadFormat")]
        public async Task<IActionResult> UploadFormat([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new Response(false, null, "Please upload a file."));

            var (rowsMatched, error) = await _service.UploadBtFormatAsync(file);

            return error is not null
                ? BadRequest(new Response(false, null, error))
                : Ok(new Response(true, new { RowsMatched = rowsMatched }, $"{rowsMatched} rows matched. Proceed to download format."));
        }

        [HttpGet("downloadFormat")]
        public async Task<IActionResult> DownloadFormat()
        {
            var bytes = await _service.DownloadBtFormatAsync();

            if (bytes is null)
                return NotFound(new Response(false, null, "No data found to generate format."));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BTUploadFormat.xlsx"
            );
        }

        // ── Step 2 : Upload Filled BT File ────────────────────

        [HttpPost("uploadBTFile")]
        public async Task<IActionResult> UploadBTFile([FromForm] IFormFile file, [FromQuery] string uploadedBy)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new Response(false, null, "Please upload a file."));

            var (rowsUploaded, error) = await _service.UploadFilledBtFileAsync(file, uploadedBy);

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

        [HttpGet("emailMaster")]
        public async Task<IActionResult> GetEmailMaster()
        {
            var data = await _service.GetAllEmailMasterAsync();

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("uploadEmailMaster")]
        public async Task<IActionResult> UploadEmailMaster([FromForm] IFormFile file, [FromQuery] int uploadedBy)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new Response(false, null, "Please upload a file."));

            var (rowsUploaded, error) = await _service.UploadEmailMasterAsync(file, uploadedBy);

            return error is not null
                ? BadRequest(new Response(false, null, error))
                : Ok(new Response(true, new { RowsUploaded = rowsUploaded }, $"{rowsUploaded} rows uploaded successfully."));
        }

        // ── Private helpers ───────────────────────────────────

        private static bool IsValidLetterType(string lt) =>
            lt is "BT-1" or "BT-2" or "BT-3";
    }
}