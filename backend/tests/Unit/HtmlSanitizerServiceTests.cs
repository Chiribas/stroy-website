using Xunit;
using Core.Interfaces;
using Infrastructure.Services;

namespace Unit;

public class HtmlSanitizerServiceTests
{
    private readonly IHtmlSanitizerService _sut = new HtmlSanitizerService();

    [Fact]
    public void Sanitize_RemovesScriptTags()
    {
        var result = _sut.Sanitize("<p>hi</p><script>alert('x')</script>");
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>hi</p>", result);
    }

    [Fact]
    public void Sanitize_KeepsBasicFormatting()
    {
        var result = _sut.Sanitize("<p><strong>bold</strong> and <em>italic</em></p>");
        Assert.Contains("<strong>bold</strong>", result);
        Assert.Contains("<em>italic</em>", result);
    }

    [Fact]
    public void Sanitize_KeepsIframeFromTrustedDomain()
    {
        var html = "<iframe src=\"https://www.youtube.com/embed/abc123\"></iframe>";
        var result = _sut.Sanitize(html);
        Assert.Contains("youtube.com/embed/abc123", result);
    }

    [Fact]
    public void Sanitize_RemovesIframeFromUntrustedDomain()
    {
        var html = "<iframe src=\"https://evil.example.com/x\"></iframe>";
        var result = _sut.Sanitize(html);
        Assert.DoesNotContain("evil.example.com", result);
    }

    [Theory]
    [InlineData("<iframe src=\"javascript:alert(1)\"></iframe>")]
    [InlineData("<iframe src=\"https://youtube.com@evil.com/x\"></iframe>")]
    [InlineData("<iframe src=\"https://youtube.com.evil.com/x\"></iframe>")]
    [InlineData("<iframe src=\"//www.youtube.com/embed/x\"></iframe>")]
    [InlineData("<iframe src=\"data:text/html,abc\"></iframe>")]
    public void Sanitize_RemovesUntrustedOrUnsafeIframes(string html)
    {
        var result = _sut.Sanitize(html);
        Assert.DoesNotContain("<iframe", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_StripsNonHttpsImageScheme()
    {
        var result = _sut.Sanitize("<img src=\"http://example.com/x.jpg\">");
        Assert.DoesNotContain("http://example.com", result);
    }

    [Fact]
    public void Sanitize_KeepsRelativeUploadImage()
    {
        // Uploaded images are stored as same-origin relative URLs (/uploads/..),
        // served by nginx in prod. The sanitizer must preserve them.
        var result = _sut.Sanitize("<p>x</p><img src=\"/uploads/abc.webp\" alt=\"y\">");
        Assert.Contains("/uploads/abc.webp", result);
    }

    [Fact]
    public void Sanitize_WithNullInput_ReturnsEmpty()
    {
        var result = _sut.Sanitize(null!);
        Assert.Equal(string.Empty, result);
    }
}
