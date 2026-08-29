using AIRecruiter.Application.Validation;

namespace AIRecruiter.UnitTests.Validation;

public class ResumeFileValidatorTests
{
    private readonly ResumeFileValidator _sut = new();
    private const long MaxSize = 5 * 1024 * 1024;

    private static readonly byte[] PdfHeader = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
    private static readonly byte[] ZipHeader = { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 };
    private static readonly byte[] BogusHeader = { 0x00, 0x01, 0x02, 0x03 };

    [Fact]
    public void Validate_ValidPdf_Succeeds()
    {
        var result = _sut.Validate("resume.pdf", "application/pdf", 1024, PdfHeader, MaxSize);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidDocx_Succeeds()
    {
        var result = _sut.Validate(
            "resume.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            1024,
            ZipHeader,
            MaxSize);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DisallowedExtension_Fails()
    {
        var result = _sut.Validate("resume.exe", "application/pdf", 1024, PdfHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_MismatchedContentType_Fails()
    {
        var result = _sut.Validate("resume.pdf", "text/plain", 1024, PdfHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_OversizedFile_Fails()
    {
        var result = _sut.Validate("resume.pdf", "application/pdf", MaxSize + 1, PdfHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ZeroByteFile_Fails()
    {
        var result = _sut.Validate("resume.pdf", "application/pdf", 0, PdfHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ExtensionClaimsPdfButContentIsNot_Fails()
    {
        var result = _sut.Validate("resume.pdf", "application/pdf", 1024, BogusHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ExtensionClaimsDocxButContentIsPdf_Fails()
    {
        var result = _sut.Validate(
            "resume.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            1024,
            PdfHeader,
            MaxSize);

        Assert.False(result.IsValid);
    }
}
