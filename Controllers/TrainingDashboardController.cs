using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrainingDashboardController : ControllerBase
    {
        private readonly TrainingDashboardService _service;

        public TrainingDashboardController(
            TrainingDashboardService service)
        {
            _service = service;
        }

        // =========================================
        // GET TRAINING NAMES
        // =========================================

        [HttpGet("trainings")]
        public IActionResult GetTrainingNames()
        {
            try
            {
                var data = _service.GetTrainingNames();

                return Ok(new Response(
                    success: true,
                    data: data,
                    message: "Success"
                ));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(
                    success: false,
                    message: ex.Message
                ));
            }
        }

        // =========================================
        // GET LOCATIONS
        // =========================================

        [HttpGet("locations")]
        public IActionResult GetLocations()
        {
            try
            {
                var data = _service.GetLocations();

                return Ok(new Response(
                    success: true,
                    data: data,
                    message: "Success"
                ));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(
                    success: false,
                    message: ex.Message
                ));
            }
        }

        // =========================================
        // GET EMPLOYEE LIST
        // =========================================

        [HttpGet("employees/{userId}")]
        public IActionResult GetEmployeeList(string userId)
        {
            try
            {
                var data = _service.GetEmployeeList(userId);

                return Ok(new Response(
                    success: true,
                    data: data,
                    message: "Success"
                ));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(
                    success: false,
                    message: ex.Message
                ));
            }
        }

        // =========================================
        // ASSIGN SINGLE TRAINING
        // =========================================

        [HttpPost("employees/assign")]
        public IActionResult AssignTraining(
            [FromBody] AssignTrainingRequestModel model)
        {
            try
            {
                int result = _service.AssignTraining(model);

                if (result == 1)
                {
                    return Ok(new Response(
                        success: true,
                        message: "Training assigned successfully"
                    ));
                }

                return BadRequest(new Response(
                    success: false,
                    message: "Failed to assign training"
                ));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(
                    success: false,
                    message: ex.Message
                ));
            }
        }

        // =========================================
        // BULK ASSIGN
        // =========================================

        [HttpPost("employees/bulk-assign")]
        public IActionResult BulkAssignTraining(
            [FromBody] BulkAssignTrainingModel model)
        {
            try
            {
                var result = _service.BulkAssignTraining(model);

                return Ok(new Response(
                    success: true,
                    data: result,
                    message: "Bulk training assignment completed"
                ));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(
                    success: false,
                    message: ex.Message
                ));
            }
        }

        // =========================================
        // DISABLE EMPLOYEE
        // =========================================

        [HttpPost("employees/disable")]
        public IActionResult DisableEmployee([FromBody] DisableEmployeeDto dto)
        {
            try
            {
                bool result = _service.DisableEmployee(dto);

                if (result)
                {
                    return Ok(new Response(
                        success: true,
                        message: "Employee disabled successfully"
                    ));
                }

                return BadRequest(new Response(
                    success: false,
                    message: "Failed to disable employee training"
                ));
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(
                    success: false,
                    message: ex.Message
                ));
            }
        }

        // =========================================
        // DOWNLOAD REPORT
        // =========================================

        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport()
        {
            try
            {
                var excelBytes = await _service.DownloadReportExcelAsync();

                if (excelBytes == null)
                    return NoContent();

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"TrainingReport_{DateTime.Now:ddMMyyyy}.xlsx");
            }
            catch (Exception ex)
            {
                return BadRequest(new Response(
                    success: false,
                    message: ex.Message));
            }
        }
    }
}