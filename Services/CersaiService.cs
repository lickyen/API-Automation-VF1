using ClosedXML.Excel;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using System.Data;

namespace ProductivityApi.Services
{
    public class CersaiService
    {
        private readonly CersaiRepository _repository;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly TemplateService _templateService;
        private readonly UserService _userService;

        public CersaiService(CersaiRepository repository, EmailService emailService, SmsService smsService, TemplateService templateService, UserService userService)
        {
            _repository = repository;
            _emailService = emailService;
            _templateService = templateService;
            _userService = userService;
        }

        public async Task<IEnumerable<CersaiModel>?> GetPendingAsync(string type)
        {
            return type.ToUpper() switch
            {
                "CC" => await _repository.GetPendingCCAsync(),
                "CS" => await _repository.GetPendingCSAsync(),
                _ => throw new ArgumentException("Invalid Input", nameof(type))
            };
        }

        public async Task<int> ChargeCreationAsync(ChargeCreationDto dto)
        {
            var result = await _repository.ChargeCreationAsync(dto);
            return result;
        }

        public async Task<int> ChargeSatisfactionAsync(ChargeSatisfactionDto dto)
        {
            var result = await _repository.ChargeSatisfactionAsync(dto);
            return result;
        }

        public async Task<MemoryStream?> GenerateReportExcelAsync()
        {
            var data = await _repository.GetReportDataAsync();

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
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Cersai Report");
            worksheet.Cell(1, 1).InsertTable(dt);

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return stream;
        }


    }
}
