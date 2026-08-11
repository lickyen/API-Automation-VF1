using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SARFAESIController : ControllerBase
    {
        private readonly SARFAESIService _service;

        public SARFAESIController(SARFAESIService service)
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
        public async Task<IActionResult> GetByUser([FromQuery] string userId, [FromQuery] string? status, [FromQuery] string? role)
        {
            var data = await _service.GetByUserAsync(userId, status, role);

            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] SARFAESICreateDto dto)
        {
            var result = await _service.CreateAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, result, "Created successfully")),
                -1 => Conflict(new Response(false, null, "Record already exists")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SARFAESICreateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Updated successfully")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "Error occurred"))
            };
        }

        //[HttpPost("uploadFile")]
        //public async Task<IActionResult> UploadFile([FromForm] SARFAESIFileUploadDto dto)
        //{
        //    var result = await _service.UploadFileAsync(dto); 
        //    return Ok(new Response(true, null, "File uploaded successfully"));
        //}

        [HttpPost("uploadFile")]
        public async Task<IActionResult> UploadFile([FromForm] SARFAESIFileUploadDto dto)
        {
            var (success, message) = await _service.UploadFileAsync(dto);

            if (!success)
                return BadRequest(new Response(false, null, message));

            return Ok(new Response(true, null, message));
        }

        [HttpGet("downloadFile")]
        public async Task<IActionResult> DownloadFile([FromQuery] string fileName)
        {
            var fileBytes = await _service.DownloadFileAsync(fileName);

            if (fileBytes == null)
                return NotFound("File not found");

            var contentType = FileHelper.GetContentType(fileName);

            return File(fileBytes, contentType, fileName);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] SARFAESISubmitDto dto)
        {
            await _service.SubmitAsync(dto);
            return Ok(new Response(true, null, "Submitted successfully"));
        }

        [HttpPost("report/excel")]
        public async Task<IActionResult> GetReportExcel([FromBody] SARFAESIReportFilterDto dto)
        {
            var bytes = await _service.GetReportExcelAsync(dto);

            if (bytes is null)
                return NotFound(new Response(false, null, "No data found for report"));

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SARFAESI_14(3)_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }

        [HttpPost("report/data")]
        public async Task<IActionResult> GetReportData([FromBody] SARFAESIReportFilterDto dto)
        {
            var data = await _service.GetReportDataAsync(dto);

            if (data == null || !data.Any())
                return NotFound(new Response(false, null, "No data found"));

            return Ok(data);
        }

        //[HttpPost("uploadLanMapping")]
        //public async Task<IActionResult> UploadLanMapping([FromBody] SARFAESILanUploadDto dto)
        //{
        //    var result = await _service.UploadLanMappingAsync(dto);

        //    if (!result.Success)
        //        return BadRequest(new Response(false, null, result.Message));

        //    return Ok(new Response(true, null, result.Message));
        //}

        //[HttpGet("getMappedLans")]
        //public async Task<IActionResult> GetLansByEmpId([FromQuery] string empId)
        //{
        //    var data = await _service.GetLansByEmpIdAsync(empId);

        //    return Ok(new Response(true, data, "Success"));
        //}

        //[HttpGet("getAllMappedLans")]
        //public async Task<IActionResult> GetAllMappedLans()
        //{
        //    var data = await _service.GetAllMappedLansAsync();

        //    return Ok(new Response(true, data, "Success"));
        //}

        //[HttpDelete("deleteMappedLan/{id}")]
        //public async Task<IActionResult> DeleteMappedLan(int id)
        //{
        //    var deleted = await _service.DeleteMappedLanAsync(id);

        //    if (!deleted)
        //        return NotFound(new Response(false, null, "Record not found."));

        //    return Ok(new Response(true, null, "Record deleted successfully."));
        //}

        //[HttpPost("updateMappedLan")]
        //public async Task<IActionResult> UpdateMappedLan([FromBody] SARFAESIUpdateMappedLanRequest request)
        //{
        //    var updated = await _service.UpdateMappedLanAsync(request);

        //    if (!updated)
        //        return NotFound(new Response(false, null, "Record not found."));

        //    return Ok(new Response(true, null, "Record updated successfully."));
        //}

        [HttpPost("uploadLanMapping")]
        public async Task<IActionResult> UploadLanMapping([FromBody] SARFAESILanUploadDto dto)
        {
            var result = await _service.UploadLanMappingAsync(dto);

            if (!result.Success)
                return BadRequest(new Response(false, null, result.Message));

            return Ok(new Response(true, null, result.Message));
        }

        [HttpGet("getMappedLans")]
        public async Task<IActionResult> GetLansByEmpId([FromQuery] string empId, [FromQuery] string? sectionCode)
        {
            var data = await _service.GetLansByEmpIdAsync(empId, sectionCode);

            return Ok(new Response(true, data, "Success"));
        }

        [HttpGet("getAllMappedLans")]
        public async Task<IActionResult> GetAllMappedLans([FromQuery] string? sectionCode)
        {
            var data = await _service.GetAllMappedLansAsync(sectionCode);

            return Ok(new Response(true, data, "Success"));
        }

        [HttpDelete("deleteMappedLan/{id}")]
        public async Task<IActionResult> DeleteMappedLan(int id)
        {
            var deleted = await _service.DeleteMappedLanAsync(id);

            if (!deleted)
                return NotFound(new Response(false, null, "Record not found."));

            return Ok(new Response(true, null, "Record deleted successfully."));
        }

        [HttpPost("updateMappedLan")]
        public async Task<IActionResult> UpdateMappedLan([FromBody] SARFAESIUpdateMappedLanRequest request)
        {
            var result = await _service.UpdateMappedLanAsync(request);

            if (!result.Success)
                return BadRequest(new Response(false, null, result.Message));

            return Ok(new Response(true, null, result.Message));
        }

    }
}