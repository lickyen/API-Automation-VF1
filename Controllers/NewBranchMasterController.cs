using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewBranchMasterController : Controller
    {
        private readonly NewBranchMasterServices _services;

        public NewBranchMasterController(
            NewBranchMasterServices services)
        {
            _services = services;
        }

        #region ADD NEW BRANCH

        // POST : api/NewBranchMaster
        [HttpPost]
        public IActionResult AddNewBranch(AddNewBranchRequestDto model)
        {
            try
            {
                var response = _services.AddNewBranch(model);

                return Ok(new Response(true, response, response.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(false, null,
                    $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion


        #region GET BRANCH LIST

        // GET : api/NewBranchMaster?status=Pending&role=IT
        [HttpGet("byRole")]
        public IActionResult GetBranches(
            [FromQuery] string? status,
            [FromQuery] string? role)
        {
            try
            {
                GetNewBranchListRequestDto model =
                    new GetNewBranchListRequestDto
                    {
                        Status = status,
                        Role = role
                    };

                var branches = _services.GetBranches(model);

                if (branches == null || !branches.Any())
                    return Ok(new Response(false, null, "No records found"));

                return Ok(new Response(true, branches, "Success"));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(false, null,
                    $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion


        #region GET BRANCH BY ID

        // GET : api/NewBranchMaster/5
        [HttpGet("{id}")]
        public IActionResult GetBranchById(int id)
        {
            try
            {
                var branch = _services.GetBranchById(id);

                if (branch == null)
                    return Ok(new Response(false, null, "Branch not found"));

                return Ok(new Response(true, branch, "Success"));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(false, null,
                    $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion
    }
}