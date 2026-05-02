namespace Infra.Configs;

public sealed record TogetherAIConfig(
    string ApiKey
)
{
    public void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("TogetherAI ApiKey is required.");
    }
}