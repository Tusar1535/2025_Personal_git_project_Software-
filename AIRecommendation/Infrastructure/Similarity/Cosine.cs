namespace AIRecommendation.Infrastructure.Similarity;

public static class Cosine
{
    public static double Similarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

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