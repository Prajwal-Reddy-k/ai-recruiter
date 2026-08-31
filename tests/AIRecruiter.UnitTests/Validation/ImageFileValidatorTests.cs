using AIRecruiter.Application.Validation;

namespace AIRecruiter.UnitTests.Validation;

public class ImageFileValidatorTests
{
    private readonly ImageFileValidator _sut = new();
    private const long MaxSize = 5 * 1024 * 1024;

    private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };
    private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] WebpHeader = { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };
    private static readonly byte[] BogusHeader = { 0x00, 0x01, 0x02, 0x03 };

    [Fact]
    public void Validate_ValidJpeg_Succeeds()
    {
        var result = _sut.Validate("photo.jpg", "image/jpeg", 1024, JpegHeader, MaxSize);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidPng_Succeeds()
    {
        var result = _sut.Validate("photo.png", "image/png", 1024, PngHeader, MaxSize);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidWebp_Succeeds()
    {
        var result = _sut.Validate("photo.webp", "image/webp", 1024, WebpHeader, MaxSize);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DisallowedExtension_Fails()
    {
        var result = _sut.Validate("photo.gif", "image/gif", 1024, JpegHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_MismatchedContentType_Fails()
    {
        var result = _sut.Validate("photo.jpg", "text/plain", 1024, JpegHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_OversizedFile_Fails()
    {
        var result = _sut.Validate("photo.jpg", "image/jpeg", MaxSize + 1, JpegHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ZeroByteFile_Fails()
    {
        var result = _sut.Validate("photo.jpg", "image/jpeg", 0, JpegHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ExtensionClaimsJpegButContentIsNot_Fails()
    {
        var result = _sut.Validate("photo.jpg", "image/jpeg", 1024, BogusHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ExtensionClaimsPngButContentIsJpeg_Fails()
    {
        var result = _sut.Validate("photo.png", "image/png", 1024, JpegHeader, MaxSize);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ExtensionClaimsWebpButRiffTagMissing_Fails()
    {
        var result = _sut.Validate("photo.webp", "image/webp", 1024, JpegHeader, MaxSize);
        Assert.False(result.IsValid);
    }
}
