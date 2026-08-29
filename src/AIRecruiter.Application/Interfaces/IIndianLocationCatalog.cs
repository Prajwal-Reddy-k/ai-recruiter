using AIRecruiter.Application.DTOs.Locations;

namespace AIRecruiter.Application.Interfaces;

/// <summary>
/// Checked-in, zero-cost source of truth for valid India state/city pairs. No external
/// location API is involved — this is our own static dataset.
/// </summary>
public interface IIndianLocationCatalog
{
    IndiaLocationCatalogDto GetCatalog();
    bool IsValidState(string state);
    bool IsValidCity(string state, string city);
}
