namespace AIRecommendation.Infrastructure.Similarity;

public static class Cosine
{
    public static double Similarity(float[] a, float[] b)
    {
        // VITAL: If lengths don't match, the dot product is invalid.
        // This often happens if data was imported with Dummy service (128) 
        // but searched with OpenAI service (1536).
        if (a.Length != b.Length || a.Length == 0) 
            return 0;

        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb) + 1e-10);
    }
}