using AIRecommendation.Application.Interfaces;

namespace AIRecommendation.Infrastructure.Parsing;

public class CsvDocumentParser : IDocumentParser
{
    public bool CanParse(string extension)
        => extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(string path)
        => Task.FromResult(File.ReadAllText(path));
}