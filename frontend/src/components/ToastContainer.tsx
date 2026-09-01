import { CheckCircle2, XCircle, Info, X } from "lucide-react";
import { useToast } from "../context/ToastContext";

function ToastIcon({ kind }: { kind: "success" | "error" | "info" }) {
  if (kind === "success") return <CheckCircle2 size={18} />;
  if (kind === "info") return <Info size={18} />;
  return <XCircle size={18} />;
}

export default function ToastContainer() {
  const { toasts, dismiss } = useToast();

  if (toasts.length === 0) return null;

  return (
    <div className="toast-stack" role="status" aria-live="polite">
      {toasts.map((toast) => (
        <div key={toast.id} className={`toast toast-${toast.kind}`}>
          <ToastIcon kind={toast.kind} />
          <span>{toast.message}</span>
          <button type="button" onClick={() => dismiss(toast.id)} aria-label="Dismiss notification">
            <X size={14} />
          </button>
        </div>
      ))}
    </div>
  );
}
