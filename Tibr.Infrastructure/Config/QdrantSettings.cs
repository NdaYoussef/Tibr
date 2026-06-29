namespace Tibr.Infrastructure.Config;

public class QdrantSettings
{
    public const string SectionName = "Qdrant";
    public string Url { get; set; } = "http://localhost:6333";
    public int VectorSize { get; set; } = 1024;
}
