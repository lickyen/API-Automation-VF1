// Controllers/LcRefundController.cs
using Microsoft.AspNetCore.Mvc;
using ProductivityApi.Models;
using ProductivityApi.Services;

namespace ProductivityApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LcRefundController : ControllerBase
    {
        private readonly LcRefundService _service;

        public LcRefundController(LcRefundService service)
        {
            _service = service;
        }

        // ────────────────────────────────────────────────────────────────────────
        // QUERIES
        // ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// GET api/LcRefund/freshCases
        /// Returns all active (non-NotCovered, non-Completed) cancelled-loan cases.
        /// Mirrors SP_LC_Freshcases.
        /// </summary>
        [HttpGet("freshCases")]
        public async Task<IActionResult> GetFreshCases()
        {
            var data = await _service.GetFreshCasesAsync();

            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        /// <summary>
        /// GET api/LcRefund/getByStatus?status=In Progress
        /// </summary>
        [HttpGet("getByStatus")]
        public async Task<IActionResult> GetByStatus([FromQuery] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new Response(false, null, "Status is required"));

            var data = await _service.GetByStatusAsync(status);

            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No records found"));

            return Ok(new Response(true, data, "Success"));
        }

        /// <summary>
        /// GET api/LcRefund/getById/5
        /// </summary>
        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new Response(false, null, "Invalid ID"));

            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new Response(true, null, "No record found"));

            return Ok(new Response(true, data, "Success"));
        }

        /// <summary>
        /// GET api/LcRefund/getByLan/LAN123
        /// </summary>
        [HttpGet("getByLan/{lan}")]
        public async Task<IActionResult> GetByLan(string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
                return BadRequest(new Response(false, null, "LAN is required"));

            var data = await _service.GetByLanAsync(lan);

            if (data == null)
                return NotFound(new Response(true, null, "No record found"));

            return Ok(new Response(true, data, "Success"));
        }

        /// <summary>
        /// GET api/LcRefund/getUtr/LAN123
        /// Returns all UTR entries (possibly multiple per insurer) for a given LAN.
        /// </summary>
        [HttpGet("getUtr/{lan}")]
        public async Task<IActionResult> GetUtrByLan(string lan)
        {
            if (string.IsNullOrWhiteSpace(lan))
                return BadRequest(new Response(false, null, "LAN is required"));

            var data = await _service.GetUtrByLanAsync(lan);

            if (!data.Any())
                return Ok(new Response(true, new List<object>(), "No UTR records found"));

            return Ok(new Response(true, data, "Success"));
        }

        // ────────────────────────────────────────────────────────────────────────
        // ACTIONS
        // ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// POST api/LcRefund/triggerMail
        /// Send mail to one insurer for the selected LANs → status moves to "In Progress".
        ///
        /// Body: { ids: [1,2,3], insurer: "Kotak" | "ICICI" | "NivaBupa", updatedBy: "user" }
        /// </summary>
        [HttpPost("triggerMail")]
        public async Task<IActionResult> TriggerMail([FromBody] LcRefundTriggerMailDto dto)
        {
            if (dto == null || dto.Ids == null || dto.Ids.Count == 0)
                return BadRequest(new Response(false, null, "Select at least one LAN"));

            if (string.IsNullOrWhiteSpace(dto.Insurer))
                return BadRequest(new Response(false, null, "Insurer is required"));

            try
            {
                var updatedCount = await _service.TriggerMailAsync(dto);
                return Ok(new Response(true, new { updatedCount }, "Mail sent successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, ex.Message));
            }
        }

        /// <summary>
        /// POST api/LcRefund/markNotCovered
        /// Mark one or more LANs as Not Covered (removes them from the active queue).
        ///
        /// Body: { ids: [1,2], updatedBy: "user" }
        /// </summary>
        [HttpPost("markNotCovered")]
        public async Task<IActionResult> MarkNotCovered([FromBody] LcRefundNotCoveredDto dto)
        {
            if (dto == null || dto.Ids == null || dto.Ids.Count == 0)
                return BadRequest(new Response(false, null, "Select at least one LAN"));

            try
            {
                var updatedCount = await _service.MarkNotCoveredAsync(dto);
                return Ok(new Response(true, new { updatedCount }, "Marked as Not Covered"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, ex.Message));
            }
        }

        /// <summary>
        /// POST api/LcRefund/upsertUtr
        /// Add or update UTR entries for a LAN.
        /// Multiple entries are allowed (one per insurer, or multiple per insurer).
        ///
        /// Body:
        /// {
        ///   lan: "LAN123",
        ///   entries: [
        ///     { type: "Kotak",    utr: "UTR001", paymentReceivedDate: "2024-01-15", amount: 5600 },
        ///     { type: "ICICI",    utr: "UTR002", paymentReceivedDate: "2024-01-16", amount: 5600 },
        ///     { type: "NivaBupa", utr: "UTR003", paymentReceivedDate: "2024-01-17", amount: 5560 }
        ///   ],
        ///   updatedBy: "user"
        /// }
        /// </summary>
        [HttpPost("upsertUtr")]
        public async Task<IActionResult> UpsertUtr([FromBody] LcRefundAddUtrDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.LAN))
                return BadRequest(new Response(false, null, "LAN is required"));

            if (dto.Entries == null || !dto.Entries.Any())
                return BadRequest(new Response(false, null, "At least one UTR entry is required"));

            try
            {
                var affected = await _service.UpsertUtrAsync(dto);
                return Ok(new Response(true, new { affected }, "UTR details saved"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new Response(false, null, ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response(false, null, ex.Message));
            }
        }

        /// <summary>
        /// POST api/LcRefund/markCompleted
        /// Mark a case as Completed once UTR is verified.
        ///
        /// Body: { id: 5, updatedBy: "user" }
        /// </summary>
        [HttpPost("markCompleted")]
        public async Task<IActionResult> MarkCompleted([FromBody] LcRefundCompleteDto dto)
        {
            if (dto == null || dto.Id <= 0)
                return BadRequest(new Response(false, null, "Invalid request"));

            var result = await _service.MarkCompletedAsync(dto);
            return result switch
            {
                > 0 => Ok(new Response(true, null, "Marked as Completed")),
                -1 => NotFound(new Response(false, null, "Record not found")),
                _ => StatusCode(500, new Response(false, null, "An error occurred"))
            };
        }

        // ────────────────────────────────────────────────────────────────────────
        // REPORT
        // ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// GET api/LcRefund/report?fromDate=2024-01-01&amp;toDate=2024-01-31
        /// Downloads an Excel report with Summary + Not Covered sheets.
        /// </summary>
        [HttpGet("report")]
        public async Task<IActionResult> DownloadReport(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var fileBytes = await _service.GetReportExcelAsync(fromDate, toDate);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"LC_Refund_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }
    }
}