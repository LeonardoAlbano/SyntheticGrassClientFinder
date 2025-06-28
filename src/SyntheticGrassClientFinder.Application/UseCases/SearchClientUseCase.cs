using SyntheticGrassClientFinder.Application.Services;
using SyntheticGrassClientFinder.Communication.Requests;
using SyntheticGrassClientFinder.Communication.Responses;
using SyntheticGrassClientFinder.Domain.Entities;
using SyntheticGrassClientFinder.Domain.Repositories;
using SyntheticGrassClientFinder.Domain.Services;
using SyntheticGrassClientFinder.Domain.ValueObjects;

namespace SyntheticGrassClientFinder.Application.UseCases;

public interface ISearchClientsUseCase
{
    Task<SearchResultResponse> ExecuteAsync(SearchClientsRequest request, CancellationToken cancellationToken = default);
}

public class SearchClientsUseCase : ISearchClientsUseCase
{
    private readonly IPlacesSearchService _placesSearchService;
    private readonly IClientRepository _clientRepository;
    private readonly IGeocodingService _geocodingService;

    private static readonly Dictionary<string, ClientType> KeywordToClientType = new()
    {
        { "campo de futebol", ClientType.SoccerSchool },
        { "quadra society", ClientType.SportsClub },
        { "escolinha de futebol", ClientType.SoccerSchool },
        { "condomínio com quadra", ClientType.Condominium },
        { "academia", ClientType.Company },
        { "clube esportivo", ClientType.SportsClub }
    };

    public SearchClientsUseCase(
        IPlacesSearchService placesSearchService,
        IClientRepository clientRepository,
        IGeocodingService geocodingService)
    {
        _placesSearchService = placesSearchService;
        _clientRepository = clientRepository;
        _geocodingService = geocodingService;
    }

    public async Task<SearchResultResponse> ExecuteAsync(SearchClientsRequest request, CancellationToken cancellationToken = default)
    {
        var centerLocation = await _geocodingService.GetLocationAsync($"{request.City}, {request.State}", cancellationToken);
        if (centerLocation == null)
            throw new InvalidOperationException($"Não foi possível encontrar a localização para {request.City}, {request.State}");

        var keywords = request.Keywords.Any() ? request.Keywords : GetDefaultKeywords();
        var newClients = new List<Client>();
        var existingCount = 0;

        foreach (var keyword in keywords)
        {
            var query = $"{keyword} em {request.City} {request.State}";
            var searchResults = await _placesSearchService.SearchPlacesAsync(
                query, 
                centerLocation, 
                request.RadiusKm * 1000, 
                cancellationToken);

            foreach (var result in searchResults)
            {
                var address = ParseAddress(result.FormattedAddress);
                var clientType = DetermineClientType(keyword, result.Name);

                if (await _clientRepository.ExistsAsync(result.Name, address, cancellationToken))
                {
                    existingCount++;
                    continue;
                }

                var client = new Client(result.Name, address, clientType, result.Location, result.PlaceId);
                
                if (!string.IsNullOrEmpty(result.PhoneNumber) || !string.IsNullOrEmpty(result.Website))
                {
                    var contactInfo = new ContactInfo(
                        Phone: result.PhoneNumber,
                        Website: result.Website);
                    client.UpdateContactInfo(contactInfo);
                }

                if (result.Rating.HasValue)
                {
                    client.UpdateRating(result.Rating.Value);
                }

                newClients.Add(client);
            }
        }

        if (newClients.Any())
        {
            await _clientRepository.AddRangeAsync(newClients, cancellationToken);
        }

        var allClients = newClients.Concat(await _clientRepository.GetByCityAsync(request.City, request.State, cancellationToken));
        var clientResponses = allClients.Select(MapToResponse).ToList();

        return new SearchResultResponse
        {
            Clients = clientResponses,
            TotalFound = clientResponses.Count,
            NewClients = newClients.Count,
            ExistingClients = existingCount,
            SearchLocation = $"{request.City}, {request.State}",
            RadiusKm = request.RadiusKm,
            ClientsByType = clientResponses.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private static List<string> GetDefaultKeywords() => new()
    {
        "campo de futebol",
        "quadra society",
        "escolinha de futebol",
        "condomínio com quadra",
        "academia",
        "clube esportivo"
    };

    private static ClientType DetermineClientType(string keyword, string placeName)
    {
        if (KeywordToClientType.TryGetValue(keyword.ToLower(), out var type))
            return type;

        var lowerName = placeName.ToLower();
        if (lowerName.Contains("condomínio") || lowerName.Contains("residencial"))
            return ClientType.Condominium;
        if (lowerName.Contains("escola") || lowerName.Contains("escolinha"))
            return ClientType.SoccerSchool;
        if (lowerName.Contains("clube") || lowerName.Contains("associação"))
            return ClientType.SportsClub;

        return ClientType.Other;
    }

    private static Address ParseAddress(string formattedAddress)
    {
        var parts = formattedAddress.Split(',').Select(p => p.Trim()).ToArray();
        
        return new Address(
            Street: parts.Length > 0 ? parts[0] : "",
            City: parts.Length > 1 ? parts[1].Split('-')[0].Trim() : "",
            State: parts.Length > 1 && parts[1].Contains('-') ? parts[1].Split('-')[1].Trim() : "",
            PostalCode: parts.Length > 2 ? parts[2] : "",
            Country: "Brasil");
    }

    private static ClientResponse MapToResponse(Client client)
    {
        return new ClientResponse
        {
            Id = client.Id.Value,
            Name = client.Name,
            Address = client.Address.FormattedAddress,
            Type = client.Type.ToString(),
            Latitude = client.Location.Latitude,
            Longitude = client.Location.Longitude,
            Phone = client.ContactInfo?.Phone,
            Email = client.ContactInfo?.Email,
            Website = client.ContactInfo?.Website,
            CreatedAt = client.CreatedAt,
            Status = client.Status.ToString(),
            Rating = client.Rating,
            GooglePlaceId = client.GooglePlaceId
        };
    }
}
