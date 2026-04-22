using ClosedXML.Excel;
using System.Data;

namespace ProductivityApi.Helpers
{
    public static class ExcelHelper
    {
        public static byte[]? GenerateExcel(IEnumerable<dynamic> data, string sheetName = "Data")
        {
            if (data == null || !data.Any())
                return null;

            using var workbook = new XLWorkbook();
            AddSheet(workbook, data, sheetName);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static byte[]? GenerateExcel(params (IEnumerable<dynamic> Data, string SheetName)[] sheets)
        {
            using var workbook = new XLWorkbook();

            foreach (var (data, sheetName) in sheets)
            {
                if (data != null && data.Any())
                    AddSheet(workbook, data, sheetName);
            }

            if (!workbook.Worksheets.Any())
                return null;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void AddSheet(XLWorkbook workbook, IEnumerable<dynamic> data, string sheetName)
        {
            var list = data?.ToList();
            if (list == null || list.Count == 0)
                return;

            var first = list.First();

            if (first is not IDictionary<string, object> firstDict)
                throw new InvalidOperationException("Data must be ExpandoObject or IDictionary<string, object>");

            var columns = firstDict.Keys.ToList();

            var dt = new DataTable();

            foreach (var col in columns)
                dt.Columns.Add(col);

            foreach (var row in list)
            {
                if (row is not IDictionary<string, object> dict)
                    throw new InvalidOperationException("All rows must be ExpandoObject or IDictionary<string, object>");

                var values = columns.Select(c => dict.ContainsKey(c) ? dict[c] : null).ToArray();
                dt.Rows.Add(values);
            }

            var ws = workbook.Worksheets.Add(sheetName);

            var table = ws.Cell(1, 1).InsertTable(dt);

            // Styling
            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            ws.Row(1).Style.Font.FontColor = XLColor.White;

            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();
        }
    }
}