namespace SyntheticGrassClientFinder.Domain.ValueObjects;

public record ContactInfo(
    string? Phone = null,
    string? Email = null,
    string? Website = null,
    string? SocialMedia = null);