// Services/LcRefundService.cs
using ClosedXML.Excel;
using ProductivityApi.Constants;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Data;
using System.Net.Mail;
using System.Text;

namespace ProductivityApi.Services
{
    public class LcRefundService
    {
        private readonly LcRefundRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024; // 10 MB

        public LcRefundService(
            LcRefundRepository repository,
            EmailService emailService,
            TemplateService templateService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
        }

        // ── Queries ──────────────────────────────────────────────────────────────

        public async Task<List<LcRefundModel>> GetFreshCasesAsync()
            => await _repository.GetFreshCasesAsync();

        public async Task<List<LcRefundModel>> GetByStatusAsync(string status)
            => await _repository.GetByStatusAsync(status);

        public async Task<LcRefundModel?> GetByIdAsync(int id)
            => await _repository.GetByIdAsync(id);

        public async Task<LcRefundModel?> GetByLanAsync(string lan)
            => await _repository.GetByLanAsync(lan);

        public async Task<List<LcUtrModel>> GetUtrByLanAsync(string lan)
            => await _repository.GetUtrByLanAsync(lan);

        // ── Mail trigger ─────────────────────────────────────────────────────────

        /// <summary>
        /// Sends one mail for the selected insurer (covering all chosen LANs),
        /// then stamps the trigger date on each row.
        /// </summary>
        public async Task<int> TriggerMailAsync(LcRefundTriggerMailDto dto)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
                throw new ArgumentException("No LANs selected");

            if (!IsValidInsurer(dto.Insurer))
                throw new ArgumentException($"Unknown insurer: {dto.Insurer}");

            var distinctIds = dto.Ids.Distinct().ToList();

            // Fetch records
            var records = new List<LcRefundModel>();
            foreach (var id in distinctIds)
            {
                var record = await _repository.GetByIdAsync(id);
                if (record != null)
                    records.Add(record);
            }

            if (!records.Any())
                throw new Exception("No matching records found");

            // Send mail
            var (sent, message) = await SendInsurerMailAsync(records, dto.Insurer);
            if (!sent)
                throw new Exception(message);

            // Stamp trigger date & move to In Progress
            return await _repository.UpdateMailTriggerAsync(distinctIds, dto.Insurer, dto.UpdatedBy);
        }

        private async Task<(bool Success, string Message)> SendInsurerMailAsync(
            List<LcRefundModel> records, string insurer)
        {
            var emailDetails = await _repository.GetInsurerEmailsAsync(insurer);
            if (!emailDetails.Any())
                return (false, $"No recipient email IDs configured for {insurer}");

            var to = emailDetails.Where(e => e.EmailType == "1").Select(e => e.EmailId).ToList();
            var cc = emailDetails.Where(e => e.EmailType == "2").Select(e => e.EmailId).ToList();

            if (!to.Any())
                return (false, "No recipients configured");

            // Build table based on which insurer columns are relevant
            var tableHtml = BuildTableHtml(records, insurer);

            var template = _templateService.GetEmailTemplate("LcRefundMailTriggerEmail");
            var placeholders = new Dictionary<string, string>
            {
                { "Insurer",   insurer },
                { "DateTime",  DateTime.Now.ToString("dd-MMM-yyyy HH:mm") },
                { "Table",     tableHtml }
            };

            var subject = $"LC Premium Refund – {insurer} | {DateTime.Now:dd-MMM-yyyy}";
            var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

            var result = await _emailService.SendEmail(subject, body, to, cc, new List<Attachment>());
            return result == "Success"
                ? (true, "Mail sent successfully")
                : (false, result);
        }

        private string BuildTableHtml(List<LcRefundModel> records, string insurer)
        {
            var sb = new StringBuilder();
            sb.Append("<table border='1' cellpadding='3' style='border-collapse:collapse;'>");
            sb.Append("<tr style='background-color:#f2f2f2;'>");
            sb.Append("<th>LAN</th>");
            sb.Append("<th>Customer Name</th>");
            sb.Append("<th>Policy Number</th>");
            sb.Append("<th>System Generated No</th>");
            sb.Append("<th>Disbursement Date</th>");
            sb.Append("<th>Cancellation Date</th>");

            // Insurer-specific extra columns
            if (insurer == LcRefundInsurer.NivaBupa)
            {
                sb.Append("<th>HDC</th>");
                sb.Append("<th>PA</th>");
                sb.Append("<th>OPD</th>");
            }

            sb.Append("</tr>");

            foreach (var r in records)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{r.LAN}</td>");
                sb.Append($"<td>{r.FullName}</td>");
                sb.Append($"<td>{r.PolicyNumber}</td>");
                sb.Append($"<td>{r.SystemGeneratedNo}</td>");
                sb.Append($"<td>{r.DisbDate:dd-MMM-yyyy}</td>");
                sb.Append($"<td>{r.CancelledDate:dd-MMM-yyyy}</td>");

                if (insurer == LcRefundInsurer.NivaBupa)
                {
                    sb.Append($"<td>{r.HDC}</td>");
                    sb.Append($"<td>{r.PA}</td>");
                    sb.Append($"<td>{r.OPD}</td>");
                }

                sb.Append("</tr>");
            }

