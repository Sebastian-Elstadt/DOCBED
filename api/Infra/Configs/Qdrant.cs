namespace Infra.Configs;

public sealed record QdrantConfig(
    string Host,
    ushort Port,
    bool Https
)
{
    public void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException("Qdrant Host is required.");

        if (Port < 1)
            throw new InvalidOperationException("Qdrant Port is required.");
    }
}