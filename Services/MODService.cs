using ClosedXML.Excel;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using System.Data;

namespace ProductivityApi.Services
{
    public class ModService
    {
        private readonly ModRepository _repository;

        public ModService(ModRepository repository)
        {
            _repository = repository;
        }

        public async Task<(bool Success, string Message)> UploadModDataAsync(IFormFile file, string month)
        {
            var dt = await ReadExcelAsync(file);

            if (!ValidateData(dt))
                return (false, "Data validation failed. Please check the file.");

            await _repository.DeleteExistingDataAsync(month);
            await _repository.InsertDataAsync(dt, month);

            return (true, "Upload successful");
        }

        private async Task<DataTable> ReadExcelAsync(IFormFile file)
        {
            var dt = new DataTable();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            bool firstRow = true;

            foreach (var row in worksheet.RowsUsed())
            {
                if (firstRow)
                {
                    foreach (var cell in row.Cells())
                        dt.Columns.Add(cell.GetString());

                    firstRow = false;
                }
                else
                {
                    var newRow = dt.NewRow();
                    for (int i = 0; i < dt.Columns.Count; i++)
                        newRow[i] = row.Cell(i + 1).Value.ToString();

                    dt.Rows.Add(newRow);
                }
            }

            return dt;
        }

        private bool ValidateData(DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
            {
                if (IsInvalidGroup(row, "CM") ||
                    IsInvalidGroup(row, "BM") ||
                    IsInvalidGroup(row, "OPS"))
                    return false;
            }

            return true;
        }

        private bool IsInvalidGroup(DataRow row, string prefix)
        {
            var empId = row[$"{prefix} EMPID"]?.ToString();
            var name = row[$"{prefix} NAME"]?.ToString();
            var desig = row[$"{prefix} DESIGNATION"]?.ToString();
            var appt = row[$"{prefix} APPOINTMENT DATE"]?.ToString();

            if (string.IsNullOrEmpty(empId) &&
                (!string.IsNullOrEmpty(name) ||
                 !string.IsNullOrEmpty(desig) ||
                 !string.IsNullOrEmpty(appt)))
                return true;

            if (!string.IsNullOrEmpty(empId) &&
                (string.IsNullOrEmpty(name) ||
                 string.IsNullOrEmpty(desig) ||
                 string.IsNullOrEmpty(appt)))
                return true;

            return false;
        }
    }
}


