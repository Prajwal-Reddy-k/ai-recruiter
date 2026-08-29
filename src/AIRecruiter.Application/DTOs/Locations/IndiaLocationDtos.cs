namespace AIRecruiter.Application.DTOs.Locations;

public record IndianStateDto(string Name, IReadOnlyList<string> Cities);

public record IndiaLocationCatalogDto(IReadOnlyList<IndianStateDto> States);
