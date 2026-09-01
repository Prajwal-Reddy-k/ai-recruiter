import { useCallback, useEffect, useState } from "react";
import Cropper, { type Area, type Point } from "react-easy-crop";
import { RotateCw, RotateCcw, ZoomIn } from "lucide-react";
import Modal from "./ui/Modal";
import Button from "./ui/Button";
import { getCroppedImageBlob, type PixelCrop } from "../utils/cropImage";

interface AvatarCropModalProps {
  file: File;
  onCancel: () => void;
  onSave: (file: File) => void;
  saving: boolean;
}

export default function AvatarCropModal({ file, onCancel, onSave, saving }: AvatarCropModalProps) {
  const [imageSrc, setImageSrc] = useState<string | null>(null);
  const [crop, setCrop] = useState<Point>({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [rotation, setRotation] = useState(0);
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<PixelCrop | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const url = URL.createObjectURL(file);
    setImageSrc(url);
    return () => URL.revokeObjectURL(url);
  }, [file]);

  const onCropComplete = useCallback((_croppedArea: Area, croppedAreaPixelsResult: Area) => {
    setCroppedAreaPixels(croppedAreaPixelsResult);
  }, []);

  function handleReset() {
    setCrop({ x: 0, y: 0 });
    setZoom(1);
    setRotation(0);
  }

  async function handleSave() {
    if (!imageSrc || !croppedAreaPixels) return;
    setError(null);
    try {
      const blob = await getCroppedImageBlob(imageSrc, croppedAreaPixels, rotation);
      const croppedFile = new File([blob], "avatar.jpg", { type: "image/jpeg" });
      onSave(croppedFile);
    } catch {
      setError("Couldn't process this image. Please try a different photo.");
    }
  }

  return (
    <Modal
      open
      onClose={onCancel}
      title="Adjust your photo"
      footer={
        <>
          <Button variant="secondary" onClick={onCancel} disabled={saving}>Cancel</Button>
          <Button onClick={handleSave} loading={saving} disabled={!imageSrc}>Save Photo</Button>
        </>
      }
    >
      <div className="avatar-crop-area">
        {imageSrc && (
          <Cropper
            image={imageSrc}
            crop={crop}
            zoom={zoom}
            rotation={rotation}
            aspect={1}
            cropShape="round"
            showGrid={false}
            onCropChange={setCrop}
            onZoomChange={setZoom}
            onRotationChange={setRotation}
            onCropComplete={onCropComplete}
          />
        )}
      </div>

      <div className="avatar-crop-controls">
        <label className="avatar-crop-zoom" htmlFor="avatar-zoom">
          <ZoomIn size={16} aria-hidden="true" />
          <input
            id="avatar-zoom"
            type="range"
            min={1}
            max={3}
            step={0.05}
            value={zoom}
            onChange={(e) => setZoom(Number(e.target.value))}
            aria-label="Zoom"
          />
        </label>

        <div className="avatar-crop-buttons">
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => setRotation((r) => r - 90)} aria-label="Rotate left">
            <RotateCcw size={16} />
          </button>
          <button type="button" className="btn btn-ghost btn-sm" onClick={() => setRotation((r) => r + 90)} aria-label="Rotate right">
            <RotateCw size={16} />
          </button>
          <button type="button" className="btn btn-ghost btn-sm" onClick={handleReset}>Reset</button>
        </div>
      </div>

      {error && <p className="error" style={{ marginTop: "0.75rem" }}>{error}</p>}
    </Modal>
  );
}
