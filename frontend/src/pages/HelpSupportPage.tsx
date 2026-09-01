import { useState, type FormEvent } from "react";
import { Mail, MapPin, Phone, LifeBuoy } from "lucide-react";
import { submitFeedback, FEEDBACK_CATEGORY_OPTIONS, type FeedbackCategoryValue } from "../api/feedback";
import { useAuth } from "../context/AuthContext";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";

interface FaqItem {
  question: string;
  answer: string;
}

interface FaqGroup {
  title: string;
  items: FaqItem[];
}

const FAQ_GROUPS: FaqGroup[] = [
  {
    title: "Registration & Login",
    items: [
      { question: "How do I create an account?", answer: "Click Register in the top-right corner, choose Candidate or Recruiter, and fill in your details. You can start browsing or posting jobs right away." },
      { question: "I forgot my password — what do I do?", answer: "Use the \"Forgot password?\" link on the login page. We'll send a 6-digit verification code to your email that lets you set a new password." },
      { question: "Can I change my email address?", answer: "Not yet from your account settings — contact support using the form below and we'll help." },
    ],
  },
  {
    title: "Job Search",
    items: [
      { question: "How does job matching work?", answer: "When you apply, we compute a local, explainable match score comparing your skills and resume against the job's requirements — no external AI service is used." },
      { question: "Can I save jobs to review later?", answer: "Yes — click the bookmark icon on any job card or job detail page. Saved jobs appear under \"Saved Jobs\" on your dashboard." },
      { question: "How do I get notified about new matching jobs?", answer: "Set up a Job Alert from the Job Alerts page with your preferred skills and location — you'll see new matches on your dashboard." },
    ],
  },
  {
    title: "Applications",
    items: [
      { question: "How do I track my application status?", answer: "Open \"My Applications\" from your dashboard — every application shows its current status and full history." },
      { question: "Can I withdraw an application?", answer: "Yes, from the application detail page, as long as the recruiter hasn't already moved it to a final status." },
    ],
  },
  {
    title: "Recruiter Job Posting",
    items: [
      { question: "How do I post a job?", answer: "Complete your Company Setup once, then use \"Post a Job\" from your dashboard or the navigation bar." },
      { question: "Can I reuse a job posting as a template?", answer: "Yes — save any job as a template from Manage Jobs, or start a new posting from an existing template." },
      { question: "How do I invite a specific candidate to apply?", answer: "From Candidate Search, use \"Discover candidates\" and click \"Invite to Apply\" on anyone with a public profile." },
    ],
  },
  {
    title: "Interviews",
    items: [
      { question: "How do I schedule an interview?", answer: "From an applicant's row on the applicants page, click \"Schedule interview\" and set a date, time, and format." },
      { question: "Can I download a calendar invite?", answer: "Yes — once an interview is scheduled and accepted, either side can download an .ics file from the interview details." },
    ],
  },
  {
    title: "Password Reset",
    items: [
      { question: "How long is the reset code valid?", answer: "10 minutes. If it expires, just request a new one from the \"Forgot password?\" page." },
      { question: "I'm logged in — can I change my password directly?", answer: "Yes — go to Account Settings and use the Change Password section, no email code required." },
    ],
  },
];

interface FeedbackFieldErrors {
  name?: string;
  email?: string;
  message?: string;
  general?: string;
}

export default function HelpSupportPage() {
  const { user } = useAuth();
  const [name, setName] = useState(user?.fullName ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [category, setCategory] = useState<FeedbackCategoryValue>("General");
  const [message, setMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FeedbackFieldErrors>({});

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    if (!name.trim() || !email.trim() || message.trim().length < 10) {
      setFieldErrors({
        name: !name.trim() ? "Name is required." : undefined,
        email: !email.trim() ? "Email is required." : undefined,
        message: message.trim().length < 10 ? "Message must be at least 10 characters." : undefined,
      });
      return;
    }

    setSubmitting(true);
    try {
      await submitFeedback({ name: name.trim(), email: email.trim(), category, message: message.trim() });
      setSubmitted(true);
    } catch (err) {
      const serverFieldErrors = getFieldErrors(err);
      if (serverFieldErrors) {
        setFieldErrors(serverFieldErrors as FeedbackFieldErrors);
      } else {
        setFieldErrors({ general: getErrorMessage(err, "Failed to submit your message. Please try again.") });
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div>
      <PageHeader title="Help & Support" subtitle="Answers to common questions, and a way to reach us directly." />

      {FAQ_GROUPS.map((group) => (
        <div className="faq-group" key={group.title}>
          <h3>{group.title}</h3>
          {group.items.map((item) => (
            <details className="faq-item" key={item.question}>
              <summary>{item.question}</summary>
              <p>{item.answer}</p>
            </details>
          ))}
        </div>
      ))}

      <Card className="ui-card-padded" style={{ marginTop: "2rem" }}>
        <h2><LifeBuoy size={18} style={{ verticalAlign: "-3px", marginRight: "0.4rem" }} />Contact us</h2>
        <p className="hint" style={{ marginBottom: "1rem" }}>
          <Mail size={13} style={{ verticalAlign: "-2px" }} /> support@airecruiter-demo.in ·{" "}
          <Phone size={13} style={{ verticalAlign: "-2px" }} /> +91 90000 00000 ·{" "}
          <MapPin size={13} style={{ verticalAlign: "-2px" }} /> Bengaluru, Karnataka, India
        </p>

        {submitted ? (
          <p className="success">Thanks — your message has been received. We'll get back to you soon.</p>
        ) : (
          <form onSubmit={handleSubmit} noValidate>
            <div className="form-row">
              <FormField label="Name" htmlFor="feedback-name" required error={fieldErrors.name}>
                <input id="feedback-name" value={name} onChange={(e) => setName(e.target.value)} aria-required="true" />
              </FormField>
              <FormField label="Email" htmlFor="feedback-email" required error={fieldErrors.email}>
                <input id="feedback-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} aria-required="true" />
              </FormField>
            </div>
            <FormField label="Category" htmlFor="feedback-category">
              <select id="feedback-category" value={category} onChange={(e) => setCategory(e.target.value as FeedbackCategoryValue)}>
                {FEEDBACK_CATEGORY_OPTIONS.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
              </select>
            </FormField>
            <FormField label="Message" htmlFor="feedback-message" required hint="At least 10 characters." error={fieldErrors.message}>
              <textarea id="feedback-message" rows={5} value={message} onChange={(e) => setMessage(e.target.value)} aria-required="true" />
            </FormField>

            {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

            <div className="form-actions">
              <Button type="submit" loading={submitting}>Send message</Button>
            </div>
          </form>
        )}
      </Card>
    </div>
  );
}
