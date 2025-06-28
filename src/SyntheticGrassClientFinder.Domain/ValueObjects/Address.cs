namespace SyntheticGrassClientFinder.Domain.ValueObjects;

public record Address(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country = "Brasil")
{
    public string FormattedAddress => $"{Street}, {City} - {State}, {PostalCode}, {Country}";
}