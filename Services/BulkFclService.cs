using ClosedXML.Excel;
using ProductivityApi.Models;
using ProductivityApi.Repository;

namespace ProductivityApi.Services
{
    public class BulkFclService
    {
        private readonly BulkFclRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly string _filePath;

        public BulkFclService(BulkFclRepository repository, IConfiguration configuration, AppSettings appSettings)
        {
            _repository = repository;
            _configuration = configuration;
            _filePath = appSettings.FilePaths.BulkFclFilePath;
        }

        public async Task<BulkFclCreateResult> UploadAsync(IFormFile file, string createdBy)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                throw new ArgumentException("Invalid file type. Please upload an Excel file.");

            List<string> lans;

            using (var stream = file.OpenReadStream())
            {
                lans = ReadLansFromExcel(stream);
            }

            if (lans.Count == 0)
                throw new ArgumentException("No LANs found in the uploaded file.");

            // Save only the original filename for audit purposes (optional)
            var batchId = await _repository.CreateBatchAsync(file.FileName, createdBy);
            var inserted = await _repository.InsertLansAsync(batchId, lans, createdBy);

            return new BulkFclCreateResult
            {
                BatchId = batchId,
                LansInserted = inserted
            };
        }
        private List<string> ReadLansFromExcel(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);
            var lans = new List<string>();

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var lan = row.Cell(1).GetString().Trim();

                if (!string.IsNullOrWhiteSpace(lan))
                    lans.Add(lan);
            }

            return lans;
        }
        public async Task<IEnumerable<BulkFclBatchModel>> GetBatchesAsync(BulkFclListFilter filter)
        {
            filter ??= new BulkFclListFilter();
            return await _repository.GetBatchesAsync(filter);
        }

        public async Task<IEnumerable<BulkFclLanModel>> GetLansByBatchAsync(long batchId)
        {
            return await _repository.GetLansByBatchAsync(batchId);
        }

        //public async Task UpdateValuesAsync(BulkFclUpdateValuesDto dto)
        //{
        //    await _repository.UpdateValuesAsync(dto);
        //}

        public async Task<(byte[] Content, string FileName)> DownloadBatchAsync(long batchId)
        {
            var rows = (await _repository.GetDownloadDataAsync(batchId)).ToList();
            if (rows.Count == 0)
                throw new InvalidOperationException("No data found for this batch.");

            await _repository.UpdateStatusAsync(batchId, "COMPLETED");

            if (!Directory.Exists(_filePath))
                Directory.CreateDirectory(_filePath);

            var fileName = $"Batch_{batchId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            var fullPath = Path.Combine(_filePath, fileName);

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("BulkFCL");

                string[] headers =
                {
                    "BatchId","Lan","FetchDate","LoanAmt","DisbAmt","CustId","CustName",
                    "PrincipalOut","PrincipalDue","InterestDue","FCCharges","BPI","LPI",
                    "OtherCharges","ExcessAmt","TotalAmt","PerDayBPI"
                };
                for (var c = 0; c < headers.Length; c++)
                {
                    ws.Cell(1, c + 1).Value = headers[c];
                    ws.Cell(1, c + 1).Style.Font.Bold = true;
                }

                var r = 2;
                foreach (var row in rows)
                {
                    ws.Cell(r, 1).Value = row.BatchId;
                    ws.Cell(r, 2).Value = row.Lan;
                    ws.Cell(r, 3).Value = row.FetchDate;
                    ws.Cell(r, 4).Value = row.LoanAmt;
                    ws.Cell(r, 5).Value = row.DisbAmt;
                    ws.Cell(r, 6).Value = row.CustId;
                    ws.Cell(r, 7).Value = row.CustName;
                    ws.Cell(r, 8).Value = row.PrincipalOut;
                    ws.Cell(r, 9).Value = row.PrincipalDue;
                    ws.Cell(r, 10).Value = row.InterestDue;
                    ws.Cell(r, 11).Value = row.FCCharges;
                    ws.Cell(r, 12).Value = row.BPI;
                    ws.Cell(r, 13).Value = row.LPI;
                    ws.Cell(r, 14).Value = row.OtherCharges;
                    ws.Cell(r, 15).Value = row.ExcessAmt;
                    ws.Cell(r, 16).Value = row.TotalAmt;
                    ws.Cell(r, 17).Value = row.PerDayBPI;
                    r++;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(fullPath);
            }

            await _repository.UpdateBatchFileAsync(batchId, fileName);

            var bytes = await File.ReadAllBytesAsync(fullPath);
            return (bytes, fileName);
        }
    }
}