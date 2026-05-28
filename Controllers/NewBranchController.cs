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

        public NewBranchController(
            NewBranchServices services)
        {
            _services = services;
        }

        #region ADD NEW BRANCH

        // POST : api/NewBranch
        [HttpPost]
        public IActionResult AddNewBranch(
            AddNewBranchRequestDto model)
        {
            try
            {
                var response =
                    _services.AddNewBranch(model);

                return Ok(
                    new Response(
                        true,
                        response,
                        response.Message)
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(
                        false,
                        null,
                        $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion


        #region GET BRANCH LIST

        // GET : api/NewBranch/byRole?status=Pending&role=IT
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

                var branches =
                    _services.GetBranches(model);

                if (branches == null || !branches.Any())
                {
                    return Ok(
                        new Response(
                            false,
                            null,
                            "No records found")
                    );
                }

                return Ok(
                    new Response(
                        true,
                        branches,
                        "Success")
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(
                        false,
                        null,
                        $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion


        #region GET BRANCH BY ID

        // GET : api/NewBranch/5
        [HttpGet("{id}")]
        public IActionResult GetBranchById(int id)
        {
            try
            {
                var branch =
                    _services.GetBranchById(id);

                if (branch == null)
                {
                    return Ok(
                        new Response(
                            false,
                            null,
                            "Branch not found")
                    );
                }

                return Ok(
                    new Response(
                        true,
                        branch,
                        "Success")
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(
                        false,
                        null,
                        $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion


        #region GET BRANCH DETAILS

        // GET : api/NewBranch/details/5
        [HttpGet("details/{tid}")]
        public IActionResult GetBranchDetails(int tid)
        {
            try
            {
                var response =
                    _services.GetBranchDetails(tid);

                if (response == null)
                {
                    return Ok(
                        new Response(
                            false,
                            null,
                            "Branch not found")
                    );
                }

                return Ok(
                    new Response(
                        true,
                        response,
                        "Success")
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(
                        false,
                        null,
                        $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion


        #region GET BRANCH TRANSACTIONS

        // GET : api/NewBranch/transactions/5
        [HttpGet("transactions/{tid}")]
        public IActionResult GetBranchTransactions(int tid)
        {
            try
            {
                var transactions =
                    _services.GetBranchTransactions(tid);

                if (transactions == null || !transactions.Any())
                {
                    return Ok(
                        new Response(
                            false,
                            null,
                            "No transactions found")
                    );
                }

                return Ok(
                    new Response(
                        true,
                        transactions,
                        "Success")
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(
                        false,
                        null,
                        $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion


        #region UPDATE TRANSACTIONS

        // PUT : api/NewBranch/transactions/5
        [HttpPut("transactions/{tid}")]
        public IActionResult UpdateTransactions(
            int tid,
            UpdateBranchTransactionsRequestDto model)
        {
            try
            {
                if (tid != model.Tid)
                {
                    return BadRequest(
                        new Response(
                            false,
                            null,
                            "Invalid branch id")
                    );
                }

                var response =
                    _services.UpdateTransactions(model);

                if (!response.IsSuccess)
                {
                    return Ok(
                        new Response(
                            false,
                            null,
                            response.Message)
                    );
                }

                return Ok(
                    new Response(
                        true,
                        null,
                        response.Message)
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new Response(
                        false,
                        null,
                        $"Internal server error : {ex.Message}")
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
    }
}