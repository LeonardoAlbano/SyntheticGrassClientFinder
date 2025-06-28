using SyntheticGrassClientFinder.Domain.ValueObjects;

namespace SyntheticGrassClientFinder.Domain.Services;

public interface IPlacesSearchService
{
    Task<IEnumerable<PlaceSearchResult>> SearchPlacesAsync(
        string query,
        Location location,
        int radiusMeters,
        CancellationToken cancellationToken = default);
}

public record PlaceSearchResult(
    string Name,
    string FormattedAddress,
    Location Location,
    string? PhoneNumber = null,
    string? Website = null,
    double? Rating = null,
    string? PlaceId = null)
{
    public string Address { get; set; }
}