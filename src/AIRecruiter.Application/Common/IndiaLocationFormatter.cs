namespace AIRecruiter.Application.Common;

public static class IndiaLocationFormatter
{
    public static string Format(string? city, string? state, bool isRemote)
    {
        if (isRemote) return "Remote — India";
        if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(state)) return $"{city}, {state}, India";
        return "Location not specified";
    }
}
