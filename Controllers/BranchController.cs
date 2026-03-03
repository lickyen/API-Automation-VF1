using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Services;
using ProductivityApi.Models;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchController : Controller
    {
        private readonly BranchServices _branchServices;

        public BranchController(BranchServices branchServices)
        {
            _branchServices = branchServices;
        }

        [HttpGet]
        public IActionResult GetAllBranches()
        {
            try
            {
                var branches = _branchServices.GetBranches();

                if (branches == null || !branches.Any())
                    return Ok(new Response(false, null, "No records found"));

                return Ok(new Response(true, branches, "Success"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, $"Internal server error: {ex.Message}"));
            }
        }

        [HttpGet("{BranchCode}")]
        public IActionResult GetBranch(string BranchCode)
        {
            try
            {
                var branch = _branchServices.GetBranchByID(BranchCode);

                if (branch == null)
                    return Ok(new Response(false, null, $"Branch not found"));

                return Ok(new Response(true, branch, "Success"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, $"Internal server error: {ex.Message}"));
            }
        }

    }
}
