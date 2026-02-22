using AIRecommendation.Application.Interfaces;

namespace AIRecommendation.Infrastructure.Embeddings;

public class DummyEmbeddingService : IEmbeddingService
{
    // deterministic vector from text (good for demo + cosine)
    public float[] GenerateEmbedding(string text)
    {
        const int dim = 128;
        var v = new float[dim];

        unchecked
        {
            int hash = 17;
            foreach (var ch in text)
                hash = hash * 31 + ch;

            var rand = new Random(hash);
            for (int i = 0; i < dim; i++)
                v[i] = (float)(rand.NextDouble() * 2.0 - 1.0);
        }

        return v;
    }
}