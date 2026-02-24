/*using System.Text;
using AIRecommendation.Application.Interfaces;
using AIRecommendation.Infrastructure.Parsing;
using ClosedXML.Excel;

namespace AIRecommendation.Infrastructure.Parsing;

public class XlsxDocumentParser : IDocumentParser
{
    public bool CanParse(string extension)
        => extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(string path)
    {
        var sb = new StringBuilder();

        using var wb = new XLWorkbook(path);
        foreach (var ws in wb.Worksheets)
        {
            sb.AppendLine($"[Sheet: {ws.Name}]");

            var range = ws.RangeUsed();
            if (range is null) continue;

            foreach (var row in range.RowsUsed())
            {
                var values = row.Cells()
                    .Select(c => c.GetValue<string>()?.Trim() ?? "")
                    .Where(v => !string.IsNullOrWhiteSpace(v));

                var line = string.Join(" | ", values);
                if (!string.IsNullOrWhiteSpace(line))
                    sb.AppendLine(line);
            }

            sb.AppendLine();
        }

        return Task.FromResult(sb.ToString());
    }
} */