using System.Text;
using AIRecruiter.Application.Interfaces;
using UglyToad.PdfPig;

namespace AIRecruiter.Infrastructure.TextExtraction;

public class PdfResumeTextExtractor : IResumeTextExtractor
{
    public bool CanHandle(string contentType) => contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(Stream content, CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(content);
        foreach (var page in document.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine(page.Text);
        }

        return Task.FromResult(sb.ToString());
    }
}
