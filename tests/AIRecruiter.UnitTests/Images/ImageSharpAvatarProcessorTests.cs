using AIRecruiter.Infrastructure.Images;
using AIRecruiter.Infrastructure.Options;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AIRecruiter.UnitTests.Images;

public class ImageSharpAvatarProcessorTests
{
    private static ImageSharpAvatarProcessor CreateSut(int maxDimensionPx = 512) =>
        new(Options.Create(new AvatarOptions { MaxDimensionPx = maxDimensionPx }));

    private static MemoryStream CreateImageStream(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task ProcessAsync_WideRectangularInput_CropsToCenterSquare()
    {
        var sut = CreateSut();
        using var input = CreateImageStream(800, 400);

        var result = await sut.ProcessAsync(input);
        using var output = await Image.LoadAsync(result.Content);

        Assert.Equal(output.Width, output.Height);
    }

    [Fact]
    public async Task ProcessAsync_TallRectangularInput_CropsToCenterSquare()
    {
        var sut = CreateSut();
        using var input = CreateImageStream(300, 900);

        var result = await sut.ProcessAsync(input);
        using var output = await Image.LoadAsync(result.Content);

        Assert.Equal(output.Width, output.Height);
    }

    [Fact]
    public async Task ProcessAsync_OutputNeverExceedsMaxDimension()
    {
        var sut = CreateSut(maxDimensionPx: 256);
        using var input = CreateImageStream(1000, 1000);

        var result = await sut.ProcessAsync(input);
        using var output = await Image.LoadAsync(result.Content);

        Assert.True(output.Width <= 256);
        Assert.True(output.Height <= 256);
    }

    [Fact]
    public async Task ProcessAsync_AlwaysOutputsJpeg()
    {
        var sut = CreateSut();
        using var input = CreateImageStream(500, 500);

        var result = await sut.ProcessAsync(input);

        Assert.Equal("image/jpeg", result.ContentType);
        Assert.Equal(".jpg", result.Extension);
    }
}
