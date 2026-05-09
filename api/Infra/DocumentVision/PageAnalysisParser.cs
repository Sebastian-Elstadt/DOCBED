using System.Text.Json;
using System.Text.RegularExpressions;
using App.DocumentVision;

namespace Infra.DocumentVision;

public static class PageAnalysisParser
{
    private static readonly Regex JsonFenceRegex = new(
        @"```(?:json)?\s*(\{.*?\})\s*```",
        RegexOptions.Singleline | RegexOptions.Compiled
    );

    public static DocumentPageAnalysis Parse(string response)
    {
        var candidate = ExtractJsonCandidate(response)
            ?? throw new InvalidOperationException($"No JSON found in response: {response}");

        return JsonSerializer.Deserialize<DocumentPageAnalysis>(candidate)
            ?? throw new InvalidOperationException($"Failed to deserialize: {candidate}");
    }

    public static string? ExtractJsonCandidate(string response)
    {
        var matches = JsonFenceRegex.Matches(response);
        if (matches.Count > 0)
            return matches[^1].Groups[1].Value;

        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start >= 0 && end > start)
            return response[start..(end + 1)];

        return null;
    }
}
