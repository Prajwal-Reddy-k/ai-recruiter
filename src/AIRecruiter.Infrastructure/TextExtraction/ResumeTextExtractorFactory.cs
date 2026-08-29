using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;

namespace AIRecruiter.Infrastructure.TextExtraction;

public class ResumeTextExtractorFactory : IResumeTextExtractorFactory
{
    private readonly IReadOnlyList<IResumeTextExtractor> _extractors;

    public ResumeTextExtractorFactory(IEnumerable<IResumeTextExtractor> extractors)
    {
        _extractors = extractors.ToList();
    }

    public IResumeTextExtractor GetExtractor(string contentType)
    {
        return _extractors.FirstOrDefault(e => e.CanHandle(contentType))
            ?? throw new ValidationException("Unsupported resume file type.");
    }
}
