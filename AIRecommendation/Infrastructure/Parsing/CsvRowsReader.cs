using System.Text;
using System.Text.RegularExpressions;

namespace AIRecommendation.Infrastructure.Parsing;

public static class CsvRowsReader
{
    public static IEnumerable<Dictionary<string, string>> ReadRows(string path, char delimiter = ',', Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        var lines = File.ReadAllLines(path, encoding);
        if (lines.Length == 0) yield break;

        string[] headers = ParseCsvLine(lines[0], delimiter).Select(h => h?.Trim() ?? "").Where(h => !string.IsNullOrEmpty(h)).ToArray();
        if (headers.Length == 0) yield break;

        for (int i = 1; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i], delimiter);
            if (fields.All(f => string.IsNullOrWhiteSpace(f))) continue;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < headers.Length; j++)
            {
                var value = j < fields.Length ? fields[j]?.Trim() ?? "" : "";
                dict[headers[j]] = value;
            }

            yield return dict;
        }
    }

    private static string[] ParseCsvLine(string line, char delimiter)
    {
        // Dynamically create regex for the delimiter
        string pattern = $"(?:^|{Regex.Escape(delimiter.ToString())})(?:\"(?<q>(?:[^\"]|\"\")*)\"|(?<u>[^{Regex.Escape(delimiter.ToString())}]*))";
        var matches = Regex.Matches(line, pattern);

        var list = new List<string>(matches.Count);
        foreach (Match m in matches)
        {
            var q = m.Groups["q"].Value;
            if (m.Groups["q"].Success)
            {
                // Unescape doubled quotes
                list.Add(q.Replace("\"\"", "\""));
            }
            else
            {
                list.Add(m.Groups["u"].Value);
            }
        }
        return list.ToArray();
    }
}