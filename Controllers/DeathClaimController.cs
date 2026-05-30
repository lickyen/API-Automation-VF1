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

        [HttpPost("intimation")]
        public async Task<IActionResult> Intimation([FromBody] IntimationRequestDto dto)
        {
            var result = await _service.IntimationAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, new { id = result }, "Claim intimation submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("submitDocs")]
        public async Task<IActionResult> UploadDocs([FromBody] UploadDocsRequestDto dto)
        {
            var result = await _service.UploadDocsAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Documents uploaded successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("submitClaimForm")]
        public async Task<IActionResult> SubmitBranch2([FromBody] SubmitBranch2RequestDto dto)
        {
            var result = await _service.SubmitBranch2Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Branch stage 2 submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("submitAdditionalReq")]
        public async Task<IActionResult> SubmitBranch3([FromBody] SubmitBranch3RequestDto dto)
        {
            var result = await _service.SubmitBranch3Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Additional documents submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("updateCollection")]
        public async Task<IActionResult> SubmitBranch5([FromBody] SubmitBranch5RequestDto dto)
        {
            var result = await _service.SubmitBranch5Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Branch stage 5 submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("copsVerification")]
        public async Task<IActionResult> SubmitCops1([FromBody] SubmitCops1RequestDto dto)
        {
            var result = await _service.SubmitCops1Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "COPS stage 1 submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("updateClaimInitiated")]
        public async Task<IActionResult> SubmitCops2([FromBody] SubmitCops2RequestDto dto)
        {
            var result = await _service.SubmitCops2Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "COPS stage 2 submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        //[HttpPut("saveClaimInitiated")]
        //public async Task<IActionResult> SaveCops2([FromBody] SaveCops2RequestDto dto)
        //{
        //    var result = await _service.SaveCops2Async(dto);
        //    return result switch
        //    {
        //        > 0 => Ok(new Response(true, null, "COPS stage 2 saved successfully")),
        //        -1 => NotFound(new Response(false, null, "Record not found")),
        //        _ => StatusCode(500, new Response(false, null, "An error occurred"))
        //    };
        //}

        [HttpPut("additionalRequestSubmit")]
        public async Task<IActionResult> SubmitCops3([FromBody] SubmitCops3RequestDto dto)
        {
            var result = await _service.SubmitCops3Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "COPS stage 3 submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("updateClaimStatus")]
        public async Task<IActionResult> SubmitCops4([FromBody] SubmitCops4RequestDto dto)
        {
            var result = await _service.SubmitCops4Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "COPS stage 4 submitted successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        [HttpPut("finalClosure")]
        public async Task<IActionResult> SubmitCops5([FromBody] SubmitCops5RequestDto dto)
        {
            var result = await _service.SubmitCops5Async(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "COPS stage 5 submitted successfully")),
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

        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport([FromQuery] string month)
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