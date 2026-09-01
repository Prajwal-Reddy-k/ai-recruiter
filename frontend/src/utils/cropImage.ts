export interface PixelCrop {
  x: number;
  y: number;
  width: number;
  height: number;
}

function createImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.addEventListener("load", () => resolve(img));
    img.addEventListener("error", (err) => reject(err));
    img.crossOrigin = "anonymous";
    img.src = src;
  });
}

function toRadians(degrees: number): number {
  return (degrees * Math.PI) / 180;
}

/** Draws the rotated source image onto a canvas, then extracts the cropped-area pixels into
 * a second canvas — the standard react-easy-crop recipe, kept as a small local utility
 * rather than a dependency since it's ~30 lines of plain Canvas API. */
export async function getCroppedImageBlob(imageSrc: string, cropPixels: PixelCrop, rotationDegrees: number): Promise<Blob> {
  const image = await createImage(imageSrc);
  const rotation = toRadians(rotationDegrees);

  const canvas = document.createElement("canvas");
  const ctx = canvas.getContext("2d");
  if (!ctx) throw new Error("Canvas is not supported in this browser.");

  const sin = Math.abs(Math.sin(rotation));
  const cos = Math.abs(Math.cos(rotation));
  const rotatedWidth = image.width * cos + image.height * sin;
  const rotatedHeight = image.width * sin + image.height * cos;

  canvas.width = rotatedWidth;
  canvas.height = rotatedHeight;

  ctx.translate(rotatedWidth / 2, rotatedHeight / 2);
  ctx.rotate(rotation);
  ctx.translate(-image.width / 2, -image.height / 2);
  ctx.drawImage(image, 0, 0);

  const cropCanvas = document.createElement("canvas");
  const cropCtx = cropCanvas.getContext("2d");
  if (!cropCtx) throw new Error("Canvas is not supported in this browser.");

  cropCanvas.width = cropPixels.width;
  cropCanvas.height = cropPixels.height;
  cropCtx.drawImage(canvas, cropPixels.x, cropPixels.y, cropPixels.width, cropPixels.height, 0, 0, cropPixels.width, cropPixels.height);

  return new Promise((resolve, reject) => {
    cropCanvas.toBlob((blob) => {
      if (blob) resolve(blob);
      else reject(new Error("Failed to generate the cropped image."));
    }, "image/jpeg", 0.9);
  });
}
