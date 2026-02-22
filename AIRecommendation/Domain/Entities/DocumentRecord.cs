namespace AIRecommendation.Domain.Entities;

public class DocumentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FileName { get; set; } = "";
    public string FileType { get; set; } = "";
    public string FilePath { get; set; } = "";

    public string Title { get; set; } = "";
    public string Content { get; set; } = "";   // extracted text

    // store embedding as JSON string (easiest for cosine similarity later)
    public string EmbeddingJson { get; set; } = "";

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = ""; // dataset name like "support" / "movies"
    public string? Url { get; set; }
}