namespace AIRecruiter.Infrastructure.Options;

/// <summary>Thin wrapper around the "ConnectionStrings" section — bound purely so its
/// required-in-Production presence can be validated at startup via IValidateOptions, exactly
/// like every other required setting. DependencyInjection.cs still reads the raw connection
/// string directly off IConfiguration for UseSqlServer(...), unchanged.</summary>
public class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    public string? DefaultConnection { get; set; }
}
