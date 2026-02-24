using System.Text.Json;
using AIRecommendation.Infrastructure.Embeddings;
using AIRecommendation.Application.Interfaces;
using AIRecommendation.Infrastructure.Persistence;
using AIRecommendation.Infrastructure.Similarity;
using AIRecommendation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// --- 1. SETUP & CONFIG ---
var config = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."))
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var cs = config.GetConnectionString("Default")!;
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(cs)
    .Options;

using var db = new AppDbContext(options);

// Initialize embedding service: prefer OpenAI if OPENAI_API_KEY is provided
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
IEmbeddingService embeddingService;
if (!string.IsNullOrWhiteSpace(openAiKey))
{
    Console.WriteLine("Using OpenAI embedding service (from OPENAI_API_KEY environment variable)");
    embeddingService = new OpenAiEmbeddingService(openAiKey);
}
else
{
    Console.WriteLine("OPENAI_API_KEY not found in environment; using DummyEmbeddingService");
    embeddingService = new DummyEmbeddingService();
}

// --- 2. INITIALIZE AGENT ---
// The RecommendationTool is the "Plug-in" that talks to the DB
var searchTool = new RecommendationTool(db, embeddingService);
// The Agent is the "Brain" that decides how to use the tool
var agent = new RecommendationAgent(searchTool);

// --- 3. UI INTERACTION ---
Console.WriteLine("=== AI Recommendation Agent ===");
Console.WriteLine($"Database connected. Total records: {db.Documents.Count()}");

while (true)
{
    Console.WriteLine("\nType a query (e.g., 'I want to eat Japanese food' or 'find a scary book')");
    Console.WriteLine("Type 'exit' to quit.");
    Console.Write("Query: ");
    var prompt = Console.ReadLine() ?? "";

    if (prompt.ToLower() == "exit") break;

    Console.Write("Top N results (default 5): ");
    var topN = int.TryParse(Console.ReadLine(), out var n) ? n : 5;

    // Agent handles the mapping and execution (Requirement 3.2)
    await agent.HandleRequest(prompt, topN);
}

// --- 4. AGENT & TOOL CLASSES ---

public class RecommendationAgent
{
    private readonly RecommendationTool _tool;

    public RecommendationAgent(RecommendationTool tool) => _tool = tool;

    public async Task HandleRequest(string prompt, int topN)
    {
        // INTENT MAPPING (Requirement 3.2)
        // Map prompt to intent arguments (Category)
        string detectedCategory = "";

        if (prompt.Contains("read", StringComparison.OrdinalIgnoreCase) ||
            prompt.Contains("book", StringComparison.OrdinalIgnoreCase))
        {
            detectedCategory = "books";
            Console.WriteLine("[Agent] Detected Intent: Book Recommendation");
        }
        else if (prompt.Contains("eat", StringComparison.OrdinalIgnoreCase) ||
                 prompt.Contains("resturent", StringComparison.OrdinalIgnoreCase) ||
                 prompt.Contains("restaurant", StringComparison.OrdinalIgnoreCase) ||
                 prompt.Contains("food", StringComparison.OrdinalIgnoreCase) ||
                 prompt.Contains("frankfurt", StringComparison.OrdinalIgnoreCase))
        {
            detectedCategory = "restaurant";
            Console.WriteLine("[Agent] Detected Intent: Dining/Restaurant Search");
        }

        var results = await _tool.SearchAsync(prompt, detectedCategory, topN);
        DisplayResults(results);
    }

    private void DisplayResults(IEnumerable<ScoredResult> results)
    {
        if (!results.Any())
        {
            Console.WriteLine("No relevant documents found for this intent.");
            return;
        }

        Console.WriteLine($"\nFound {results.Count()} relevant matches:");

        foreach (var res in results)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n[{res.Doc.Category.ToUpper()}] RECOMMENDATION");
            Console.ResetColor();

            var displayName = string.IsNullOrWhiteSpace(res.Doc.Title) ? res.Doc.FileName : res.Doc.Title;

            // Professional Metadata Display (Requirement 3.4)
            Console.WriteLine($"   Relevance Score : {res.Score:P2}");
            Console.WriteLine($"   Title           : {displayName}");
            Console.WriteLine($"   Source File     : {res.Doc.FileName}");

            Console.WriteLine("   --- Details ---");
            if (!string.IsNullOrWhiteSpace(res.Doc.Content))
            {
                // Splits the Content by common separators used in CSV/Excel parsing
                var details = res.Doc.Content.Split(new[] { '|', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var detail in details)
                {
                    if (!string.IsNullOrWhiteSpace(detail))
                        Console.WriteLine($"   {detail.Trim()}");
                }
            }

            if (!string.IsNullOrWhiteSpace(res.Doc.Url))
                Console.WriteLine($"   Link            : {res.Doc.Url}");

            Console.WriteLine(new string('-', 60));
        }
    }
}

public class RecommendationTool
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embeddings;

    public RecommendationTool(AppDbContext db, IEmbeddingService embeddings)
    {
        _db = db;
        _embeddings = embeddings;
    }

    public async Task<List<ScoredResult>> SearchAsync(string query, string category, int topN)
    {
        var queryVec = _embeddings.GenerateEmbedding(query);
        var queryable = _db.Documents.AsNoTracking();

        // Use the Intent Argument (category) to filter results
        if (!string.IsNullOrEmpty(category))
            queryable = queryable.Where(d => d.Category == category);

        var docs = await queryable.ToListAsync();

        if (docs.Any())
        {
            var firstDocVec = JsonSerializer.Deserialize<float[]>(docs[0].EmbeddingJson) ?? Array.Empty<float>();
            if (queryVec.Length != firstDocVec.Length)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[Diagnostic] Vector length mismatch! Query={queryVec.Length}, DB={firstDocVec.Length}");
                Console.WriteLine("Please re-run ImporterApp to ensure embeddings are consistent.");
                Console.ResetColor();
            }
        }

        return docs.Select(d => new ScoredResult
        {
            Doc = d,
            Score = Cosine.Similarity(queryVec, JsonSerializer.Deserialize<float[]>(d.EmbeddingJson) ?? Array.Empty<float>())
        })
        .OrderByDescending(r => r.Score)
        .Take(topN)
        .ToList();
    }
}

public class ScoredResult
{
    public DocumentRecord Doc { get; set; } = null!;
    public double Score { get; set; }
}