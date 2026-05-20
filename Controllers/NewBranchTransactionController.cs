using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewBranchTransactionController : Controller
    {
        private readonly NewBranchTransactionServices _services;

        public NewBranchTransactionController(
            NewBranchTransactionServices services)
        {
            _services = services;
        }

        #region GET BRANCH TRANSACTIONS

        // GET : api/NewBranchTransaction/5
        [HttpGet("{tid}")]
        public IActionResult GetBranchTransactions(int tid)
        {
            try
            {
                var transactions = _services.GetBranchTransactions(tid);

                if (transactions == null || !transactions.Any())
                    return Ok(new Response(false, null, "No records found"));

                return Ok(new Response(true, transactions, "Success"));
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


        #region GET BRANCH DETAILS

        // GET : api/NewBranchTransaction/details/5
        [HttpGet("details/{tid}")]
        public IActionResult GetBranchDetails(int tid)
        {
            try
            {
                var response = _services.GetBranchDetails(tid);

                if (response == null)
                    return Ok(new Response(false, null, "Branch not found"));

                return Ok(new Response(true, response, "Success"));
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


        #region UPDATE TRANSACTIONS

        // PUT : api/NewBranchTransaction/5
        [HttpPut("{tid}")]
        public IActionResult UpdateTransactions(
            int tid,
            UpdateBranchTransactionsRequestDto model)
        {
            try
            {
                if (tid != model.Tid)
                {
                    return BadRequest(
                        new Response(false, null, "Invalid branch id")
                    );
                }

                var response = _services.UpdateTransactions(model);

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
                    new Response(false, null,
                    $"Internal server error : {ex.Message}")
                );
            }
        }

        #endregion
    }
}