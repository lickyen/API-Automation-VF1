using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;
using System;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DCController : Controller
    {

        private readonly DCServices _dCServices;

        public DCController(DCServices dCServices)
        {
            _dCServices = dCServices;
        }


        [HttpPost("AddCase")]
        public IActionResult AddCase([FromBody] AddCaseDto addCaseDto)
        {
            try
            {
                var result = _dCServices.AddCase(addCaseDto);

                if (result.Contains("Success"))
                    return Ok(new Response(true, null, result));
                return Ok(new Response(false, null, result));

               
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, $"Internal server error: {ex.Message}"));
            }
        }

        [HttpPost("AddEmployeestoCase")]
        public IActionResult AddEmployeestoCase([FromBody] AddEmployeesToCaseDto addEmployeesToCaseDto)
        {
            try
            {
                var result = _dCServices.AddCaseEmployees(addEmployeesToCaseDto);

                if (result.Contains("Employees added successfully"))
                    return Ok(new Response(true, null, result));
                return Ok(new Response(false, null, result));


            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, $"Internal server error: {ex.Message}"));
            }
        }

        [HttpGet("GetAllCases")]
        public IActionResult GetAllCases()
        {
            try
            {
                var users = _dCServices.GetAllCases();

                if (users == null || !users.Any())
                    return Ok(new Response(false, null, "No Cases found."));

                return Ok(new Response(true, users, "Cases retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, $"Internal server error: {ex.Message}"));
            }
        }

        //[HttpPost("GetCasesByEmpid")]
        //public IActionResult GetCasesByEmpid([FromBody] CaseRequest caseRequest)
        //{
        //    try
        //    {
        //        var users = _dCServices.GetCasesByEmpid(caseRequest);

        //        if (users == null || !users.Any())
        //            return Ok(new Response(false, null, "No cases found."));

        //        return Ok(new Response(true, users, "Cases retrieved successfully."));
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new Response(false, null, $"Internal server error: {ex.Message}"));
        //    }
        //}

        [HttpGet("{caseId}")]
        public IActionResult GetCase(int caseId)
        {
            try
            {
                var casedtls = _dCServices.GetCase(caseId);

                if (casedtls == null)
                    return Ok(new Response(false, null, $"Case not found"));

                return Ok(new Response(true, casedtls, "Success"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, $"Internal server error: {ex.Message}"));
            }
        }

        [HttpPost("CaseUpdateByHR")]
        public async Task<IActionResult> CaseUpdateByHR([FromBody] HRUpdateDto dto)
        {
            var result = await _dCServices.UpdateHRResponseAsync(dto);

            return result switch
            {
                > 0 => Ok(new Response(true, null, "Success")),
                0 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred while processing the request"))
            };
        }

        [HttpPost("UpdateCase")]
        public IActionResult UpdateCase([FromBody] UpdateCaseDto dto)
        {
            try
            {
                var result = _dCServices.UpdateCase(dto);

                if (result.Contains("Success"))
                    return Ok(new Response(true, null, result));
                return Ok(new Response(false, null, result));


            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, $"Internal server error: {ex.Message}"));
            }
        }

        [HttpPost("CaseUpdateEmployee")]
        public IActionResult CaseUpdateEmployee([FromBody] EmployeeResponseDto dto)
        {
            try
            {
                var result = _dCServices.UpdateEmployeeResponse(dto);

                if (result.Contains("Success"))
                    return Ok(new Response(true, null, result));
                return Ok(new Response(false, null, result));


            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, $"Internal server error: {ex.Message}"));
            }
        }


    }
}
