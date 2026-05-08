namespace Infra.Configs;

public sealed record TogetherAIConfig(
    string BaseUrl,
    string ApiKey
)
{
    public void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("TogetherAI ApiKey is required.");
    }
}