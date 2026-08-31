namespace AIRecruiter.Application.Validation;

/// <summary>
/// Validates image uploads (extension, declared content type, size, and file signature)
/// before anything is persisted. Pure logic — no IO — so it is fully unit-testable.
/// Mirrors <see cref="ResumeFileValidator"/> for a different allowed file set.
/// </summary>
public class ImageFileValidator
{
    private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] RiffTag = { 0x52, 0x49, 0x46, 0x46 }; // "RIFF"
    private static readonly byte[] WebpTag = { 0x57, 0x45, 0x42, 0x50 }; // "WEBP" (bytes 8-11 of a WEBP file)

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public ResumeValidationResult Validate(string fileName, string? contentType, long sizeBytes, byte[] headerBytes, long maxSizeBytes)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return ResumeValidationResult.Fail("Only JPG, JPEG, PNG, and WEBP images are supported.");
        }

        if (string.IsNullOrEmpty(contentType) || !AllowedContentTypes.Contains(contentType))
        {
            return ResumeValidationResult.Fail("The uploaded file's type could not be verified as an image.");
        }

        if (sizeBytes <= 0 || sizeBytes > maxSizeBytes)
        {
            return ResumeValidationResult.Fail($"Image must be between 1 byte and {maxSizeBytes / 1024 / 1024} MB.");
        }

        if (!MatchesSignature(extension, headerBytes))
        {
            return ResumeValidationResult.Fail("The file's content does not match its extension.");
        }

        return ResumeValidationResult.Success();
    }

    private static bool MatchesSignature(string extension, byte[] headerBytes)
    {
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return StartsWith(headerBytes, PngSignature);
        }

        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            // WEBP = "RIFF" (bytes 0-3) + 4-byte size + "WEBP" (bytes 8-11).
            return headerBytes.Length >= 12
                && StartsWith(headerBytes, RiffTag)
                && headerBytes.AsSpan(8, 4).SequenceEqual(WebpTag);
        }

        // .jpg / .jpeg
        return StartsWith(headerBytes, JpegSignature);
    }

    private static bool StartsWith(byte[] headerBytes, byte[] signature) =>
        headerBytes.Length >= signature.Length && headerBytes.AsSpan(0, signature.Length).SequenceEqual(signature);
}
