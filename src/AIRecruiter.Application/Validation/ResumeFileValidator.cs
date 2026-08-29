namespace AIRecruiter.Application.Validation;

public record ResumeValidationResult(bool IsValid, string? Error)
{
    public static ResumeValidationResult Success() => new(true, null);
    public static ResumeValidationResult Fail(string error) => new(false, error);
}

/// <summary>
/// Validates resume uploads (extension, declared content type, size, and file signature)
/// before anything is persisted. Pure logic — no IO — so it is fully unit-testable.
/// </summary>
public class ResumeFileValidator
{
    private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
    private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 }; // PK\x03\x04 (docx is a zip)

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx" };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    public ResumeValidationResult Validate(string fileName, string? contentType, long sizeBytes, byte[] headerBytes, long maxSizeBytes)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return ResumeValidationResult.Fail("Only PDF and DOCX files are supported.");
        }

        if (string.IsNullOrEmpty(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            return ResumeValidationResult.Fail("The uploaded file's type could not be verified as PDF or DOCX.");
        }

        if (sizeBytes <= 0 || sizeBytes > maxSizeBytes)
        {
            return ResumeValidationResult.Fail($"File must be between 1 byte and {maxSizeBytes / 1024 / 1024} MB.");
        }

        var isPdf = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        var expectedSignature = isPdf ? PdfSignature : ZipSignature;

        if (headerBytes.Length < expectedSignature.Length ||
            !headerBytes.AsSpan(0, expectedSignature.Length).SequenceEqual(expectedSignature))
        {
            return ResumeValidationResult.Fail("The file's content does not match its extension.");
        }

        return ResumeValidationResult.Success();
    }
}
