import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { MessageSquare } from "lucide-react";
import { getInbox } from "../api/messages";
import type { ConversationSummary } from "../types";
import { getErrorMessage } from "../utils/errors";
import Card from "../components/ui/Card";
import EmptyState from "../components/ui/EmptyState";
import MessageThread from "../components/MessageThread";

export default function CandidateMessagesPage() {
  const [conversations, setConversations] = useState<ConversationSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<ConversationSummary | null>(null);

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await getInbox();
      setConversations(data);
      if (data.length > 0) setSelected((prev) => prev ?? data[0]);
    } catch (err) {
      setError(getErrorMessage(err, "Failed to load messages"));
    } finally {
      setLoading(false);
    }
  }

  function handleSelect(c: ConversationSummary) {
    setSelected(c);
    setConversations((prev) => prev.map((x) => (x.jobApplicationId === c.jobApplicationId ? { ...x, unreadCount: 0 } : x)));
  }

  return (
    <div>
      <div className="page-header">
        <h1><MessageSquare size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Messages</h1>
        <p>Conversations with recruiters about jobs you've applied to.</p>
      </div>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

      {loading ? (
        <p>Loading...</p>
      ) : conversations.length === 0 ? (
        <EmptyState icon={<MessageSquare size={32} />} title="No conversations yet" description="Messages from recruiters about your applications will show up here." />
      ) : (
        <div className="messages-layout">
          <Card className="messages-conversation-list" padded={false}>
            {conversations.map((c) => (
              <button
                key={c.jobApplicationId}
                type="button"
                className={`conversation-row ${selected?.jobApplicationId === c.jobApplicationId ? "conversation-row-active" : ""}`}
                onClick={() => handleSelect(c)}
              >
                <div>
                  <p className="conversation-row-name">{c.counterpartName}</p>
                  <p className="hint">{c.jobTitle} · {c.companyName}</p>
                  {c.lastMessageBody && <p className="conversation-row-preview">{c.lastMessageBody}</p>}
                </div>
                {c.unreadCount > 0 && <span className="notif-badge" style={{ position: "static" }}>{c.unreadCount}</span>}
              </button>
            ))}
          </Card>

          <Card className="ui-card-padded messages-thread-panel">
            {selected ? (
              <>
                <div className="messages-thread-header">
                  <h3>{selected.counterpartName}</h3>
                  <Link to={`/applications/${selected.jobApplicationId}`} className="hint">View application →</Link>
                </div>
                <MessageThread applicationId={selected.jobApplicationId} onSent={load} />
              </>
            ) : (
              <p className="hint">Select a conversation.</p>
            )}
          </Card>
        </div>
      )}
    </div>
  );
}
