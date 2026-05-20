using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewBranchChecklistMasterController : Controller
    {
        private readonly NewBranchChecklistMasterServices _services;

        public NewBranchChecklistMasterController(
            NewBranchChecklistMasterServices services)
        {
            _services = services;
        }

        #region GET CHECKLIST MASTER

        // GET : api/NewBranchChecklistMaster
        [HttpGet]
        public IActionResult GetChecklistMaster()
        {
            try
            {
                var checklists = _services.GetChecklistMaster();

                if (checklists == null || !checklists.Any())
                    return Ok(new Response(false, null, "No records found"));

                return Ok(new Response(true, checklists, "Success"));
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