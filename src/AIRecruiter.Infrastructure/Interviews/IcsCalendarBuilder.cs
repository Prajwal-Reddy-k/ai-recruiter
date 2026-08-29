using System.Text;

namespace AIRecruiter.Infrastructure.Interviews;

/// <summary>
/// Minimal, hand-rolled RFC 5545 calendar builder — no library, no external calendar API.
/// Produces a single VEVENT that any calendar app (Google Calendar, Outlook, Apple
/// Calendar, etc.) can import via a plain .ics file download.
/// </summary>
public static class IcsCalendarBuilder
{
    public static string BuildEvent(string uid, string summary, string description, DateTime startUtc, DateTime endUtc)
    {
        string Fold(string text) => text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
        string Stamp(DateTime dt) => dt.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");

        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//AI Recruiter//Interview Scheduling//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{uid}@ai-recruiter.local");
        sb.AppendLine($"DTSTAMP:{Stamp(DateTime.UtcNow)}");
        sb.AppendLine($"DTSTART:{Stamp(startUtc)}");
        sb.AppendLine($"DTEND:{Stamp(endUtc)}");
        sb.AppendLine($"SUMMARY:{Fold(summary)}");
        sb.AppendLine($"DESCRIPTION:{Fold(description)}");
        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");

        return sb.ToString();
    }
}
