using ClosedXML.Excel;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class BulkDocsService
    {
        private readonly BulkDocsRepository _repository;

        public BulkDocsService(BulkDocsRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> UploadAsync(IFormFile file, string createdBy)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                throw new ArgumentException("Invalid file type. Please upload an Excel file.");

            List<BulkDocItem> items;

            using (var stream = file.OpenReadStream())
            {
                items = ReadItemsFromExcel(stream);
            }

            if (items.Count == 0)
                throw new ArgumentException("No records found in the uploaded file.");

            return await _repository.BulkInsertAsync(items, createdBy);
        }

        private List<BulkDocItem> ReadItemsFromExcel(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);
            var items = new List<BulkDocItem>();

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var lan = row.Cell(1).GetString().Trim();

                if (string.IsNullOrWhiteSpace(lan))
                    continue;

                items.Add(new BulkDocItem
                {
                    Lan = lan,
                    DocType = row.Cell(2).GetString().Trim(),
                    FromDate = row.Cell(3).GetDateTime(),
                    ToDate = row.Cell(4).GetDateTime()
                });
            }

            return items;
        }

        public async Task<IEnumerable<BulkDocsModel>> GetLanListAsync(BulkDocListFilter filter)
        {
            return await _repository.GetLanListAsync(filter);
        }
    }
}