            sb.Append("</table>");
            return sb.ToString();
        }

        // ── Not Covered ──────────────────────────────────────────────────────────

        public async Task<int> MarkNotCoveredAsync(LcRefundNotCoveredDto dto)
        {
            if (dto.Ids == null || !dto.Ids.Any())
                throw new ArgumentException("No IDs provided");

            return await _repository.MarkNotCoveredAsync(dto.Ids.Distinct().ToList(), dto.UpdatedBy);
        }

        // ── UTR ──────────────────────────────────────────────────────────────────

        public async Task<int> UpsertUtrAsync(LcRefundAddUtrDto dto)
        {
            if (dto.Entries == null || !dto.Entries.Any())
                throw new ArgumentException("No UTR entries provided");

            foreach (var entry in dto.Entries)
                if (!IsValidInsurer(entry.Type))
                    throw new ArgumentException($"Unknown insurer type: {entry.Type}");

            return await _repository.UpsertUtrAsync(dto.LAN, dto.Entries, dto.UpdatedBy);
        }

        // ── Complete ─────────────────────────────────────────────────────────────

        public async Task<int> MarkCompletedAsync(LcRefundCompleteDto dto)
            => await _repository.MarkCompletedAsync(dto.Id, dto.UpdatedBy);

        // ── Report ───────────────────────────────────────────────────────────────

        public async Task<byte[]> GetReportExcelAsync(DateTime? fromDate, DateTime? toDate)
        {
            var data = await _repository.GetReportAsync(fromDate, toDate);

            using var wb = new XLWorkbook();
            AddSummarySheet(wb, data);
            AddNotCoveredSheet(wb, data.Where(x => x.Status == LcRefundStatus.NotCovered).ToList());

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        private void AddSummarySheet(XLWorkbook wb, List<LcRefundModel> data)
        {
            var dt = new DataTable("Summary");
            dt.Columns.AddRange(new[]
            {
                new DataColumn("LAN"),
                new DataColumn("Customer Name"),
                new DataColumn("Policy Number"),
                new DataColumn("System Generated No"),
                new DataColumn("Disbursement Date"),
                new DataColumn("Cancellation Date"),
                new DataColumn("Source"),
                new DataColumn("ICICI"),
                new DataColumn("Niva Bupa"),
                new DataColumn("HDC"),
                new DataColumn("PA"),
                new DataColumn("OPD"),
                new DataColumn("Kotak Mail Date"),
                new DataColumn("ICICI Mail Date"),
                new DataColumn("Niva Bupa Mail Date"),
                new DataColumn("Status")
            });

            foreach (var x in data)
            {
                dt.Rows.Add(
                    x.LAN,
                    x.FullName,
                    x.PolicyNumber,
                    x.SystemGeneratedNo,
                    x.DisbDate.ToString("dd-MM-yyyy"),
                    x.CancelledDate.ToString("dd-MM-yyyy"),
                    x.Source,
                    x.Icici,
                    x.NivaBupa,
                    x.HDC ?? "",
                    x.PA ?? "",
                    x.OPD ?? "",
                    x.KotakMailTriggerDt?.ToString("dd-MM-yyyy HH:mm") ?? "",
                    x.IciciMailTriggerDt?.ToString("dd-MM-yyyy HH:mm") ?? "",
                    x.NivaBupaMailTriggerDt?.ToString("dd-MM-yyyy HH:mm") ?? "",
                    x.Status
                );
            }

            var ws = wb.Worksheets.Add(dt);
            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        private void AddNotCoveredSheet(XLWorkbook wb, List<LcRefundModel> data)
        {
            var dt = new DataTable("Not Covered");
            dt.Columns.AddRange(new[]
            {
                new DataColumn("LAN"),
                new DataColumn("Customer Name"),
                new DataColumn("Cancellation Date"),
                new DataColumn("Updated By"),
                new DataColumn("Updated Date")
            });

            foreach (var x in data)
            {
                dt.Rows.Add(
                    x.LAN,
                    x.FullName,
                    x.CancelledDate.ToString("dd-MM-yyyy"),
                    x.UpdatedBy ?? "",
                    x.UpdatedDate?.ToString("dd-MM-yyyy") ?? ""
                );
            }

            var ws = wb.Worksheets.Add(dt);
            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static bool IsValidInsurer(string insurer) =>
            insurer is LcRefundInsurer.Kotak
                    or LcRefundInsurer.ICICI
                    or LcRefundInsurer.NivaBupa;
    }
}