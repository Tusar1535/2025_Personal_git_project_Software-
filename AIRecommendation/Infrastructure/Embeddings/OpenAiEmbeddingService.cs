using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AIRecommendation.Application.Interfaces;

namespace AIRecommendation.Infrastructure.Embeddings;

public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;

    public OpenAiEmbeddingService(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("OpenAI API key is required", nameof(apiKey));

        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public float[] GenerateEmbedding(string text)
    {
        return GenerateEmbeddingAsync(text).GetAwaiter().GetResult();
    }

    private async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var payload = new
        {
            model = "text-embedding-3-small",
            input = text
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var res = await _http.PostAsync("https://api.openai.com/v1/embeddings", content);
        res.EnsureSuccessStatusCode();

        var resJson = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resJson);

        var data = doc.RootElement.GetProperty("data");
        if (data.GetArrayLength() == 0)
            return Array.Empty<float>();

        var embElem = data[0].GetProperty("embedding");
        var list = new List<float>(embElem.GetArrayLength());
        foreach (var v in embElem.EnumerateArray())
            list.Add(v.GetSingle());

        return list.ToArray();
    }
}
