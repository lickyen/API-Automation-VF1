// Services/LcRefundService.cs
using ClosedXML.Excel;
using ProductivityApi.Constants;
using ProductivityApi.Helpers;
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

        public LcRefundService(
            LcRefundRepository repository,
            EmailService emailService,
            TemplateService templateService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
        }

        public async Task<FcRefundBulkUploadResult> AddLanAsync(List<LcRefundAddLanDto> dtos)
            => await _repository.AddLanAsync(dtos);

        public async Task<LcRefundModel?> GetByLanAsync(string lan)
            => await _repository.GetByLanAsync(lan);

        public async Task<List<LcRefundModel>> GetByStatusAsync(string status)
            => await _repository.GetByStatusAsync(status);

        public async Task<List<LcUtrModel>> GetUtrByLanAsync(string lan)
            => await _repository.GetUtrByLanAsync(lan);

        public async Task<int> TriggerMailAsync(LcRefundTriggerMailDto dto)
        {
            if (dto.Lans == null || dto.Lans.Count == 0)
                throw new ArgumentException("No LANs selected");

            var distinctLans = dto.Lans.Distinct().ToList();

            // 🔥 Parallel fetch (faster)
            var tasks = distinctLans.Select(lan => _repository.GetByLanAsync(lan));
            var results = await Task.WhenAll(tasks);
            var records = results.Where(r => r != null).ToList();

            if (!records.Any())
                throw new Exception("No matching records found");

            int totalUpdated = 0;

            // 🔹 Split per insurer
            var insurerMap = new Dictionary<string, List<LcRefundModel>>
            {
                { "Kotak",   records.Where(r => r.Kotak?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true).ToList() },
                { "ICICI",   records.Where(r => r.Icici?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true).ToList() },
                { "NivaBupa",records.Where(r => r.NivaBupa?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true).ToList() },
                { "GoDigit", records.Where(r => r.GoDigit?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true).ToList() }
            };

            // 🔥 Loop instead of repeated if blocks
            foreach (var kvp in insurerMap)
            {
                var insurer = kvp.Key;
                var insurerRecords = kvp.Value;

                if (!insurerRecords.Any())
                    continue;

                var (sent, msg) = await SendInsurerMailAsync(insurerRecords, insurer);
                if (!sent)
                    throw new Exception($"{insurer}: {msg}");

                var lans = insurerRecords.Select(x => x.Lan).Distinct().ToList();

                totalUpdated += await _repository.UpdateMailTriggerAsync(
                    lans,
                    insurer,
                    dto.UpdatedBy
                );
            }

            return totalUpdated;
        }
        private async Task<(bool Success, string Message)> SendInsurerMailAsync(List<LcRefundModel> records, string insurer)
        {
            var emailDetails = await _repository.GetInsurerEmailsAsync(insurer);

            if (!emailDetails.Any())
                return (false, $"No recipient email IDs configured for {insurer}");

            var to = emailDetails
                .Where(e => e.EmailType == "1")
                .Select(e => e.EmailId)
                .ToList();

            var cc = emailDetails
                .Where(e => e.EmailType == "2")
                .Select(e => e.EmailId)
                .ToList();

            if (!to.Any())
                return (false, $"No To recipients configured for {insurer}");

            var template = _templateService.GetEmailTemplate("LcRefundMailTriggerEmail");

            var placeholders = new Dictionary<string, string>
            {
                { "Insurer", insurer },
                { "DateTime", DateTime.Now.ToString("dd-MMM-yyyy HH:mm") },
                { "Table", BuildTableHtml(records, insurer) }
            };

            var subject = $"Loan Cancellation Premium Refund | {DateTime.Now:dd-MMM-yyyy HH:mm}";

            var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

            var result = await _emailService.SendEmail(
                subject,
                body,
                to,
                cc,
                new List<Attachment>()
            );

            return result == "Success"
                ? (true, "Mail sent successfully")
                : (false, result);
        }
        private string BuildTableHtml(List<LcRefundModel> records, string insurer)
        {
            var sb = new StringBuilder();
            sb.Append("<table border='1' cellpadding='3' style='border-collapse:collapse;'>");

            if (insurer == "Kotak")
            {
                sb.Append("<tr style='background-color: #f2f2f2;'><th>Policy Number</th><th>Client Name</th><th>Member Name</th><th>Loan A/C ID</th><th>System Generated No.</th><th>DOC</th><th>Member Name as per NEFT COPY</th><th>Account No</th><th>IFSC code</th><th>Payout</th></tr>");

                foreach (var r in records)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td>{r.PolicyNumber}</td>");
                    sb.Append($"<td>Vistaar Financial Services Pvt. Ltd.</td>");
                    sb.Append($"<td>{r.FullName}</td>");
                    sb.Append($"<td>{r.Lan}</td>");
                    sb.Append($"<td>{r.SgNo}</td>");
                    sb.Append($"<td>{r.DisbDate:dd-MMM-yyyy}</td>");
                    sb.Append($"<td>NA</td><td>NA</td><td>NA</td>");
                    sb.Append($"<td>MPH</td>");
                    sb.Append("</tr>");
                }
            }
            else if (insurer == "ICICI")
            {
                sb.Append("<tr style='background-color: #f2f2f2;'><th>Loan Account No</th><th>Customer Name</th><th>DOC</th></tr>");

                foreach (var r in records)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td>{r.Lan}</td>");
                    sb.Append($"<td>{r.FullName}</td>");
                    sb.Append($"<td>{r.DisbDate:dd-MMM-yyyy}</td>");
                    sb.Append("</tr>");
                }
            }
            else if (insurer == "NivaBupa")
            {
                sb.Append("<tr style='background-color: #f2f2f2;'><th>Loan Account No</th><th>Premium</th><th>HDC</th><th>PA</th><th>OPD</th></tr>");

                foreach (var r in records)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td>{r.Lan}</td>");
                    sb.Append($"<td>5560</td>");
                    sb.Append($"<td>{r.Hdc}</td>");
                    sb.Append($"<td>{r.Pa}</td>");
                    sb.Append($"<td>{r.Opd}</td>");
                    sb.Append("</tr>");
                }
            }
            else if (insurer == "GoDigit")
            {
                sb.Append("<tr style='background-color: #f2f2f2;'><th>LAN</th><th>Policy Number</th></tr>");

                foreach (var r in records)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td>{r.Lan}</td>");
                    sb.Append($"<td>{r.PolicyNumber}</td>");
                    sb.Append("</tr>");
                }
            }

            sb.Append("</table>");
            return sb.ToString();
        }

        public async Task<int> MarkNotCoveredAsync(LcRefundNotCoveredDto dto)
        {
            return await _repository.MarkNotCoveredAsync(dto.Lans.Distinct().ToList(), dto.UpdatedBy);
        }
        public async Task<int> MarkCompletedAsync(LcRefundCompleteDto dto)
        {
            return await _repository.MarkCompletedAsync(dto.Lans.Distinct().ToList(), dto.UpdatedBy);

        }
        public async Task<(int success, int duplicates, int total)> AddUtrAsync(LcRefundAddUtrDto dto)
        {
            return await _repository.AddUtrAsync(dto.lans, dto.UpdatedBy);
        }
        public async Task<byte[]?> GetReportExcelAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (refunds, utrs) = await _repository.GetReportAsync(fromDate, toDate);

            return ExcelHelper.GenerateExcel(
                (refunds, "Data"),
                (utrs, "UTR")
            );
        }
    }
}