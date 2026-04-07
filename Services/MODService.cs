using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ProductivityApi.Models;
using ProductivityApi.Repository;
using ProductivityApi.Services;
using System.Data;
using System.Xml.Linq;

namespace ProductivityApi.Services
{
    public class ModService
    {
        private readonly ModRepository _repository;
        private readonly string _filePath;

        public ModService(ModRepository repository, AppSettings appSettings)
        {
            _repository = repository;
            _filePath = appSettings.FilePaths.ModLettersFilePath;

        }

        public async Task<(bool Success, string Message)> UploadHrDataAsync(UploadHrDataDto dto)
        {
            var dt = await ReadExcelAsync(dto.File);

            if (!ValidateData(dt))
                return (false, "Data validation failed. Please check the file.");

            // Delete old data in DB
            await _repository.DeleteExistingDataAsync(dto.Month);
            await _repository.InsertDataAsync(dt, dto);

            // Save uploaded Excel file
            var excelFileName = $"MOD_{dto.Month}.xlsx";
            var excelFilePath = Path.Combine(_filePath, excelFileName);
            using (var stream = new FileStream(excelFilePath, FileMode.Create, FileAccess.Write))
            {
                await dto.File.CopyToAsync(stream);
            }

            var eligibleEmployees = await _repository.GetEligibleListAsync(dto.Month);

            GeneratePdf(dto.Month, eligibleEmployees);

            return (true, "Upload successful");
        }

        public async Task<List<MODEligibleDto>> GetEligibleListAsync(string month)
        {
            return await _repository.GetEligibleListAsync(month);
        }

        public async Task<List<LetterHistoryDto>> GetLetterHistoryByEmpIdAsync(string empId)
        {
            return await _repository.GetLetterHistoryByEmpIdAsync(empId);
        }

        public async Task<DownloadModDto> GetModLetterAsync(string month, bool signed)
        {
            var fileName = signed? $"MOD_{month}_Signed.pdf": $"MOD_{month}.pdf";
            var filePath = Path.Combine(_filePath, fileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.");

            var fileBytes = await File.ReadAllBytesAsync(filePath);

            return new DownloadModDto
            {
                FileName = fileName,
                FileBytes = fileBytes
            };
        }
        public void GeneratePdf(string month, List<MODEligibleDto> employees)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using (var document = new Document(PageSize.A4, 70f, 70f, 70f, 70f))
                {
                    var writer = PdfWriter.GetInstance(document, memoryStream);
                    document.Open();

                    // Fonts
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, Font.UNDERLINE);
                    var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, Font.NORMAL);
                    var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                    var smallBoldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);

                    // Title
                    var titleParagraph = new Paragraph("TO WHOMSOEVER IT MAY CONCERN", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER
                    };
                    document.Add(titleParagraph);
                    document.Add(new Paragraph("\n"));

                    // Paragraphs
                    document.Add(CreateParagraph(
                        "I, Mr. Soumyajyoti Roychoudhury, holding the office of Chief Operating Officer of Vistaar Financial Services Private Limited, a Company incorporated and registered under the Companies Act, 1956, and having its registered office at Plot No. 59 & 60-23, 22nd Cross, 29th Main, BTM Layout, 2nd Stage, Bengaluru - 560076 (Hereinafter ",
                        "\"the Company\"",
                        ").",
                        normalFont,
                        boldFont));

                    document.Add(new Paragraph("\n"));

                    document.Add(CreateParagraph("WHEREAS ", null, "I am holding the office of Chief Operating Officer of the Company;", normalFont, boldFont));

                    document.Add(new Paragraph("\n"));

                    document.Add(CreateParagraph("AND WHEREAS ", null,
                        "as per the Board Resolution dated 28th May, 2025, I am empowered to appoint, any person to be the attorney or agent of the Company with such powers as may be requisite in relation to the functions of the Company.",
                        normalFont, boldFont));

                    document.Add(new Paragraph("\n"));

                    document.Add(CreateParagraph("AND WHEREAS ", null,
                        "for the purpose of efficiently carrying out the management of the office and various branches of the Company, and to act on behalf of the Company, it is desirable to confer certain powers and authorities on certain employees of the Company.",
                        normalFont, boldFont));

                    document.Add(new Paragraph("\n"));

                    document.Add(CreateParagraph("NOW KNOWN ALL BY THESE PRESENTS, ", null,
                        "that I Soumyajyoti Roychoudhury, in exercise of and in pursuance of powers vested in me by virtue of Board Resolution dated 28th May, 2025, DO HEREBY nominate, constitute and appoint the following employee of the Company viz.",
                        normalFont, boldFont));

                    document.Add(new Paragraph("\n"));

                    // Employee table
                    AddEmployees(document, employees, boldFont, normalFont);

                    document.Add(new Paragraph("\n"));

                    // Paragraphs after table
                    document.Add(new Paragraph(
                        "and confer upon him the following powers and authorities which shall be exercised by him independently and singly by carrying his original Employee Identity Card failing which nothing shall be binding on the Company;",
                        normalFont)
                    { Alignment = Element.ALIGN_JUSTIFIED });

                    document.Add(new Paragraph("\n"));

