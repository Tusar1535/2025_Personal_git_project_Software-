using System.Text.Json;
using AIRecommendation.Infrastructure.Embeddings;
using AIRecommendation.Infrastructure.Persistence;
using AIRecommendation.Infrastructure.Similarity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")) // RecommenderApp folder
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var cs = config.GetConnectionString("Default")!;
Console.WriteLine("RecommenderApp DB connection string loaded.");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(cs)
    .Options;

using var db = new AppDbContext(options);

Console.WriteLine($"DB rows: {db.Documents.Count()}");


// Use same embedding service as ImporterApp
var embeddingService = new DummyEmbeddingService();

Console.WriteLine("Type a query (example: 'USB cable' or 'wifi issue').");
Console.Write("Query: ");
var prompt = Console.ReadLine() ?? "";

Console.Write("Top N results (default 5): ");
var nText = Console.ReadLine();
var topN = int.TryParse(nText, out var n) && n > 0 ? n : 5;

Console.Write("Category filter (press Enter for all, e.g. 'support'): ");
var category = Console.ReadLine();

var queryVec = embeddingService.GenerateEmbedding(prompt);

var docsQuery = db.Documents.AsNoTracking();

if (!string.IsNullOrWhiteSpace(category))
    docsQuery = docsQuery.Where(d => d.Category == category);

var docs = await docsQuery.ToListAsync();

if (docs.Count == 0)
{
    Console.WriteLine("No documents found in DB (or category filter returned none).");
    return;
}

var scored = docs
    .Select(d =>
    {
        var vec = JsonSerializer.Deserialize<float[]>(d.EmbeddingJson) ?? Array.Empty<float>();
        var score = (vec.Length == 0) ? 0.0 : Cosine.Similarity(queryVec, vec);

        return new { Doc = d, Score = score };
    })
    .OrderByDescending(x => x.Score)
    .Take(topN)
    .ToList();

Console.WriteLine($"\nTop {topN} recommendations:");
foreach (var x in scored)
{
    Console.WriteLine($"Score={x.Score:F4} | {x.Doc.FileName} | category={x.Doc.Category}");
    Console.WriteLine($"Path: {x.Doc.FilePath}");
    if (!string.IsNullOrWhiteSpace(x.Doc.Url))
        Console.WriteLine($"Url : {x.Doc.Url}");
    Console.WriteLine(new string('-', 60));
}