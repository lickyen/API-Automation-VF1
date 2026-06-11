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

        public BTLettersService(BTLettersRepository repository, AppSettings appSettings, IConfiguration config)
        {
            _repository = repository;
            _filePath = appSettings.FilePaths.BTFilePath;
            _config = config;
        }

        // ── Step 1 : Upload master Excel → filter in memory → return download ──
        // No DB insert. Applies the same WHERE conditions as BT_GetUploadFormat SP.

        public Task<(int RowsMatched, string? Error)> UploadBtFormatAsync(IFormFile file)
        {
            var allowed = new[] { ".xls", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowed.Contains(ext))
                return Task.FromResult((0, (string?)"Unsupported file type. Upload .xls or .xlsx."));

            using var stream = file.OpenReadStream();
            var rows = FilterExcelRows(stream);

            if (!rows.Any())
                return Task.FromResult((0, (string?)"0 rows matched the filter criteria. Please check the file."));

            // Cache for immediate download in same request cycle
            _filteredRows = rows;

            return Task.FromResult((rows.Count, (string?)null));
        }

        public Task<byte[]?> DownloadBtFormatAsync()
        {
            if (_filteredRows == null || !_filteredRows.Any())
                return Task.FromResult<byte[]?>(null);

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

            for (int r = 0; r < _filteredRows.Count; r++)
            {
                var row = _filteredRows[r];
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

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return Task.FromResult<byte[]?>(ms.ToArray());
        }

        // Same WHERE conditions as BT_GetUploadFormat SP:
        //   [Cheque/DD availability]                    = 'Handed over to customer'
        //   DATEDIFF(day, [Disb/File received Date], GETDATE()) >= 7
        //   [Document category(Green,Amber,Red)]        = 'Red'
        //   [Balance take over or Property purchase case] = 'BT / Take Over Case'
        //   [DPD]                                       = '#N/A'
        private static List<BTUploadFormatRowDto> FilterExcelRows(Stream stream)
        {
            var list = new List<BTUploadFormatRowDto>();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var chequeDDAvailability = row.Cell(12).GetValue<string>();
                var disbDate = row.Cell(15).GetValue<DateTime?>();
                var documentCategory = row.Cell(25).GetValue<string>();
                var balanceTakeOver = row.Cell(26).GetValue<string>();
                var dpd = row.Cell(35).GetValue<string>();

                if (chequeDDAvailability != "Handed over to customer") continue;
                if (disbDate == null) continue;
                if ((DateTime.Now - disbDate.Value).TotalDays < 7) continue;
                if (documentCategory != "Red") continue;
                if (balanceTakeOver != "BT / Take Over Case") continue;
                if (dpd != "#N/A") continue;

                list.Add(new BTUploadFormatRowDto
                {
                    LAN = row.Cell(6).GetValue<string>(),
                    CustomerName = row.Cell(7).GetValue<string>(),
                    BranchCode = row.Cell(1).GetValue<int>(),
                    DisbDate = disbDate.Value,
                    PrevFinancier = row.Cell(31).GetValue<string>(),
                    BtLan = row.Cell(30).GetValue<string>(),
                });
            }

            return list;
        }

        private List<BTUploadFormatRowDto>? _filteredRows;

        // ── Step 2 : Upload filled BT file → DELETE old + INSERT new ─────────

        public async Task<(int RowsUploaded, string? Error)> UploadFilledBtFileAsync(IFormFile file, string uploadedBy)
        {
            var allowed = new[] { ".xls", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowed.Contains(ext))
                return (0, "Unsupported file type. Upload .xls or .xlsx.");

            if (!Directory.Exists(_filePath))
                Directory.CreateDirectory(_filePath);

            var fullPath = Path.Combine(_filePath, file.FileName);
            await using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            var rows = ParseFilledBtRows(fullPath);

            var validationError = ValidateRows(rows);
            if (validationError != null)
                return (0, validationError);

            // DELETE existing + INSERT new batch in one transaction (no separate ClearData SP)
            var count = await _repository.UploadBtFileRowsAsync(rows, uploadedBy);

            return count == 0
                ? (0, "0 rows uploaded. Please check the file.")
                : (count, null);
        }

        // ── Step 3 : Letter data ──────────────────────────────

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
                    var sent = await SendMailAsync(row, letterType);
                    if (sent)
                    {
                        await _repository.UpdateMailStatusAsync(row.LAN, letterType, sentBy);
                        result.MailsSent++;
                    }
                    else
                    {
                        result.Errors.Add($"LAN {row.LAN}: No email found for '{row.PrevFinancierName}'.");
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"LAN {row.LAN}: {ex.Message}");
                }
            }

            return result;
        }

        // ── Email Master ──────────────────────────────────────

        public async Task<List<BTEmailMaster>> GetAllEmailMasterAsync()
        {
            var items = await _repository.GetAllEmailMasterAsync();
            return items.ToList();
        }

        public async Task<(int RowsUploaded, string? Error)> UploadEmailMasterAsync(IFormFile file, int uploadedBy)
        {
            var allowed = new[] { ".xls", ".xlsx" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowed.Contains(ext))
                return (0, "Unsupported file type. Upload .xls or .xlsx.");

            var rows = ParseEmailMasterRows(file);

            if (!rows.Any())
                return (0, "No valid rows found in the file.");

            var count = 0;
            foreach (var row in rows)
            {
                var result = await _repository.UpsertEmailMasterAsync(row, uploadedBy);
                if (result == 1) count++;
            }

            return count == 0
                ? (0, "0 rows uploaded. Please check the file.")
                : (count, null);
        }

        private static List<BTEmailMasterUploadDto> ParseEmailMasterRows(IFormFile file)
        {
            var list = new List<BTEmailMasterUploadDto>();

            using var stream = file.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var bankName = row.Cell(1).GetValue<string>();
                var emailId = row.Cell(2).GetValue<string>();

                if (string.IsNullOrWhiteSpace(bankName) || string.IsNullOrWhiteSpace(emailId))
                    continue;

                list.Add(new BTEmailMasterUploadDto
                {
                    Bank_Name = bankName.Trim(),
                    Email_ID = emailId.Trim()
                });
            }

            return list;
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

                list.Add(new BTUploadFormatRowDto
                {
                    LAN = lan,
                    CustomerName = row.Cell(2).GetValue<string>(),
                    BranchCode = row.Cell(3).GetValue<int>(),       // int
                    DisbDate = row.Cell(4).GetValue<DateTime>(),
                    PrevFinancier = row.Cell(5).GetValue<string>(),
                    BtLan = row.Cell(6).GetValue<string>(),
                    InstrumentType = row.Cell(7).GetValue<string>(),
                    InstrumentNumber = row.Cell(8).GetValue<string>(),
                    InstrumentDate = row.Cell(9).GetValue<string>(),
                    InstrumentBank = row.Cell(10).GetValue<string>(),
                    InstrumentAmount = row.Cell(11).GetValue<string>()    // varchar
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

        private async Task<bool> SendMailAsync(BTListItem row, string letterType)
        {
            var emailId = await _repository.GetBankEmailIdAsync(row.PrevFinancierName ?? "");
            if (string.IsNullOrWhiteSpace(emailId))
                return false;

            var msg = new MailMessage
            {
                From = new MailAddress("notification@vistaarfinance.com", "VISTAAR"),
                Priority = MailPriority.High,
                IsBodyHtml = true
            };
            msg.To.Add(emailId);
            msg.CC.Add("cops.ckyc@vistaarfinance.com");

            if (letterType is "BT-1" or "BT-2")
            {
                msg.Subject = $"Take - Over Of Loan facility Of Mr. /Mrs. /Ms. {row.CustomerName}";
                msg.Body = BuildBt1Or2Body(row);
            }
            else
            {
                msg.Subject = $"Take - Over Of Loan facility Of Mr. /Mrs. /Ms. {row.CustomerName} **Reminder 2";
                msg.Body = "Referring to the RBI notification stated in the previous mail we would be reporting "
                            + "the same to RBI Ombudsman since the customer has officially permitted us to collect "
                            + "the property documents from your bank within 30 days of disbursement of our loan to the customer.";
            }

            msg.Body += BuildSignature();

            using var client = new SmtpClient(
                _config["Smtp:Host"] ?? "smtp.office365.com",
                int.Parse(_config["Smtp:Port"] ?? "587"))
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    _config["Smtp:User"],
                    _config["Smtp:Password"]),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            await client.SendMailAsync(msg);
            return true;
        }

        private static string BuildBt1Or2Body(BTListItem row)
        {
            var sb = new StringBuilder();
            sb.Append("Dear Sir/Madam<br /><br />");
            sb.Append($"This is in reference to Cheque/DD/pay order issued in your favor towards full &amp; final /Partial settlement "
                    + $"of all loan facilities availed by Mr. /Mrs. /Ms. {row.CustomerName} with your bank. "
                    + "The details of the instruments are as below, <strong>we are yet to receive the set of property documents mortgaged with you.</strong> "
                    + "We request you to share the Cersai Satisfaction transaction id /Asset Ids for the linked property/s along with NOC/complete set "
                    + "of documents within 2 days of receipt of this letter.");
            sb.Append($"<br /><br />Appreciate if you could kindly expedite and sensitize the release of Collaterals held by you against "
                    + $"the loan of Mr. /Mrs. /Ms. {row.CustomerName}. For any further clarification please feel free to contact the undersigned.");
            sb.Append($"<br /><br />Customer loan A/C number in your bank was <strong>{row.BT_LAN}</strong><br /><br />");

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

        private static string BuildSignature() =>
            "<br /><br />Thanking you.<br /><br /><br />"
          + "<b>Mayank Vajpeyee<br />Head of Central Operations<br />"
          + "Vistaar Financial Services Private Limited<br />"
          + "Email: mayank.vajpeyee@vistaarfinance.com</b>";
    }
}