using Microsoft.Extensions.Configuration;



var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var cs = config.GetConnectionString("Default");

Console.WriteLine("RecommenderApp config loaded OK");
Console.WriteLine(cs);