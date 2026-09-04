namespace AIRecruiter.Application.Validation;

/// <summary>Deterministic experience bucketing shared by salary aggregation and guidance —
/// no fuzzy role taxonomy, just a fixed years-of-experience cutoff.</summary>
public static class SalaryExperienceBand
{
    public const string Junior = "Junior";
    public const string Mid = "Mid";
    public const string Senior = "Senior";
    public const string Lead = "Lead";

    public static string From(int? years) => years switch
    {
        null => Mid,
        <= 2 => Junior,
        <= 5 => Mid,
        <= 10 => Senior,
        _ => Lead,
    };
}