                    document.Add(CreateParagraph("1. ", "\t", "To make, sign, seal, execute, deliver and endorse all/any agreements, contracts and receipts, assurance, registration of mortgages and other like instruments with customers of Vistaar Financial Services Private Limited only relating to loans given by the Company.",
                        normalFont, boldFont));

                    document.Add(new Paragraph("\n"));

                    document.Add(CreateParagraph("2. ", "\t", "This authority letter shall be effective from the date of its execution and shall remain in effect and be valid only till the time the employee remains in the employment of the Company (or) the earlier termination or supersession of the authority by the company \"whichever event shall occur earlier.\"",
                        normalFont, boldFont));

                    document.Add(new Paragraph("\n"));

                    // Signature section
                    document.Add(new Paragraph("Issued at Bengaluru, Karnataka this ___ day of __________, 2025.", smallBoldFont));
                    document.Add(new Paragraph("\n"));
                    document.Add(new Paragraph("For Vistaar Financial Services Private Limited", boldFont) { SpacingAfter = 50 });
                    document.Add(new Paragraph("Soumyajyoti Roychoudhury", boldFont));
                    document.Add(new Paragraph("Chief Operating Officer", boldFont));

                    document.Close();

                    // Save PDF
                    var fileName = $"MOD_{month}.pdf";
                    var fullFilePath = Path.Combine(_filePath, fileName);
                    File.WriteAllBytes(fullFilePath, memoryStream.ToArray());
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (logging)
                throw new ApplicationException("Error generating MOD PDF", ex);
            }
        }

        public async Task<(bool Success, string Message)> UploadSignedModAsync(UploadSignedModDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return (false, "Invalid file.");

            var fileName = $"MOD_{dto.Month}_Signed.pdf";
            var filePath = Path.Combine(_filePath, fileName);

            if (!Directory.Exists(_filePath))
                Directory.CreateDirectory(_filePath);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await dto.File.CopyToAsync(stream);
            }

            // Update MOD_EligibleList after signed MOD upload
            await _repository.UpdateLetterIssuedAsync(dto.CreatedBy);

            return (true, "Upload successful.");
        }
        private Paragraph CreateParagraph(string startText, string boldText, string endText, Font normalFont, Font boldFont)
        {
            var paragraph = new Paragraph();
            paragraph.Add(new Chunk(startText ?? string.Empty, normalFont));
            if (!string.IsNullOrEmpty(boldText))
                paragraph.Add(new Chunk(boldText, boldFont));
            paragraph.Add(new Chunk(endText ?? string.Empty, normalFont));
            paragraph.Alignment = Element.ALIGN_JUSTIFIED;
            return paragraph;
        }
        private void AddEmployees(Document document, List<MODEligibleDto> employees, Font boldFont, Font normalFont)
        {
            if (employees == null || employees.Count == 0) return;

            // Define table columns (adjust according to DTO)
            var pdfTable = new PdfPTable(4)
            {
                WidthPercentage = 100 // table width = 100% of page width
            };

            // Set relative column widths
            pdfTable.SetWidths(new float[] { 1.5f, 2f, 2f, 2f });

            // Add header row
            pdfTable.AddCell(new PdfPCell(new Phrase("Employee ID", boldFont)) { Padding = 7, HorizontalAlignment = Element.ALIGN_CENTER });
            pdfTable.AddCell(new PdfPCell(new Phrase("Name", boldFont)) { Padding = 7, HorizontalAlignment = Element.ALIGN_CENTER });
            pdfTable.AddCell(new PdfPCell(new Phrase("Designation", boldFont)) { Padding = 7, HorizontalAlignment = Element.ALIGN_CENTER });
            pdfTable.AddCell(new PdfPCell(new Phrase("Appointment Date", boldFont)) { Padding = 7, HorizontalAlignment = Element.ALIGN_CENTER });

            // Add employee rows
            foreach (var emp in employees)
            {
                pdfTable.AddCell(new PdfPCell(new Phrase(emp.EmpId ?? string.Empty, normalFont)) { Padding = 7 });
                pdfTable.AddCell(new PdfPCell(new Phrase(emp.Name ?? string.Empty, normalFont)) { Padding = 7 });
                pdfTable.AddCell(new PdfPCell(new Phrase(emp.Designation ?? string.Empty, normalFont)) { Padding = 7 });
                pdfTable.AddCell(new PdfPCell(new Phrase(emp.AppointmentDate?.ToString() ?? string.Empty, normalFont)) { Padding = 7 });
            }

            document.Add(pdfTable);
        }
        private async Task<DataTable> ReadExcelAsync(IFormFile file)
        {
            var dt = new DataTable();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            // Assume first row is header
            var headerRow = worksheet.Row(1);

            // Add columns from header
            foreach (var cell in headerRow.CellsUsed())
                dt.Columns.Add(cell.GetString().Trim());

            // Start from second row (skip header)
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                // Skip row if all cells are empty
                if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
                    continue;

                var newRow = dt.NewRow();
                for (int i = 0; i < dt.Columns.Count; i++)
                    newRow[i] = row.Cell(i + 1).GetString().Trim();  // <-- fixed here

                dt.Rows.Add(newRow);
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
