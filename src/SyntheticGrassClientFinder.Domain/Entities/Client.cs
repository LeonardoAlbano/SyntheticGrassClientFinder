using SyntheticGrassClientFinder.Domain.ValueObjects;

namespace SyntheticGrassClientFinder.Domain.Entities;

public class Client
{
    public ClientId Id { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;
    public ClientType Type { get; private set; }
    public Location Location { get; private set; } = null!;
    public ContactInfo? ContactInfo { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastContactedAt { get; private set; }
    public ClientStatus Status { get; private set; }
    public string? GooglePlaceId { get; private set; }
    public double? Rating { get; private set; }

    private Client() { } // EF Core

    public Client(string name, Address address, ClientType type, Location location, string? googlePlaceId = null)
    {
        Id = ClientId.New();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Address = address ?? throw new ArgumentNullException(nameof(address));
        Type = type;
        Location = location ?? throw new ArgumentNullException(nameof(location));
        GooglePlaceId = googlePlaceId;
        CreatedAt = DateTime.UtcNow;
        Status = ClientStatus.Prospect;
    }

    public void UpdateContactInfo(ContactInfo contactInfo)
    {
        ContactInfo = contactInfo;
    }

    public void UpdateRating(double rating)
    {
        Rating = rating;
    }

    public void MarkAsContacted()
    {
        LastContactedAt = DateTime.UtcNow;
        Status = ClientStatus.Contacted;
    }

    public void MarkAsConverted()
    {
        Status = ClientStatus.Converted;
    }

    public void MarkAsNotInterested()
    {
        Status = ClientStatus.NotInterested;
    }
}