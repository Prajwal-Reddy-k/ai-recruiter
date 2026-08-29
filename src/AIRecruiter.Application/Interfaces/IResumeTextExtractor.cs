namespace AIRecruiter.Application.Interfaces;

/// <summary>Extracts plain text from an uploaded resume file, selected by content type.</summary>
public interface IResumeTextExtractor
{
    bool CanHandle(string contentType);
    Task<string> ExtractTextAsync(Stream content, CancellationToken ct = default);
}

/// <summary>Picks the right <see cref="IResumeTextExtractor"/> for a given content type.</summary>
public interface IResumeTextExtractorFactory
{
    IResumeTextExtractor GetExtractor(string contentType);
}
