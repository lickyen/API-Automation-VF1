// Services/FcRefundService.cs
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using ProductivityApi.Constants;
using ProductivityApi.Helpers;
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
        private readonly AppSettings _appSettings;
        private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024; // 10MB

        public FcRefundService(FcRefundRepository repository, EmailService emailService, TemplateService templateService, AppSettings appSettings)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _appSettings = appSettings;
        }

        public async Task<FcRefundBulkUploadResult> AddLanAsync(List<FcRefundAddLanDto> dtos)
             => await _repository.AddLanAsync(dtos);
        public async Task<FcRefundModel?> GetByLanAsync(string lan)
            => await _repository.GetByLanAsync(lan);
        public async Task<List<FcRefundModel>> GetByStatusAsync(string status)
            => await _repository.GetByStatusAsync(status);

        public async Task<List<FcRefundModel>> GetByBranchAsync(string branchCode, string? status)
            => await _repository.GetByBranchAsync(branchCode, status);
        public async Task<int> BranchSubmitAsync(FcRefundBranchSubmitDto dto)
        {
            var basePath = _appSettings.FilePaths.FcRefundFilePath;

            var record = await _repository.GetByLanAsync(dto.Lan);
            if (record == null) return -1;

            string? bankProofFilePath = record.BankProofFilePath;
            string? requestLetterFilePath = record.RequestLetterFilePath;

            if (!string.IsNullOrWhiteSpace(basePath))
            {
                Directory.CreateDirectory(basePath);

                var bankProofExt = Path.GetExtension(dto.BankProofFileName);
                var bankProofName = $"{record.Lan}_BankProof{bankProofExt}";
                await File.WriteAllBytesAsync(
                    Path.Combine(basePath, bankProofName),
                    Convert.FromBase64String(dto.BankProofBase64)
                );
                bankProofFilePath = bankProofName;

                var requestLetterExt = Path.GetExtension(dto.RequestLetterFileName);
                var requestLetterName = $"{record.Lan}_ReqLetter{requestLetterExt}";
                await File.WriteAllBytesAsync(
                    Path.Combine(basePath, requestLetterName),
                    Convert.FromBase64String(dto.RequestLetterBase64)
                );
                requestLetterFilePath = requestLetterName;
            }

            return await _repository.BranchSubmitAsync(dto, bankProofFilePath, requestLetterFilePath);
        }

        private bool IsAttachmentSizeValid(List<FcRefundModel> records, string basePath, out long totalSize)
        {
            totalSize = 0;

            foreach (var record in records)
            {
                if (!string.IsNullOrWhiteSpace(record.BankProofFilePath))
                {
                    var path = Path.Combine(basePath, record.BankProofFilePath);
                    if (File.Exists(path))
                        totalSize += new FileInfo(path).Length;
                }

                if (!string.IsNullOrWhiteSpace(record.RequestLetterFilePath))
                {
                    var path = Path.Combine(basePath, record.RequestLetterFilePath);
                    if (File.Exists(path))
                        totalSize += new FileInfo(path).Length;
                }
            }

            return totalSize <= MaxAttachmentSizeBytes;
        }

        public async Task<int> TriggerMailAsync(FcRefundTriggerMailDto dto)
        {
            if (dto.Lans == null || dto.Lans.Count == 0)
                throw new ArgumentException("No LANs selected");

            var distinctLans = dto.Lans.Distinct().ToList();

            // Step 1: Fetch records
            var records = new List<FcRefundModel>();
            foreach (var lan in distinctLans)
            {
                var record = await _repository.GetByLanAsync(lan);
                if (record != null)
                    records.Add(record);
            }

            if (!records.Any())
                throw new Exception("No matching records found");

            // 🔥 Split based on InsurerName
            var kotakRecords = records
                .Where(r => r.InsurerName == "Kotak")
                .ToList();

            var goDigitRecords = records
                .Where(r => r.InsurerName == "GoDigit")
                .ToList();

            int totalUpdated = 0;

            // Kotak
            if (kotakRecords.Any())
            {
                var (sent, msg) = await SendKotakMailAsync(kotakRecords);
                if (!sent) throw new Exception("Kotak: " + msg);

                var lans = kotakRecords.Select(x => x.Lan).ToList();
                totalUpdated += await _repository.UpdateMailTriggerAsync(lans, dto.UpdatedBy, "Kotak");
            }

            // GoDigit
            if (goDigitRecords.Any())
            {
                var (sent, msg) = await SendGoDigitMailAsync(goDigitRecords);
                if (!sent) throw new Exception("GoDigit: " + msg);

                var lans = goDigitRecords.Select(x => x.Lan).ToList();
                totalUpdated += await _repository.UpdateMailTriggerAsync(lans, dto.UpdatedBy, "GoDigit");
            }

            return totalUpdated;
        }
        public async Task<(bool Success, string Message)> SendKotakMailAsync(List<FcRefundModel> records)
        {
            var basePath = _appSettings.FilePaths.FcRefundFilePath;

            // Size check
            if (!IsAttachmentSizeValid(records, basePath, out _))
                return (false, "Mail size exceeds 10MB. Please select fewer cases.");

            // Fetch To/Cc from DB
            var emailDetails = await _repository.GetBankEmailDetailsAsync("Kotak");
            if (!emailDetails.Any())
                return (false, "No recipient email IDs configured for Kotak");

            var to = emailDetails.Where(e => e.EmailType == "1").Select(e => e.EmailId).ToList();
            var cc = emailDetails.Where(e => e.EmailType == "2").Select(e => e.EmailId).ToList();

            if (!to.Any())
                return (false, "No recipients");

            // Load template
            var template = _templateService.GetEmailTemplate("FcRefundMailTriggerEmail");
            var placeholders = new Dictionary<string, string>
            {
                { "DateTime", DateTime.Now.ToString("dd-MMM-yyyy HH:mm") },
                { "Table",    BuildKotakTableHtml(records) }
            };

            var subject = "Loan Foreclosure Premium refund | " + DateTime.Now;

            var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

            var attachments = BuildAttachments(records, basePath);

            var result = await _emailService.SendEmail(subject, body, to, cc, attachments);
            return result == "Success" ? (true, "Mail sent successfully") : (false, result);
        }
        public async Task<(bool Success, string Message)> SendGoDigitMailAsync(List<FcRefundModel> records)
        {
            var basePath = _appSettings.FilePaths.FcRefundFilePath;

            // Size check
            if (!IsAttachmentSizeValid(records, basePath, out _))
                return (false, "Mail size exceeds 10MB. Please select fewer cases.");

            var emailDetails = await _repository.GetBankEmailDetailsAsync("GoDigit");
            if (!emailDetails.Any())
                return (false, "No recipient email IDs configured for GoDigit");

            var to = emailDetails.Where(e => e.EmailType == "1").Select(e => e.EmailId).ToList();
            var cc = emailDetails.Where(e => e.EmailType == "2").Select(e => e.EmailId).ToList();

            if (!to.Any())
                return (false, "No recipients");

            var template = _templateService.GetEmailTemplate("FcRefundMailTriggerEmail");

            var placeholders = new Dictionary<string, string>
            {
                { "DateTime", DateTime.Now.ToString("dd-MMM-yyyy HH:mm") },
                { "Table", BuildGoDigitTableHtml(records) }
            };

            var subject = "Loan Foreclosure Premium refund | " + DateTime.Now;

            var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

            var attachments = BuildAttachments(records, basePath);

            var result = await _emailService.SendEmail(subject, body, to, cc, attachments);
            return result == "Success" ? (true, "Mail sent successfully") : (false, result);
        }
        private string BuildKotakTableHtml(List<FcRefundModel> records)
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
                table.Append($"<td style='text-align:center;'>{r.Lan}</td>");
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
        private string BuildGoDigitTableHtml(List<FcRefundModel> records)
        {
            var table = new StringBuilder();

            table.Append("<table border='1' cellpadding='3' style='border-collapse: collapse;'>");
            table.Append("<tr style='background-color: #f2f2f2;'>");
            table.Append("<th>LAN</th>");
            table.Append("<th>Policy Number</th>");
            table.Append("<th>Customer Name</th>");
            table.Append("<th>Foreclosure Date</th>");
            table.Append("<th>Customer Name as per NEFT COPY</th>");
            table.Append("<th>Account No</th>");
            table.Append("<th>IFSC Code</th>");
            table.Append("</tr>");

            foreach (var r in records)
            {
                table.Append("<tr>");
                table.Append($"<td style='text-align:center;'>{r.Lan}</td>");
                table.Append($"<td style='text-align:center;'>{r.PolicyNo}</td>");
                table.Append($"<td style='text-align:center;'>{r.FullName}</td>");
                table.Append($"<td style='text-align:center;'>{r.ForeclosedDate:dd-MMM-yyyy}</td>");
                table.Append($"<td style='text-align:center;'>{r.FullNameAsPerBankProof}</td>");
                table.Append($"<td style='text-align:center;'>{r.AccountNo}</td>");
                table.Append($"<td style='text-align:center;'>{r.Ifsc}</td>");
                table.Append("</tr>");
            }

            table.Append("</table>");
            return table.ToString();
        }
        private List<Attachment> BuildAttachments(List<FcRefundModel> records, string basePath)
        {
            var attachments = new List<Attachment>();

            foreach (var record in records)
            {
                if (!string.IsNullOrWhiteSpace(record.BankProofFilePath))
                {
                    var path = Path.Combine(basePath, record.BankProofFilePath);
                    if (File.Exists(path))
                        attachments.Add(new Attachment(path));
                }

                if (!string.IsNullOrWhiteSpace(record.RequestLetterFilePath))
                {
                    var path = Path.Combine(basePath, record.RequestLetterFilePath);
                    if (File.Exists(path))
                        attachments.Add(new Attachment(path));
                }
            }

            return attachments;
        }
        public async Task<int> UpdateRefundAsync(List<FcRefundUpdateRefundDto> dtos)
            => await _repository.UpdateRefundAsync(dtos);
        public async Task<int> UpdateAdditionalRequestAsync(List<FcRefundAdditionalRequestDto> dtos)
            => await _repository.UpdateAdditionalRequestAsync(dtos);

        public (bool Success, string Message, FcRefundDownloadFileResponseDto? Data) GetFileAsBase64(FcRefundDownloadFileRequestDto dto)
        {
            var basePath = _appSettings.FilePaths.FcRefundFilePath;

            if (string.IsNullOrWhiteSpace(basePath))
                return (false, "FcRefundFilePath not configured", null);

            var fullPath = Path.Combine(basePath, dto.FileName);

            if (!File.Exists(fullPath))
                return (false, "File not found on server", null);

            return (true, "Success", new FcRefundDownloadFileResponseDto
            {
                Base64 = Convert.ToBase64String(File.ReadAllBytes(fullPath)),
                FileName = dto.FileName
            });
        }
        public async Task<byte[]?> GetReportExcelAsync(DateTime? fromDate, DateTime? toDate)
        {
            var completed = await _repository.GetReportAsync(fromDate, toDate, FcRefundStatus.Completed);
            var mailsent = await _repository.GetReportAsync(fromDate, toDate, FcRefundStatus.MailSentToInsurer);

            return ExcelHelper.GenerateExcel(
                (completed, "Completed"),
                (mailsent, "Mail Sent To Insurer")
            );
        }
    }

}