using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;

namespace AIRecommendation.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Find solution root by walking up until it contains "ImporterApp"
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "ImporterApp")))
            dir = dir.Parent;

        if (dir == null)
            throw new InvalidOperationException("Could not locate solution root (folder containing 'ImporterApp').");

        var importerPath = Path.Combine(dir.FullName, "ImporterApp");
        var appsettingsPath = Path.Combine(importerPath, "appsettings.json");

        if (!File.Exists(appsettingsPath))
            throw new InvalidOperationException($"Missing appsettings.json at: {appsettingsPath}");

        var config = new ConfigurationBuilder()
            .SetBasePath(importerPath)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var cs = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Connection string 'Default' not found in ImporterApp/appsettings.json.");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(cs);

        return new AppDbContext(optionsBuilder.Options);
    }
}