using AIRecommendation.Application.Interfaces;

namespace AIRecommendation.Infrastructure.Parsing;

public class TxtDocumentParser : IDocumentParser
{
    public bool CanParse(string extension)
        => extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(string path)
        => File.ReadAllTextAsync(path);
}