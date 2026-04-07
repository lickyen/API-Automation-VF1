using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2013.Excel;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Data;


namespace ProductivityApi.Services
{
    public class BranchVisitService
    {
        private readonly BranchVisitRepository _repository;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;

        public BranchVisitService(BranchVisitRepository repository, EmailService emailService, SmsService smsService, TemplateService templateService, UserService userService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
        }

        public async Task<BranchVisitDto> GetVisitByIdAsync(int visitId)
        {
            return await _repository.GetVisitByIdAsync(visitId);
        }

        public async Task<List<BranchVisitChecklist>> GetChecklistAsync(string section)
        {
            return await _repository.GetChecklistAsync(section);
        }

        public async Task<List<BranchVisit>> GetVisitsByUserAsync( string userId,  string month, string? status)
        {
            return await _repository.GetVisitsByUserAsync(userId, month, status);
        }

        public async Task<int> UpdateBranchVisitAsync(BranchVisitSubmitDto dto)
        {
            var result = await _repository.UpdateBranchVisitAsync(dto);
            return result;
        }

        public async Task<string?> GenerateReportExcelBase64Async(string month)
        {
            var data = await _repository.GetReportDataAsync(month);

            if (data == null || !data.Any())
                return null;

            // Convert to DataTable
            var dt = new DataTable();
            foreach (var col in ((IDictionary<string, object>)data.First()).Keys)
            {
                dt.Columns.Add(col);
            }

            foreach (var row in data)
            {
                var dict = (IDictionary<string, object>)row;
                dt.Rows.Add(dict.Values.ToArray());
            }

            // Generate Excel
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Report");
            worksheet.Cell(1, 1).InsertTable(dt);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var bytes = stream.ToArray();
            var base64 = Convert.ToBase64String(bytes);

            return base64;
        }
    }
}
