using System.Text.Json.Serialization;

namespace AIRecruiter.Infrastructure.Locations;

public class NominatimResult
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("lat")]
    public string? Lat { get; set; }

    [JsonPropertyName("lon")]
    public string? Lon { get; set; }
}
