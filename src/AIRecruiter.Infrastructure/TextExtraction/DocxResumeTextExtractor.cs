using System.Text;
using AIRecruiter.Application.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AIRecruiter.Infrastructure.TextExtraction;

public class DocxResumeTextExtractor : IResumeTextExtractor
{
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public bool CanHandle(string contentType) => contentType.Equals(DocxContentType, StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(Stream content, CancellationToken ct = default)
    {
        using var wordDocument = WordprocessingDocument.Open(content, false);
        var body = wordDocument.MainDocumentPart?.Document?.Body;

        if (body is null)
        {
            return Task.FromResult(string.Empty);
        }

        var sb = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            ct.ThrowIfCancellationRequested();
            foreach (var text in paragraph.Descendants<Text>())
            {
                sb.Append(text.Text);
                sb.Append(' ');
            }
            sb.AppendLine();
        }

        return Task.FromResult(sb.ToString());
    }
}
