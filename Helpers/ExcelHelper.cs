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

        //private static void AddSheet(XLWorkbook workbook, IEnumerable<dynamic> data, string sheetName)
        //{
        //    var list = data?.ToList();
        //    if (list == null || list.Count == 0)
        //        return;

        //    var first = list.First();

        //    if (first is not IDictionary<string, object> firstDict)
        //        throw new InvalidOperationException("Data must be ExpandoObject or IDictionary<string, object>");

        //    var columns = firstDict.Keys.ToList();

        //    var dt = new DataTable();

        //    foreach (var col in columns)
        //        dt.Columns.Add(col);

        //    foreach (var row in list)
        //    {
        //        if (row is not IDictionary<string, object> dict)
        //            throw new InvalidOperationException("All rows must be ExpandoObject or IDictionary<string, object>");

        //        var values = columns.Select(c => dict.ContainsKey(c) ? dict[c] : null).ToArray();
        //        dt.Rows.Add(values);
        //    }

        //    var ws = workbook.Worksheets.Add(sheetName);

        //    var table = ws.Cell(1, 1).InsertTable(dt);

        //    // Styling
        //    ws.Row(1).Style.Font.Bold = true;
        //    ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
        //    ws.Row(1).Style.Font.FontColor = XLColor.White;

        //    ws.SheetView.FreezeRows(1);
        //    ws.Columns().AdjustToContents();
        //}

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
            {
                // Detect column type from first non-null value across all rows
                var colType = DetectColumnType(list, col);
                dt.Columns.Add(col, colType);
            }

            foreach (var row in list)
            {
                if (row is not IDictionary<string, object> dict)
                    throw new InvalidOperationException("All rows must be ExpandoObject or IDictionary<string, object>");

                var values = columns.Select(c =>
                {
                    var val = dict.ContainsKey(c) ? dict[c] : null;
                    return CoerceValue(val, dt.Columns[c]!.DataType);
                }).ToArray();

                dt.Rows.Add(values);
            }

            var ws = workbook.Worksheets.Add(sheetName);
            ws.Cell(1, 1).InsertTable(dt);

            // Apply date format to DateTime columns
            for (int i = 0; i < columns.Count; i++)
            {
                if (dt.Columns[i].DataType == typeof(DateTime))
                {
                    // i+1 because ClosedXML columns are 1-indexed, +1 row for header
                    ws.Column(i + 1)
                      .Cells(2, list.Count + 1)
                      .Style.NumberFormat.Format = "dd-MM-yyyy";
                }
            }

            // Styling
            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            ws.Row(1).Style.Font.FontColor = XLColor.White;
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();
        }

        // Scan all rows to find first non-null value and infer its type
        private static Type DetectColumnType(List<dynamic> rows, string col)
        {
            foreach (var row in rows)
            {
                if (row is IDictionary<string, object> dict &&
                    dict.TryGetValue(col, out var val) && val != null)
                {
                    var t = val.GetType();
                    if (t == typeof(DateTime)) return typeof(DateTime);
                    if (t == typeof(int)) return typeof(int);
                    if (t == typeof(long)) return typeof(long);
                    if (t == typeof(decimal)) return typeof(decimal);
                    if (t == typeof(double)) return typeof(double);
                    if (t == typeof(float)) return typeof(float);
                    if (t == typeof(bool)) return typeof(bool);
                    return typeof(string);
                }
            }
            return typeof(string);
        }

        // Safely cast value to the target DataTable column type
        private static object CoerceValue(object? val, Type targetType)
        {
            if (val == null) return DBNull.Value;

            try
            {
                if (targetType == typeof(DateTime))
                {
                    if (val is DateTime dt) return dt;
                    if (DateTime.TryParse(val.ToString(), out var parsed)) return parsed;
                    return DBNull.Value;
                }
                return Convert.ChangeType(val, targetType);
            }
            catch
            {
                return DBNull.Value;
            }
        }
    }
}