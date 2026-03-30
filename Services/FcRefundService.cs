// Services/FcRefundService.cs
using Microsoft.Data.SqlClient;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Data;
using System.Net.Mail;
using System.Text;

namespace ProductivityApi.Services
{
    public class FcRefundService
    {
        private readonly FcRefundRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private const long MaxAttachmentSizeBytes = 5 * 1024 * 1024; // 5MB

        public FcRefundService(FcRefundRepository repository, EmailService emailService, TemplateService templateService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
        }
        public async Task<FcRefundModel?> GetByIdAsync(int id)
            => await _repository.GetByIdAsync(id);

        public async Task<FcRefundModel?> GetByLanAsync(string lan)
            => await _repository.GetByLanAsync(lan);

        // Branch
        public async Task<List<FcRefundModel>> GetByBranchAsync(string branchCode, string? status)
            => await _repository.GetByBranchAsync(branchCode, status);

        public async Task<int> BranchSubmitAsync(FcRefundBranchSubmitDto dto)
            => await _repository.BranchSubmitAsync(dto);

        // COPS
        public async Task<List<FcRefundModel>> GetByStatusAsync(string status)
            => await _repository.GetByStatusAsync(status);

        public async Task<int> TriggerMailAsync(FcRefundTriggerMailDto dto)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
                throw new ArgumentException("No LANs selected");

            var distinctIds = dto.Ids.Distinct().ToList();

            // Step 1: Fetch each record individually
            var records = new List<FcRefundModel>();
            foreach (var id in distinctIds)
            {
                var record = await _repository.GetByIdAsync(id);
                if (record != null)
                    records.Add(record);
            }

            if (!records.Any())
                throw new Exception("No matching records found");

            // Step 2: Send single mail for all selected LANs
            var (mailSent, mailMessage) = await SendRefundMailAsync(records);
            if (!mailSent)
                throw new Exception(mailMessage);

            // Step 3: Bulk update all LANs
            return await _repository.UpdateMailTriggerAsync(distinctIds, dto.UpdatedBy);
        }

        public async Task<(bool Success, string Message)> SendRefundMailAsync(List<FcRefundModel> records)
        {
            // Size check
            long totalSize = 0;
            foreach (var record in records)
            {
                if (!string.IsNullOrWhiteSpace(record.BankProofFilePath))
                    totalSize += new FileInfo(record.BankProofFilePath).Length;
                if (!string.IsNullOrWhiteSpace(record.RequestLetterFilePath))
                    totalSize += new FileInfo(record.RequestLetterFilePath).Length;
            }

            if (totalSize > MaxAttachmentSizeBytes)
                return (false, "Mail size exceeds 5MB. Please select fewer cases.");

            // Fetch To/Cc from DB
            var emailDetails = await _repository.GetBankEmailDetailsAsync("Kotak");

            if (!emailDetails.Any())
                return (false, "No recipient email IDs configured for Kotak");

            var to = emailDetails.Where(e => e.EmailType == "1").Select(e => e.EmailId).ToList();
            var cc = emailDetails.Where(e => e.EmailType == "2").Select(e => e.EmailId).ToList();

            if (!to.Any())
                return (false, "No To recipients configured for Kotak");

            // Load template
            var template = _templateService.GetEmailTemplate("FcRefundMailTriggerEmail");

            var placeholders = new Dictionary<string, string>
            {
                { "DateTime", DateTime.Now.ToString("dd-MMM-yyyy HH:mm") },
                { "Table",    BuildTableHtml(records) }
            };

            var subject = _templateService.ReplacePlaceholders(template.Subject, placeholders);
            var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

            var attachments = new List<Attachment>();

            foreach (var record in records)
            {
                if (!string.IsNullOrWhiteSpace(record.BankProofFilePath) && File.Exists(record.BankProofFilePath))
                    attachments.Add(new Attachment(record.BankProofFilePath));

                if (!string.IsNullOrWhiteSpace(record.RequestLetterFilePath) && File.Exists(record.RequestLetterFilePath))
                    attachments.Add(new Attachment(record.RequestLetterFilePath));
            }

            var result = await _emailService.SendEmail(subject, body, to, cc, attachments);
            return result == "Success" ? (true, "Mail sent successfully"): (false, result);
        }
        private string BuildTableHtml(List<FcRefundModel> records)
        {
            var table = new StringBuilder();

            table.Append("<table border='1' cellpadding='3' style='border-collapse: collapse;'>");
            table.Append("<tr style='background-color: #f2f2f2;'>");
            table.Append("<th>Policy Number</th>");
            table.Append("<th>Client Name</th>");
            table.Append("<th>Member Name</th>");
            table.Append("<th>Loan A/C ID</th>");
            table.Append("<th>System Generated No.</th>");
            table.Append("<th>Foreclosure Date</th>");
            table.Append("<th>Member Name as per NEFT Copy</th>");
            table.Append("<th>Account No</th>");
            table.Append("<th>IFSC Code</th>");
            table.Append("<th>Payout</th>");
            table.Append("</tr>");

            foreach (var r in records)
            {
                table.Append("<tr>");
                table.Append($"<td style='text-align:center;'>{r.PolicyNo}</td>");
                table.Append($"<td style='text-align:center;'>Vistaar Financial Services Pvt. Ltd.</td>");
                table.Append($"<td style='text-align:center;'>{r.MemberName}</td>");
                table.Append($"<td style='text-align:center;'>{r.LAN}</td>");
                table.Append($"<td style='text-align:center;'>{r.SgNo}</td>");
                table.Append($"<td style='text-align:center;'>{r.ForeclosedDate:dd-MMM-yyyy}</td>");
                table.Append($"<td style='text-align:center;'>{r.FullNameAsPerBankProof}</td>");
                table.Append($"<td style='text-align:center;'>{r.AccountNo}</td>");
                table.Append($"<td style='text-align:center;'>{r.Ifsc}</td>");
                table.Append($"<td style='text-align:center;'>{r.PayoutLevel}</td>");
                table.Append("</tr>");
            }

            table.Append("</table>");
            return table.ToString();
        }
        public async Task<int> UpdateRefundAsync(FcRefundUpdateRefundDto dto)
            => await _repository.UpdateRefundAsync(dto);
    }

    }