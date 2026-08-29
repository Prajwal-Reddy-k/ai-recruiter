using AIRecruiter.Application.Interfaces;

namespace AIRecruiter.Application.Validation;

/// <summary>
/// Rejects any location that isn't a valid India state/city pair from the checked-in
/// catalog — the last line of defense against a caller bypassing the UI selector via a
/// raw API request. Remote listings/profiles (no city/state) are always allowed.
/// </summary>
public class IndiaLocationValidator
{
    private readonly IIndianLocationCatalog _catalog;

    public IndiaLocationValidator(IIndianLocationCatalog catalog)
    {
        _catalog = catalog;
    }

    public (bool IsValid, string? Error) Validate(string? state, string? city, bool isRemote)
    {
        if (isRemote && string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(state))
        {
            return (true, null);
        }

        if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(city))
        {
            return (false, "State and city are required for a non-remote India location.");
        }

        if (!_catalog.IsValidState(state))
        {
            return (false, $"'{state}' is not a recognized Indian state or union territory.");
        }

        if (!_catalog.IsValidCity(state, city))
        {
            return (false, $"'{city}' is not a recognized city in {state}.");
        }

        return (true, null);
    }
}
