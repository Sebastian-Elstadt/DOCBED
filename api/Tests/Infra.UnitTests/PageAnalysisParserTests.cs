using Infra.DocumentVision;

namespace Infra.UnitTests;

public class PageAnalysisParserTests
{
    private const string ValidJson = """
    {
        "page_number": 4,
        "content_summary": "summary",
        "extracted_text": "text",
        "tables": [],
        "charts_diagrams": [],
        "key_entities": ["x"],
        "layout_type": "technical_paper"
    }
    """;

    [Fact]
    public void Parse_HandlesPlainJson()
    {
        var result = PageAnalysisParser.Parse(ValidJson);

        Assert.Equal(4, result.PageNumber);
        Assert.Equal("summary", result.ContentSummary);
        Assert.Equal("technical_paper", result.LayoutType);
    }

    [Fact]
    public void Parse_HandlesJsonInsideMarkdownFences()
    {
        var wrapped = $"Here is the result:\n```json\n{ValidJson}\n```\nHope this helps.";
        var result = PageAnalysisParser.Parse(wrapped);
        Assert.Equal(4, result.PageNumber);
    }

    [Fact]
    public void Parse_HandlesUnlabeledFences()
    {
        var wrapped = $"```\n{ValidJson}\n```";
        var result = PageAnalysisParser.Parse(wrapped);
        Assert.Equal(4, result.PageNumber);
    }

    [Fact]
    public void Parse_TakesLastFencedBlockWhenMultiplePresent()
    {
        var draft = """
        {
            "page_number": 999,
            "content_summary": "draft",
            "extracted_text": "",
            "tables": [],
            "charts_diagrams": [],
            "key_entities": [],
            "layout_type": "other"
        }
        """;
        var input = $"thinking...\n```json\n{draft}\n```\nfinal:\n```json\n{ValidJson}\n```";
        var result = PageAnalysisParser.Parse(input);
        Assert.Equal(4, result.PageNumber);
    }

    [Fact]
    public void Parse_FallsBackToOuterBracesWhenNoFence()
    {
        var input = $"some chatter before {ValidJson} and after";
        var result = PageAnalysisParser.Parse(input);
        Assert.Equal(4, result.PageNumber);
    }

    [Fact]
    public void Parse_ThrowsWhenNoJsonFound()
    {
        Assert.Throws<InvalidOperationException>(() => PageAnalysisParser.Parse("no json here at all"));
    }

    [Fact]
    public void Parse_ThrowsWhenJsonIsMalformed()
    {
        Assert.ThrowsAny<Exception>(() => PageAnalysisParser.Parse("{ this is not valid json }"));
    }
}
