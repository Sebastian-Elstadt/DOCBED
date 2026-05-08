namespace App.DocumentVision;

public record DocumentAnalysis
{
    public required byte[] DocumentHash { get; init; }
    public required Func<CancellationToken, IAsyncEnumerable<DocumentPageAnalysis>> AnalyzePages { get; init; }
}