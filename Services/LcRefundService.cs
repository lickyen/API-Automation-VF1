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

            // Fetch records
            var records = new List<LcRefundModel>();
            foreach (var lan in distinctLans)
            {
                var record = await _repository.GetByLanAsync(lan); 
                if (record != null)
                    records.Add(record);
            }

            if (!records.Any())
                throw new Exception("No matching records found");

            // Send mail
            var (sent, message) = await SendInsurerMailAsync(records, dto.Insurer);
            if (!sent)
                throw new Exception(message);

            // Update trigger
            return await _repository.UpdateMailTriggerAsync(distinctLans, dto.Insurer, dto.UpdatedBy);
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
                sb.Append($"<td>{r.Lan}</td>");
                sb.Append($"<td>{r.FullName}</td>");
                sb.Append($"<td>{r.PolicyNumber}</td>");
                sb.Append($"<td>{r.SgNo}</td>");
                sb.Append($"<td>{r.DisbDate:dd-MMM-yyyy}</td>");
                sb.Append($"<td>{r.CancelledDate:dd-MMM-yyyy}</td>");

                if (insurer == LcRefundInsurer.NivaBupa)
                {
                    sb.Append($"<td>{r.Hdc}</td>");
                    sb.Append($"<td>{r.Pa}</td>");
                    sb.Append($"<td>{r.Opd}</td>");
                }

                sb.Append("</tr>");
            }

            sb.Append("</table>");
            return sb.ToString();
        }

        public async Task<int> MarkNotCoveredAsync(LcRefundNotCoveredDto dto)
        {
            if (dto.Lans == null || !dto.Lans.Any())
                throw new ArgumentException("No Lans provided");

            return await _repository.MarkNotCoveredAsync(dto.Lans.Distinct().ToList(), dto.UpdatedBy);
        }


        public async Task<int> UpsertUtrAsync(LcRefundAddUtrDto dto)
        {
            if (dto.Entries == null || !dto.Entries.Any())
                throw new ArgumentException("No UTR entries provided");

            return await _repository.UpsertUtrAsync(dto.Lan, dto.Entries, dto.UpdatedBy);
        }

        public async Task<int> MarkCompletedAsync(LcRefundCompleteDto dto)
            => await _repository.MarkCompletedAsync(dto.Id, dto.UpdatedBy);


        public async Task<byte[]?> GetReportExcelAsync(DateTime? fromDate, DateTime? toDate)
        {
            var data = await _repository.GetReportAsync(fromDate, toDate);

            return ExcelHelper.GenerateExcel(
                data
            );
        }

        public async Task<FcRefundBulkUploadResult> AddLanAsync(List<LcRefundAddLanDto> dtos)
            => await _repository.AddLanAsync(dtos);
    }
}