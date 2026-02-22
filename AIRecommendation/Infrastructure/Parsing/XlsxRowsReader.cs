using ClosedXML.Excel;

namespace AIRecommendation.Infrastructure.Parsing;

public static class XlsxRowsReader
{
    public static IEnumerable<Dictionary<string, string>> ReadRows(string path, string? sheetName = null)
    {
        using var wb = new XLWorkbook(path);
        var ws = sheetName == null ? wb.Worksheets.First() : wb.Worksheet(sheetName);

        var range = ws.RangeUsed();
        if (range is null) yield break;

        var headerRow = range.FirstRowUsed();
        var headers = headerRow.Cells().Select(c => c.GetValue<string>().Trim()).ToList();

        foreach (var row in range.RowsUsed().Skip(1))
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                var key = headers[i];
                if (string.IsNullOrWhiteSpace(key)) continue;

                dict[key] = row.Cell(i + 1).GetValue<string>()?.Trim() ?? "";
            }

            yield return dict;
        }
    }
}