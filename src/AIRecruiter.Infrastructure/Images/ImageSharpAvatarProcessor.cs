using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Options;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace AIRecruiter.Infrastructure.Images;

/// <summary>
/// Decodes an uploaded avatar image, constrains it to a bounded square (preserving aspect
/// ratio), and always re-encodes it as JPEG — this keeps stored/served avatars uniform
/// regardless of whether the upload was JPG, PNG, or WEBP.
/// </summary>
public class ImageSharpAvatarProcessor : IAvatarImageProcessor
{
    private const int JpegQuality = 85;
    private readonly int _maxDimensionPx;

    public ImageSharpAvatarProcessor(IOptions<AvatarOptions> options)
    {
        _maxDimensionPx = options.Value.MaxDimensionPx;
    }

    public async Task<AvatarProcessResult> ProcessAsync(Stream content, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(content, ct);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(_maxDimensionPx, _maxDimensionPx),
        }));

        var output = new MemoryStream();
        await image.SaveAsync(output, new JpegEncoder { Quality = JpegQuality }, ct);
        output.Position = 0;

        return new AvatarProcessResult(output, "image/jpeg", ".jpg");
    }
}
