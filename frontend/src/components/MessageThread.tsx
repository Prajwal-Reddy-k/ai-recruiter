import { useEffect, useRef, useState, type FormEvent } from "react";
import { Send } from "lucide-react";
import { getMessageThread, markThreadRead, sendMessage } from "../api/messages";
import type { Message } from "../types";
import { useAuth } from "../context/AuthContext";
import { getErrorMessage } from "../utils/errors";
import Button from "./ui/Button";

const MAX_LENGTH = 2000;

interface MessageThreadProps {
  applicationId: number;
  onSent?: () => void;
}

export default function MessageThread({ applicationId, onSent }: MessageThreadProps) {
  const { user } = useAuth();
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [body, setBody] = useState("");
  const [sending, setSending] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [applicationId]);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const thread = await getMessageThread(applicationId);
      setMessages(thread);
      await markThreadRead(applicationId);
    } catch (err) {
      setError(getErrorMessage(err, "Failed to load messages"));
    } finally {
      setLoading(false);
      setTimeout(() => bottomRef.current?.scrollIntoView({ block: "nearest" }), 0);
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const trimmed = body.trim();
    if (!trimmed) return;

    setSendError(null);
    setSending(true);
    try {
      const message = await sendMessage(applicationId, trimmed);
      setMessages((prev) => [...prev, message]);
      setBody("");
      onSent?.();
      setTimeout(() => bottomRef.current?.scrollIntoView({ block: "nearest" }), 0);
    } catch (err) {
      setSendError(getErrorMessage(err, "Failed to send message"));
    } finally {
      setSending(false);
    }
  }

  if (loading) return <p className="hint">Loading messages...</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <div className="message-thread">
      <div className="message-list" role="log" aria-live="polite">
        {messages.length === 0 ? (
          <p className="hint">No messages yet — say hello.</p>
        ) : (
          messages.map((m) => (
            <div key={m.id} className={`message-bubble ${m.senderUserId === user?.userId ? "message-bubble-mine" : ""}`}>
              <p className="message-bubble-meta">{m.senderName} · {new Date(m.createdAt).toLocaleString()}</p>
              <p className="message-bubble-body">{m.body}</p>
            </div>
          ))
        )}
        <div ref={bottomRef} />
      </div>

      <form onSubmit={handleSubmit} className="message-composer" noValidate>
        <label htmlFor="message-body" className="visually-hidden">Message</label>
        <textarea
          id="message-body"
          value={body}
          onChange={(e) => setBody(e.target.value.slice(0, MAX_LENGTH))}
          rows={2}
          placeholder="Type a message..."
          maxLength={MAX_LENGTH}
          aria-describedby="message-char-count"
        />
        <div className="message-composer-footer">
          <span id="message-char-count" className="hint">{body.length}/{MAX_LENGTH}</span>
          <Button type="submit" size="sm" icon={<Send size={14} />} loading={sending} disabled={!body.trim()}>
            Send
          </Button>
        </div>
        {sendError && <p className="error" style={{ marginTop: "0.5rem" }}>{sendError}</p>}
      </form>
    </div>
  );
}
