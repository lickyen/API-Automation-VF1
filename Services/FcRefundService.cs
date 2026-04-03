// Services/FcRefundService.cs
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using ProductivityApi.Constants;
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
        public async Task<FcRefundModel?> GetByIdAsync(int id)
            => await _repository.GetByIdAsync(id);

        public async Task<FcRefundModel?> GetByLanAsync(string lan)
            => await _repository.GetByLanAsync(lan);

        // Branch
        public async Task<List<FcRefundModel>> GetByBranchAsync(string branchCode, string? status)
            => await _repository.GetByBranchAsync(branchCode, status);

        //public async Task<int> BranchSubmitAsync(FcRefundBranchSubmitDto dto)
        //    => await _repository.BranchSubmitAsync(dto);

        public async Task<int> BranchSubmitAsync(FcRefundBranchSubmitDto dto)
        {
            var basePath = _appSettings.FilePaths.FcRefundFilePath;

            var record = await _repository.GetByIdAsync(dto.Id);
            if (record == null) return -1;

            string? bankProofFilePath = record.BankProofFilePath;
            string? requestLetterFilePath = record.RequestLetterFilePath;

            if (!string.IsNullOrWhiteSpace(basePath))
            {
                Directory.CreateDirectory(basePath);

                var bankProofExt = Path.GetExtension(dto.BankProofFileName);
                var bankProofName = $"{record.LAN}_BankProof{bankProofExt}";
                await File.WriteAllBytesAsync(
                    Path.Combine(basePath, bankProofName),
                    Convert.FromBase64String(dto.BankProofBase64)
                );
                bankProofFilePath = bankProofName;

                var requestLetterExt = Path.GetExtension(dto.RequestLetterFileName);
                var requestLetterName = $"{record.LAN}_ReqLetter{requestLetterExt}";
                await File.WriteAllBytesAsync(
                    Path.Combine(basePath, requestLetterName),
                    Convert.FromBase64String(dto.RequestLetterBase64)
                );
                requestLetterFilePath = requestLetterName;
            }

            return await _repository.BranchSubmitAsync(dto, bankProofFilePath, requestLetterFilePath);
        }
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
            var basePath = _appSettings.FilePaths.FcRefundFilePath;

            // Size check
            long totalSize = 0;
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

            if (totalSize > MaxAttachmentSizeBytes)
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
                { "Table",    BuildTableHtml(records) }
            };

            var subject = "Loan Foreclosure Premium refund | " + DateTime.Now;

            var body = _templateService.ReplacePlaceholders(template.Body, placeholders);

            // Build attachments using basePath + filename
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

            var result = await _emailService.SendEmail(subject, body, to, cc, attachments);
            return result == "Success" ? (true, "Mail sent successfully") : (false, result);
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

        public async Task<FcRefundBulkUploadResult> AddLanAsync(List<FcRefundAddLanDto> dtos)
            => await _repository.AddLanAsync(dtos);

        public async Task<int> UpdateAdditionalRequestAsync(FcRefundAdditionalRequestDto dto)
            => await _repository.UpdateAdditionalRequestAsync(dto);

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
        public async Task<byte[]> GetReportExcelAsync(DateTime? fromDate, DateTime? toDate)
        {
            var data = (await _repository.GetReportAsync(fromDate, toDate)).ToList();

            var completed = data.Where(x => x.Status == FcRefundStatus.Completed).ToList();
            var mailed = data.Where(x => x.Status == FcRefundStatus.MailSentToInsurer).ToList();
            //var pending = data.Where(x => x.Status == FcRefundStatus.PendingForAction).ToList();

            using var wb = new XLWorkbook();

            AddCompletedSheet(wb, completed);
            AddMailSentSheet(wb, mailed);
            //AddPendingSheet(wb, pending);

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        private void AddCompletedSheet(XLWorkbook wb, List<FcRefundModel> data)
        {
            var dt = new DataTable("Completed");

            dt.Columns.AddRange(new[]
            {
                new DataColumn("Policy Number"),
                new DataColumn("Loan Account No"),
                new DataColumn("Customer Name"),
                new DataColumn("Branch Name"),
                new DataColumn("Foreclosure Date"),
                new DataColumn("UTR"),
                new DataColumn("Refund received amount"),
                new DataColumn("Refund received Date"),
                new DataColumn("Branch Requested Date"),
                new DataColumn("Additional Requirement"),
                new DataColumn("Requirement Raised Date"),
                new DataColumn("Status")
            });

            foreach (var x in data)
            {
                dt.Rows.Add(
                    x.PolicyNo ?? "",
                    x.LAN ?? "",
                    x.FullName ?? "",
                    x.BranchCode ?? "",
                    x.ForeclosedDate.ToString("dd-MM-yyyy"),
                    x.UTR ?? "",
                    x.RefundAmount?.ToString("0.00"),
                    x.RefundDate?.ToString("dd-MM-yyyy"),
                    x.BranchRequestedDate?.ToString("dd-MM-yyyy"),
                    x.AdditionalRequest ?? "",
                    x.AdditionalRequestDate?.ToString("dd-MM-yyyy"),
                    x.Status ?? ""
                );
            }

            var ws = wb.Worksheets.Add(dt);
            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }
        private void AddMailSentSheet(XLWorkbook wb, List<FcRefundModel> data)
        {
            var dt = new DataTable("Mail Sent To Insurer");

            dt.Columns.AddRange(new[]
            {
                new DataColumn("Policy Number"),
                new DataColumn("Loan Account No"),
                new DataColumn("Customer Name"),
                new DataColumn("Branch Name"),
                new DataColumn("Foreclosure Date"),
                new DataColumn("System Generated No"),
                new DataColumn("Account No"),
                new DataColumn("IFSC code"),
                new DataColumn("Branch Requested Date"),
                new DataColumn("Additional Requirement"),
                new DataColumn("Requirement Raised Date"),
                new DataColumn("Status")
            });

            foreach (var x in data)
            {
                dt.Rows.Add(
                    x.PolicyNo ?? "",
                    x.LAN ?? "",
                    x.FullName ?? "",
                    x.BranchCode ?? "",
                    x.ForeclosedDate.ToString("dd-MM-yyyy"),
                    x.SgNo ?? "",
                    x.AccountNo ?? "",
                    x.Ifsc ?? "",
                    x.BranchRequestedDate?.ToString("dd-MM-yyyy"),
                    x.AdditionalRequest ?? "",
                    x.AdditionalRequestDate?.ToString("dd-MM-yyyy"),
                    x.Status ?? ""
                );
            }

            var ws = wb.Worksheets.Add(dt);
            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }
    }

}