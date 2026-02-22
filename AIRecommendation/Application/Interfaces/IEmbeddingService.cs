namespace AIRecommendation.Application.Interfaces;

public interface IEmbeddingService
{
    float[] GenerateEmbedding(string text);
}