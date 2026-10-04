using ClosedXML.Excel;
using System.Globalization;
using System.Text;

namespace Ims.Api.Features.Import;

internal static class SpreadsheetReader
{
    internal sealed record SheetRow(int RowNo, IReadOnlyList<string> Cells)
    {
        public bool IsBlank => Cells.All(string.IsNullOrWhiteSpace);

        public string Cell(int index) =>
            index >= 0 && index < Cells.Count ? Cells[index].Trim() : "";
    }

    internal sealed record Sheet(IReadOnlyList<string> Header, IReadOnlyList<SheetRow> Rows);

    internal static bool IsSupported(string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        return ext is ".xlsx" or ".csv";
    }

    internal static Sheet Read(Stream stream, string fileName) =>
        Path.GetExtension(fileName).Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? ReadCsv(stream)
            : ReadXlsx(stream);


    private static Sheet ReadXlsx(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();
        var used = ws.RangeUsed();
        if (used is null) return new Sheet([], []);

        var lastCol = used.LastColumn().ColumnNumber();
        var lastRow = used.LastRow().RowNumber();

        var header = new List<string>();
        for (var c = 1; c <= lastCol; c++) header.Add(Value(ws.Cell(1, c)));

        var rows = new List<SheetRow>();
        for (var r = 2; r <= lastRow; r++)
        {
            var cells = new List<string>(lastCol);
            for (var c = 1; c <= lastCol; c++) cells.Add(Value(ws.Cell(r, c)));
            rows.Add(new SheetRow(r, cells));
        }
        return new Sheet(header, rows);
    }

    private static string Value(IXLCell cell) => cell.DataType switch
    {
        XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        XLDataType.Number => cell.GetDouble().ToString("0.####", CultureInfo.InvariantCulture),
        XLDataType.Blank => "",
        _ => cell.GetString(),
    };


    private static Sheet ReadCsv(Stream stream)
    {
        using var sr = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = sr.ReadToEnd();

        var lines = SplitRecords(text);
        if (lines.Count == 0) return new Sheet([], []);

        var header = lines[0];
        var rows = new List<SheetRow>();
        for (var i = 1; i < lines.Count; i++) rows.Add(new SheetRow(i + 1, lines[i]));
        return new Sheet(header, rows);
    }

    private static List<List<string>> SplitRecords(string text)
    {
        var records = new List<List<string>>();
        var row = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (inQuotes)
            {
                if (ch != '"') { sb.Append(ch); continue; }
                if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                else inQuotes = false;
                continue;
            }

            switch (ch)
            {
                case '"': inQuotes = true; break;
                case ',': row.Add(sb.ToString()); sb.Clear(); break;
                case '\r': break;
                case '\n':
                    row.Add(sb.ToString()); sb.Clear();
                    records.Add(row); row = [];
                    break;
                default: sb.Append(ch); break;
            }
        }

        if (sb.Length > 0 || row.Count > 0) { row.Add(sb.ToString()); records.Add(row); }
        if (records.Count > 0 && records[^1].Count == 1 && records[^1][0].Length == 0)
            records.RemoveAt(records.Count - 1);
        return records;
    }
}
