using ClosedXML.Excel;
using ProductivityApi.Models;
using ProductivityApi.Repositories;
using System.Data;

namespace ProductivityApi.Services
{
    public class SettlementService
    {
        private readonly SettlementRepository _repository;
        private readonly EmailService _emailService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;

        public SettlementService(SettlementRepository repository, EmailService emailService, TemplateService templateService, UserService userService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
        }

        // Create a new settlement request
        public async Task<int> CreateSettlementAsync(CreateSettlementDto dto)
        {
            var result = await _repository.CreateSettlementAsync(dto);

            return result;
        }

        // Submit settlement request to COPS
        public async Task<int> SubmitSettlementAsync(SubmitSettlementDto dto)
        {
            var result = await _repository.SubmitSettlementAsync(dto);
            return result;
        }

        // Add payment
        public async Task<int> AddPaymentAsync(SettlementAddPaymentDto dto)
        {
            var result =  await _repository.AddPaymentAsync(dto);
            return result;
        }

        // Get settlement details with logs and payments
        public async Task<SettlementTransaction?> GetSettlementByIdAsync(int id)
        {
            var result = await _repository.GetSettlementByIdAsync(id);
            return result;
        }

        public async Task<int> ApproveSettlementAsync(ApproveSettlementDto dto)
        {
            var result = await _repository.ApproveSettlementAsync(dto);
            if (result >= 1)
            {
                var settlement = await GetSettlementByIdAsync(dto.Id);
                if (settlement != null) 
                {
                    var user =  _userService.GetUser(settlement.Created_By);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        var placeholders = new Dictionary<string, string>
                        {
                            { "Module", "Settlement" }, { "LAN", settlement.Lan}
                        };

                        var toList = new List<string> { user.Email };
                        var ccList = new List<string>();

                        string templateType = dto.Action switch
                        {
                            "Processed" => "ProcessedEmail",
                            "SentBack" => "MovedToQEmail",
                            "Rejected" => "RejectedEmail",
                            _ => ""
                        };

                        if (! string.IsNullOrEmpty(templateType))
                        {
                            var emailTemplate = _templateService.GetEmailTemplate(templateType);

                            var emailBody = _templateService.ReplacePlaceholders(emailTemplate.Body, placeholders);
                            await _emailService.SendEmail("Action Required - Settlement", emailBody, toList, ccList);
                        }
                    }
                }

            }
            return result;
        }

        public async Task<int> EditPaymentAsync(SettlementEditPaymentDto dto)
        {
            var result = await _repository.EditPaymentAsync(dto);
            return result;
        }

        public async Task<int> DeletePaymentAsync(int id)
        {
            var result = await _repository.DeletePaymentAsync(id);
            return result;
        }

        public async Task<IEnumerable<SettlementDashboard>> GetDashboardByUserAsync(SettlementDashboardRequestDto dto)
        {
            return await _repository.GetDashboardByUserAsync(dto);
        }

        public async Task<int> CancelSettlementAsync(CancelSettlementDto dto)
        {
            var result = await _repository.CancelSettlementAsync(dto);
            return result;
        }

        public async Task<byte[]?> GetReportExcelAsync(DateTime? fromDate, DateTime? toDate)
        {
            var (transactions, payments, logs) =
                await _repository.GetReportDataAsync(fromDate, toDate);

            using var workbook = new XLWorkbook();

            AddSheet(workbook, transactions, "Data");
            AddSheet(workbook, payments, "Payments");
            AddSheet(workbook, logs, "Logs");

            if (!workbook.Worksheets.Any())
                return null;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        private void AddSheet(XLWorkbook workbook, IEnumerable<dynamic> data, string sheetName)
        {
            if (data == null || !data.Any())
                return;

            var dt = new DataTable();
            var firstRow = (IDictionary<string, object>)data.First();

            foreach (var col in firstRow.Keys)
                dt.Columns.Add(col);

            foreach (var row in data)
            {
                var dict = (IDictionary<string, object>)row;
                dt.Rows.Add(dict.Values.ToArray());
            }

            var ws = workbook.Worksheets.Add(sheetName);
            ws.Cell(1, 1).InsertTable(dt);

            ws.Row(1).Style.Font.Bold = true;
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();
        }
    }
}
