using System.Text.Json;
using AIRecommendation.Application.Interfaces;
using AIRecommendation.Domain.Entities;
using AIRecommendation.Infrastructure.Embeddings;
using AIRecommendation.Infrastructure.Parsing;
using AIRecommendation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

IDocumentParser[] parsers = { new TxtDocumentParser(), new XlsxDocumentParser() };
IEmbeddingService embeddingService = new DummyEmbeddingService();

var config = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")) // ImporterApp folder
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var cs = config.GetConnectionString("Default")!;
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(cs)
    .Options;

using var db = new AppDbContext(options);
db.Database.Migrate();

var repo = new EfDocumentRepository(db);

Console.WriteLine("ImporterApp DB connected OK");

// === DATASET 1: Support ===
await ImportFolder(category: "support", folderName: "User_support_Dataset");

// (optional) DATASET 2: Movies
// await ImportFolder(category: "movies", folderName: "Movies_Dataset");

Console.WriteLine("✅ Import finished.");

async Task ImportFolder(string category, string folderName)
{
    var inputDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "data",
        folderName
    ));

    Console.WriteLine($"\nImporting category '{category}' from: {inputDir}");

    if (!Directory.Exists(inputDir))
    {
        Console.WriteLine("ERROR: Input directory not found.");
        return;
    }

    var files = Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories)
        .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    Console.WriteLine($"Found {files.Length} files");

    int saved = 0;
    foreach (var file in files)
    {
        try
        {
            Console.WriteLine($"Processing: {file}");

            var ext = Path.GetExtension(file).ToLowerInvariant();
            var parser = parsers.FirstOrDefault(p => p.CanParse(ext));
            if (parser is null)
            {
                Console.WriteLine($"SKIP unsupported: {file}");
                continue;
            }

            var text = await parser.ExtractTextAsync(file);
            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine($"SKIP empty: {file}");
                continue;
            }

            var vec = embeddingService.GenerateEmbedding(text);

            var doc = new DocumentRecord
            {
                FileName = Path.GetFileName(file),
                FileType = ext.TrimStart('.'),
                FilePath = file,
                Content = text,
                EmbeddingJson = JsonSerializer.Serialize(vec),
                ImportedAt = DateTime.UtcNow,
                Category = category,
                Url = $"file:///{file.Replace("\\", "/")}"
            };

            await repo.AddAsync(doc);
            saved++;

            if (saved % 25 == 0)
            {
                await repo.SaveChangesAsync();
                Console.WriteLine($"Saved {saved} docs...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR importing: {file}");
            Console.WriteLine(ex.ToString());
        }
    }

    await repo.SaveChangesAsync();
    Console.WriteLine($"✅ Imported {saved} docs into '{category}'");
}