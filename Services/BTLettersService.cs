using System.Net;
using System.Net.Mail;
using System.Text;
using ClosedXML.Excel;
using ProductivityApi.Helpers;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class BTLettersService
    {
        private readonly BTLettersRepository _repository;
        private readonly string _filePath;
        private readonly IConfiguration _config;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;


        public BTLettersService(BTLettersRepository repository, AppSettings appSettings, IConfiguration config, EmailService emailService, TemplateService templateService)
        {
            _repository = repository;
            _filePath = appSettings.FilePaths.BTFilePath;
            _config = config;
            _emailService = emailService;
            _templateService = templateService;
        }
        public async Task<(byte[]? FileBytes, string? Error)> CreateBtFormatAsync(IFormFile file)
        {
            var allowed = new[] { ".xls", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowed.Contains(ext))
                return (null, "Unsupported file type. Upload .xls or .xlsx.");

            if (!Directory.Exists(_filePath))
                Directory.CreateDirectory(_filePath);

            var masterPath = Path.Combine(_filePath, $"BT_MasterFormat{ext}");

            await using (var fs = new FileStream(masterPath, FileMode.Create, FileAccess.Write))
            {
                await file.CopyToAsync(fs);
            }

            var rows = FilterExcelRows(masterPath);

            if (!rows.Any())
                return (null, "0 rows matched the filter criteria. Please check the file.");

            SaveFilteredFormat(rows);

            var outputPath = Path.Combine(_filePath, "BT_UploadFormat.xlsx");
            var bytes = await File.ReadAllBytesAsync(outputPath);

            return (bytes, null);
        }

        private void SaveFilteredFormat(List<BTUploadFormatRowDto> rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("BT Upload Format");

            var headers = new[]
            {
                "Loan Number", "Customer Name", "Branch Code", "Disb/File received Date",
                "Previous financier name or Property Seller name", "BT LAN number",
                "Instrument Type", "Instrument Number", "Instrument Date",
                "Instrument Bank", "Instrument Amount"
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                ws.Cell(r + 2, 1).Value = row.LAN;
                ws.Cell(r + 2, 2).Value = row.CustomerName;
                ws.Cell(r + 2, 3).Value = row.BranchCode;
                ws.Cell(r + 2, 4).Value = row.DisbDate.ToString("dd-MM-yyyy");
                ws.Cell(r + 2, 5).Value = row.PrevFinancier;
                ws.Cell(r + 2, 6).Value = row.BtLan;
                ws.Cell(r + 2, 7).Value = "";   // Instrument Type     — blank for user to fill
                ws.Cell(r + 2, 8).Value = "";   // Instrument Number
                ws.Cell(r + 2, 9).Value = "";   // Instrument Date
                ws.Cell(r + 2, 10).Value = "";   // Instrument Bank
                ws.Cell(r + 2, 11).Value = "";   // Instrument Amount
            }

            var templatePath = Path.Combine(_filePath, "BT_UploadFormat.xlsx");
            wb.SaveAs(templatePath);
        }

        // Same WHERE conditions as BT_GetUploadFormat SP:
        //   [Cheque/DD availability]                    = 'Handed over to customer'
        //   DATEDIFF(day, [Disb/File received Date], GETDATE()) >= 7
        //   [Document category(Green,Amber,Red)]        = 'Red'
        //   [Balance take over or Property purchase case] = 'BT / Take Over Case'
        //   [DPD]                                       = '#N/A'
        private static List<BTUploadFormatRowDto> FilterExcelRows(string path)
        {
            var list = new List<BTUploadFormatRowDto>();
            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.First();

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var chequeDDAvailability = row.Cell(13).GetValue<string>();
                var disbDateRaw = row.Cell(16).GetValue<string>();
                var documentCategory = row.Cell(26).GetValue<string>();
                var balanceTakeOver = row.Cell(27).GetValue<string>();
                var dpd = row.Cell(36).GetValue<string>();

                if (chequeDDAvailability != "Handed over to customer") continue;
                if (!DateTime.TryParse(disbDateRaw, out var disbDate)) continue;
                if ((DateTime.Now - disbDate).TotalDays < 7) continue;
                if (documentCategory != "Red") continue;
                if (balanceTakeOver != "BT / Take Over Case") continue;
                if (dpd != "#N/A") continue;

                list.Add(new BTUploadFormatRowDto
                {
                    LAN = row.Cell(7).GetValue<string>(),
                    CustomerName = row.Cell(8).GetValue<string>(),
                    BranchCode = row.Cell(2).GetValue<int>(),
                    DisbDate = disbDate,
                    PrevFinancier = row.Cell(32).GetValue<string>(),
                    BtLan = row.Cell(31).GetValue<string>(),
                });
            }

            return list;
        }
        // ── Step 2 : Upload filled BT file → DELETE old + INSERT new ─────────

        public async Task<(int RowsUploaded, string? Error)> UploadFilledBtFileAsync(IFormFile file, string uploadedBy)
        {
            var allowed = new[] { ".xls", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowed.Contains(ext))
                return (0, "Unsupported file type. Upload .xls or .xlsx.");

            if (!Directory.Exists(_filePath))
                Directory.CreateDirectory(_filePath);

            var fullPath = Path.Combine(_filePath, $"BT_UploadFile{ext}");
            await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                await file.CopyToAsync(fs);

            var rows = ParseFilledBtRows(fullPath);

            var validationError = ValidateRows(rows);
            if (validationError != null)
                return (0, validationError);

            var count = await _repository.UploadBtFileRowsAsync(rows, uploadedBy);

            return count == 0
                ? (0, "0 rows uploaded. Please check the file.")
                : (count, null);
        }        // ── Step 3 : Letter data ──────────────────────────────

        public async Task<List<BTListItem>> GetLetterDataAsync(string letterType)
        {
            var items = await _repository.GetLetterDataAsync(letterType);
            return items.ToList();
        }

        // ── Step 4 : Send letters ─────────────────────────────

        public async Task<BTSendLetterResult> SendLettersAsync(string letterType, string sentBy)
        {
            var rows = (await _repository.GetLetterDataAsync(letterType)).ToList();
            var result = new BTSendLetterResult { TotalRows = rows.Count };

            foreach (var row in rows)
            {
                try
                {
                    var emailId = await _repository.GetBankEmailIdAsync(row.PrevFinancierName ?? "");
                    if (string.IsNullOrWhiteSpace(emailId))
                    {
                        result.Errors.Add($"LAN {row.LAN}: No email found for '{row.PrevFinancierName}'.");
                        continue;
                    }

                    string templateType = letterType is "BT-1" or "BT-2" ? "BTLetter1Email" : "BTLetter3Email";

                    var placeholders = new Dictionary<string, string>
                    {
                        { "CustomerName", row.CustomerName },
                        { "BT_LAN",       row.BT_LAN ?? "" },
                        { "Table",        BuildInstrumentTable(row) }
                    };

                    var emailTemplate = _templateService.GetEmailTemplate(templateType);
                    var subject = _templateService.ReplacePlaceholders(emailTemplate.Subject, placeholders);
                    var body = _templateService.ReplacePlaceholders(emailTemplate.Body, placeholders);

                    var toList = new List<string> { emailId };
                    var ccList = new List<string> ();

                    var sendResult = await _emailService.SendEmail(subject, body, toList, ccList, source: "BTLetters");

                    if (sendResult == "Success")
                    {
                        await _repository.UpdateMailStatusAsync(row.LAN, letterType, sentBy);
                        result.MailsSent++;
                    }
                    else
                    {
                        result.Errors.Add($"LAN {row.LAN}: {sendResult}");
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"LAN {row.LAN}: {ex.Message}");
                }
            }

            return result;
        }
        // ── Report ────────────────────────────────────────────

        public async Task<byte[]?> GetReportExcelAsync()
        {
            var data = await _repository.GetReportDataAsync();
            var list = data.ToList();

            if (!list.Any())
                return null;

            return ExcelHelper.GenerateExcel((list, "BT-MIS Report"));
        }

        // ── Private helpers ───────────────────────────────────

        private static List<BTUploadFormatRowDto> ParseFilledBtRows(string path)
        {
            var list = new List<BTUploadFormatRowDto>();
            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheets.First();

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var lan = row.Cell(1).GetValue<string>();
                if (string.IsNullOrWhiteSpace(lan)) continue;

                var disbDateRaw = row.Cell(4).GetValue<string>();
                if (!DateTime.TryParse(disbDateRaw, out var disbDate))
                    disbDate = default;

                list.Add(new BTUploadFormatRowDto
                {
                    LAN = lan,
                    CustomerName = row.Cell(2).GetValue<string>(),
                    BranchCode = row.Cell(3).GetValue<int>(),
                    DisbDate = disbDate,
                    PrevFinancier = row.Cell(5).GetValue<string>(),
                    BtLan = row.Cell(6).GetValue<string>(),
                    InstrumentType = row.Cell(7).GetValue<string>(),
                    InstrumentNumber = row.Cell(8).GetValue<string>(),
                    InstrumentDate = row.Cell(9).GetValue<string>(),
                    InstrumentBank = row.Cell(10).GetValue<string>(),
                    InstrumentAmount = row.Cell(11).GetValue<string>()
                });
            }

            return list;
        }
        private static string? ValidateRows(List<BTUploadFormatRowDto> rows)
        {
            foreach (var r in rows)
            {
                if (string.IsNullOrWhiteSpace(r.LAN)) return "Loan Number is missing.";
                if (string.IsNullOrWhiteSpace(r.CustomerName)) return $"LAN {r.LAN}: Customer Name is missing.";
                if (r.BranchCode == 0) return $"LAN {r.LAN}: Branch Code is missing.";
                if (r.DisbDate == default) return $"LAN {r.LAN}: Disbursement Date is invalid.";
                if (string.IsNullOrWhiteSpace(r.PrevFinancier)) return $"LAN {r.LAN}: Previous Financier is missing.";
                if (string.IsNullOrWhiteSpace(r.InstrumentType)) return $"LAN {r.LAN}: Instrument Type is missing.";
                if (string.IsNullOrWhiteSpace(r.InstrumentNumber)) return $"LAN {r.LAN}: Instrument Number is missing.";
                if (string.IsNullOrWhiteSpace(r.InstrumentDate)) return $"LAN {r.LAN}: Instrument Date is missing.";
                if (string.IsNullOrWhiteSpace(r.InstrumentBank)) return $"LAN {r.LAN}: Instrument Bank is missing.";
                if (string.IsNullOrWhiteSpace(r.InstrumentAmount)) return $"LAN {r.LAN}: Instrument Amount is missing.";
            }
            return null;
        }

        private static string BuildInstrumentTable(BTListItem row)
        {
            var sb = new StringBuilder();
            sb.Append("<table border='1' cellpadding='3' style='border-collapse:collapse;'>");
            sb.Append("<tr><th>Name of the Payee</th><th>Pay Order/DD/Cheque Number</th><th>Pay Order/DD/Cheque Date</th><th>Amount</th></tr>");
            sb.Append($"<tr>"
                + $"<td style='text-align:center;'>{row.PrevFinancierName}</td>"
                + $"<td style='text-align:center;'>{row.InstrumentType}: {row.InstrumentNumber}<br />{row.InstrumentBank}</td>"
                + $"<td style='text-align:center;'>{row.InstrumentDate}</td>"
                + $"<td style='text-align:center;'>&#8377;{row.InstrumentAmount}</td>"
                + "</tr>");
            sb.Append("</table>");
            return sb.ToString();
        }

        // ── Email Master ──────────────────────────────────────

        public async Task<List<BTEmailMaster>> GetEmailMasterAsync()
        {
            var items = await _repository.GetEmailMasterAsync();
            return items.ToList();
        }

        public async Task<(int Result, string? Error)> UpdateEmailMasterAsync(BTUpdateEmailMasterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Bank_Name)) return (0, "Bank Name is required.");
            if (string.IsNullOrWhiteSpace(request.Email_ID)) return (0, "Email ID is required.");

            var result = await _repository.UpdateEmailMasterAsync(request);

            return result == 1
                ? (result, null)
                : (0, "Record not found or update failed.");
        }

        public async Task<(int RowsUploaded, string? Error)> uploadEmailMasterAsync(IFormFile file, string uploadedBy)
        {
            var allowed = new[] { ".xls", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowed.Contains(ext))
                return (0, "Unsupported file type. Upload .xls or .xlsx.");

            var rows = ParseEmailMasterRows(file);

            if (!rows.Any())
                return (0, "No valid rows found in the file.");

            var count = await _repository.uploadEmailMasterAsync(rows, uploadedBy);

            return count == 0
                ? (0, "0 rows uploaded. Please check the file.")
                : (count, null);
        }

        private static List<BTEmailMaster> ParseEmailMasterRows(IFormFile file)
        {
            var list = new List<BTEmailMaster>();

            using var stream = file.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet(1);

            var headerRow = ws.FirstRowUsed();

            var bankCol = headerRow.Cells()
                .First(c => c.GetString().Trim() == "Bank_Name")
                .Address.ColumnNumber;

            var emailCol = headerRow.Cells()
                .First(c => c.GetString().Trim() == "Email_ID")
                .Address.ColumnNumber;

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var bankName = row.Cell(bankCol).GetString();
                var emailId = row.Cell(emailCol).GetString();

                if (string.IsNullOrWhiteSpace(bankName) ||
                    string.IsNullOrWhiteSpace(emailId))
                    continue;

                list.Add(new BTEmailMaster
                {
                    Bank_Name = bankName.Trim(),
                    Email_ID = emailId.Trim()
                });
            }

            return list;
        }

        //public async Task<byte[]?> DownloadEmailMasterAsync()
        //{
        //    var data = await _repository.GetEmailMasterAsync();
        //    return ExcelHelper.GenerateExcel(data, "Data");
        //}

        public async Task<byte[]?> DownloadEmailMasterAsync()
        {
            var data = await _repository.GetEmailMasterAsync();
            var list = data.ToList();

            if (!list.Any())
                return null;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Data");

            // Header
            ws.Cell(1, 1).Value = "Bank Name";
            ws.Cell(1, 2).Value = "Email ID";

            // Style header
            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#005baa");
            headerRow.Style.Font.FontColor = XLColor.White;

            // Data
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                ws.Cell(i + 2, 1).Value = row.Bank_Name;
                ws.Cell(i + 2, 2).Value = row.Email_ID;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}