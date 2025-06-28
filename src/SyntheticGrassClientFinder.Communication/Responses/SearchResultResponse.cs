namespace SyntheticGrassClientFinder.Communication.Responses;

public class SearchResultResponse
{
    public List<ClientResponse> Clients { get; set; } = new();
    public int TotalFound { get; set; }
    public int NewClients { get; set; }
    public int ExistingClients { get; set; }
    public string SearchLocation { get; set; } = string.Empty;
    public int RadiusKm { get; set; }
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, int> ClientsByType { get; set; } = new();
}