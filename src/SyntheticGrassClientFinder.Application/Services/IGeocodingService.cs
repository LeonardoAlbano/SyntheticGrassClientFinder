using SyntheticGrassClientFinder.Domain.ValueObjects;

namespace SyntheticGrassClientFinder.Application.Services;

public interface IGeocodingService
{
    Task<Location?> GetLocationAsync(string address, CancellationToken cancellationToken = default);
}