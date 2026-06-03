using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeathClaimController : ControllerBase
    {
        private readonly DeathClaimService _service;

        public DeathClaimController(DeathClaimService service)
        {
            _service = service;
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
                return NotFound(new Response(false, null, "Record not found"));
            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getByUser")]
        public async Task<IActionResult> GetByUser([FromQuery] GetByUserRequestDto dto)
        {
            var data = await _service.GetByUserAsync(dto);
            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("intimation")]
        public async Task<IActionResult> Intimation([FromBody] IntimationRequestDto dto)
        {
            var result = await _service.IntimationAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, new { id = result }, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "An active request already exists for the same LAN")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("updateIntimation")]
        public async Task<IActionResult> UpdateIntimation([FromBody] UpdateIntimationRequestDto dto)
        {
            var result = await _service.UpdateIntimationAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("submitDocs")]
        public async Task<IActionResult> SubmitDocs([FromBody] SubmitDocsRequestDto dto)
        {
            var result = await _service.SubmitDocsAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("submitClaimForm")]
        public async Task<IActionResult> SubmitBranch2([FromBody] SubmitBranch2RequestDto dto)
        {
            var result = await _service.SubmitBranch2Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("submitAdditionalDocs")]
        public async Task<IActionResult> SubmitBranch3([FromBody] SubmitBranch3RequestDto dto)
        {
            var result = await _service.SubmitBranch3Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("deficitCollection")]
        public async Task<IActionResult> SubmitBranch5([FromBody] SubmitBranch5RequestDto dto)
        {
            var result = await _service.SubmitBranch5Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("copsVerification")]
        public async Task<IActionResult> SubmitCops1([FromBody] SubmitCops1RequestDto dto)
        {
            var result = await _service.SubmitCops1Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("updateClaimInitiated")]
        public async Task<IActionResult> SubmitCops2([FromBody] SubmitCops2RequestDto dto)
        {
            var result = await _service.SubmitCops2Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("verifyAdditionalDocs")]
        public async Task<IActionResult> SubmitCops3([FromBody] SubmitCops3RequestDto dto)
        {
            var result = await _service.SubmitCops3Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("updateClaimStatus")]
        public async Task<IActionResult> SubmitCops4([FromBody] SubmitCops4RequestDto dto)
        {
            var result = await _service.SubmitCops4Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPost("finalClosure")]
        public async Task<IActionResult> SubmitCops5([FromBody] SubmitCops5RequestDto dto)
        {
            var result = await _service.SubmitCops5Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpGet("getClaimForm/{id}")]
        public async Task<IActionResult> GetClaimForm(int id)
        {
            var (bytes, lan) = await _service.GetClaimFormAsync(id);

            if (bytes is null)
                return NotFound(new Response(false, null, "No record found for the given Id"));

            return File(
                bytes,
                "application/pdf",
                $"{lan}_KotakClaimForm.pdf"
            );
        }

        [HttpGet("getDischargeForm/{id}")]
        public async Task<IActionResult> GetDischargeForm(int id)
        {
            var (bytes, lan) = await _service.GetDischargeFormAsync(id);

            if (bytes is null)
                return NotFound(new Response(false, null, "No record found for the given Id"));

            return File(
                bytes,
                "application/pdf",
                $"{lan}_DischargeForm.pdf"
            );
        }

        [HttpGet("getClaimFormat/{id}")]
        public async Task<IActionResult> GetClaimFormat(int id)
        {
            var (bytes, lan) = await _service.GetClaimFormatAsync(id);

            if (bytes is null)
                return NotFound(new Response(false, null, "No record found for the given Id"));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{lan}_ClaimFormat.xlsx"
            );
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDoc([FromForm] UploadFileRequestDto dto)
        {
            var result = await _service.UploadFileAsync(dto);
            return result.Success
                ? Ok(new Response(true, result.FilePath, "File uploaded successfully"))
                : BadRequest(new Response(false, null, result.Error));
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile([FromQuery] string fileName)
        {
            var fileBytes = await _service.DownloadFileAsync(fileName);

            if (fileBytes == null)
                return NotFound("File not found");

            var contentType = FileHelper.GetContentType(fileName);

            return File(fileBytes, contentType, fileName);
        }

        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport()
        {
            var bytes = await _service.GetReportExcelAsync();

            if (bytes is null)
                return NotFound(new Response(false, null, "No data found for report"));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DeathClaim_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }
    }
}