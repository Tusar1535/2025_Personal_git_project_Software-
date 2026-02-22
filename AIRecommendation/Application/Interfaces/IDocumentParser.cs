namespace AIRecommendation.Application.Interfaces;

public interface IDocumentParser
{
    bool CanParse(string extension);     // ".txt"
    Task<string> ExtractTextAsync(string path);
}