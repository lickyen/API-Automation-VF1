using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewBranchController : Controller
    {
        private readonly NewBranchServices _services;

        public NewBranchController(NewBranchServices services)
        {
            _services = services;
        }

        #region ADD NEW BRANCH

        // POST : api/NewBranch
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
                    new Response(false, null, $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion

        // Controller

        // Controller

        #region GET ALL BRANCHES

        // GET : api/NewBranch/all
        [HttpGet("all")]
        public IActionResult GetAllBranches(string? role, string? userId)
        {
            try
            {
                var branches = _services.GetAllBranches(role, userId);

                if (branches == null || !branches.Any())
                {
                    return Ok(new Response(false, null, "No branches found"));
                }

                return Ok(new Response(true, branches, "Success"));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(false, null, $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion


        #region GET BRANCH LIST

        // GET : api/NewBranch/byRole?status=Pending&role=IT
        [HttpGet("byRole")]
        public IActionResult GetBranches([FromQuery] string? status, [FromQuery] string? role, [FromQuery] string? userId)
        {
            try
            {
                GetNewBranchListRequestDto model = new GetNewBranchListRequestDto
                {
                    Status = status,
                    Role = role,
                    UserId = userId
                };

                var branches = _services.GetBranches(model);

                if (branches == null || !branches.Any())
                {
                    return Ok(new Response(false, null, "No records found"));
                }

                return Ok(new Response(true, branches, "Success"));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(false, null, $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion

        #region GET BRANCH DETAILS

        [HttpGet("branchDetails/{tid}")]
        public IActionResult GetBranchDetails(int tid)
        {
            try
            {
                var response = _services.GetBranchDetails(tid);

                if (response == null)
                {
                    return Ok(new Response(false, null, "Branch not found"));
                }

                return Ok(new Response(true, response, "Success"));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(false, null, $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion

        #region UPDATE TRANSACTIONS

        // PUT : api/NewBranch/transactions/5
        [HttpPut("transactions/{tid}")]
        public IActionResult UpdateTransactions(int tid, UpdateBranchTransactionsRequestDto model)
        {
            try
            {
                if (tid != model.Tid)
                {
                    return BadRequest(new Response(false, null, "Invalid branch id"));
                }

                var response = _services.UpdateTransactions(model);

                if (!response.IsSuccess)
                {
                    return Ok(new Response(false, null, response.Message));
                }

                return Ok(new Response(true, null, response.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(false, null, $"Internal server error : {ex.Message}")
                );
            }
        }
        #endregion

        [HttpGet("export/new-branch-report")]
        public async Task<IActionResult> ExportNewBranchReport()
        {
            var file = await _services.GetNewBranchReportAsync();

            if (file == null)
            {
                return NotFound(new Response(false, null, "No data found"));
            }

            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"NewBranch_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }
        #region UPDATE BRANCH CODE

        // PUT : api/NewBranch/branchCode/5

        [HttpPut("branchCode/{id}")]
        public IActionResult UpdateBranchCode(
            int id,
            [FromBody] UpdateBranchCodeRequestDto model
        )
        {
            try
            {
                if (id != model.Id)
                {
                    return BadRequest(
                        new Response(false, null, "Invalid branch id")
                    );
                }

                var response = _services.UpdateBranchCode(model);

                if (!response.IsSuccess)
                {
                    return Ok(
                        new Response(false, null, response.Message)
                    );
                }

                return Ok(
                    new Response(true, null, response.Message)
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(
                        false,
                        null,
                        $"Internal server error : {ex.Message}"
                    )
                );
            }
        }

        #endregion
        #region ADD BRANCH USER

        // POST: api/NewBranch/branch-user
        [HttpPost("addBranchUser")]
        public IActionResult AddBranchUser(
            AddBranchUserRequestDto model
        )
        {
            try
            {
                var response = _services.AddBranchUser(model);

                return Ok(
                    new Response(
                        true,
                        response,
                        response.Message
                    )
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(
                        false,
                        null,
                        $"Internal server error : {ex.Message}"
                    )
                );
            }
        }

        #endregion

        #region GET BRANCH USERS

        // GET : api/NewBranch/branchUsers/5
        [HttpGet("branchUsers/{bid}")]
        public IActionResult GetBranchUsers(int bid)
        {
            try
            {
                var users = _services.GetBranchUsersByBid(bid);

                if (users == null || !users.Any())
                {
                    return Ok(
                        new Response(false, null, "No users found for this branch")
                    );
                }

                return Ok(
                    new Response(true, users, "Success")
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(
                        false,
                        null,
                        $"Internal server error : {ex.Message}"
                    )
                );
            }
        }

        #endregion
    }
}