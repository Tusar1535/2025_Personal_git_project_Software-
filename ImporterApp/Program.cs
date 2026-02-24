using System.Text.Json;
using AIRecommendation.Application.Interfaces;
using AIRecommendation.Domain.Entities;
using AIRecommendation.Infrastructure.Embeddings;
using AIRecommendation.Infrastructure.Parsing;
using AIRecommendation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

IDocumentParser[] parsers =
{
    new TxtDocumentParser(),
    //new XlsxDocumentParser(),
    new CsvDocumentParser()
};
// Initialize embedding service: REQUIRE OpenAI for the Agent to work correctly
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (string.IsNullOrWhiteSpace(openAiKey))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("CRITICAL ERROR: OPENAI_API_KEY not found in environment.");
    Console.WriteLine("The AI Recommendation Agent REQUIRES OpenAI embeddings (1536 dimensions).");
    Console.WriteLine("If you import with Dummy embeddings, search results will have 0% relevance.");
    Console.ResetColor();
    return;
}

Console.WriteLine("Using OpenAI embedding service for import.");
IEmbeddingService embeddingService = new OpenAiEmbeddingService(openAiKey);

var config = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")) // ImporterApp folder
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var cs = config.GetConnectionString("Default")!;
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(cs)
    .Options;

using var db = new AppDbContext(options);

Console.WriteLine("Cleaning database (truncating Documents table)...");
db.Database.ExecuteSqlRaw("TRUNCATE TABLE Documents");

var repo = new EfDocumentRepository(db);

Console.WriteLine("ImporterApp DB connected and cleaned OK");

// DATASET 1
await ImportFolder(folderName: "User_support_Dataset");

Console.WriteLine("✅ Import finished.");
Console.WriteLine($"DB total rows now: {db.Documents.Count()}");

async Task ImportFolder(string folderName)
{
    var inputDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "data",
        folderName
    ));

    Console.WriteLine($"\nImporting folder: {inputDir}");

    if (!Directory.Exists(inputDir))
    {
        Console.WriteLine("ERROR: Input directory not found.");
        return;
    }

    var files = Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories)
        .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                 || f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    Console.WriteLine($"Found {files.Length} files");

    foreach (var file in files)
    {
        try
        {
            var fileName = Path.GetFileName(file);
            Console.WriteLine($"Processing: {fileName}");

            string category;
            if (fileName.Contains("Book", StringComparison.OrdinalIgnoreCase)) category = "books";
            else if (fileName.Contains("Resturant", StringComparison.OrdinalIgnoreCase)) category = "restaurant";
            else category = "support";

            // Row-by-row import for datasets
            if (fileName.Contains("BookDataset", StringComparison.OrdinalIgnoreCase) || 
                fileName.Contains("Resturant", StringComparison.OrdinalIgnoreCase))
            {
                await ImportRows(file, category);
                continue;
            }

            // Normal import for everything else (txt/unrecognized csv/xlsx)
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
                FileName = fileName,
                FileType = ext.TrimStart('.'),
                FilePath = file,
                Content = text,
                EmbeddingJson = JsonSerializer.Serialize(vec),
                ImportedAt = DateTime.UtcNow,
                Category = category,
                Url = $"file:///{file.Replace("\\", "/")}"
            };

            await repo.AddAsync(doc);
            await repo.SaveChangesAsync();
            Console.WriteLine($"✅ Imported whole file: {fileName} (category: {category})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR importing: {file}");
            Console.WriteLine(ex.ToString());
        }
    }
}

async Task ImportRows(string file, string category)
{
    var fileName = Path.GetFileName(file);
    var ext = Path.GetExtension(file).ToLowerInvariant();
    
    IEnumerable<Dictionary<string, string>> rows;
    /*if (ext == ".xlsx")
    {
       rows = XlsxRowsReader.ReadRows(file);
    } */
    if (ext == ".csv")
    {
        // Detect delimiter: semicolon for restaurant, comma for others
        char delimiter = fileName.Contains("Resturant", StringComparison.OrdinalIgnoreCase) ? ';' : ',';
        rows = CsvRowsReader.ReadRows(file, delimiter);
    }
    else
    {
        return;
    }

    int added = 0;
    foreach (var row in rows)
    {
        // Robust title detection
        string? title = null;
        if (category == "books")
        {
            if (row.TryGetValue("Book Name", out var t1)) title = t1;
            else if (row.TryGetValue("BookName", out var t2)) title = t2;
            else if (row.TryGetValue("Title", out var t3)) title = t3;
        }
        else if (category == "restaurant")
        {
            if (row.TryGetValue("Restaurant Name", out var r1)) title = r1;
            else if (row.TryGetValue("RestaurantName", out var r2)) title = r2;
            else if (row.TryGetValue("Name", out var r3)) title = r3;
        }

        if (string.IsNullOrWhiteSpace(title)) continue;

        var rowText = string.Join(" | ", row.Select(kv => $"{kv.Key}:{kv.Value}"));
        var rowVec = embeddingService.GenerateEmbedding(rowText);

        var doc = new DocumentRecord
        {
            Title = title,
            FileName = fileName,
            FileType = ext.TrimStart('.') + "-row",
            FilePath = file,
            Content = rowText,
            EmbeddingJson = JsonSerializer.Serialize(rowVec),
            ImportedAt = DateTime.UtcNow,
            Category = category,
            Url = $"file:///{file.Replace("\\", "/")}"
        };

        await repo.AddAsync(doc);
        added++;
    }

    await repo.SaveChangesAsync();
    Console.WriteLine($"✅ Imported {added} rows from {fileName} (category: {category})");
}
