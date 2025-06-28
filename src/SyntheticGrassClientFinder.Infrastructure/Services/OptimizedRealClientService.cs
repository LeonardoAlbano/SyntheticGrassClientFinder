using System.Text.Json;
using System.Web;
using System.Globalization;
using SyntheticGrassClientFinder.Domain.Services;
using SyntheticGrassClientFinder.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SyntheticGrassClientFinder.Infrastructure.Services;

public class OptimizedRealClientService : IPlacesSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OptimizedRealClientService> _logger;
    private readonly string _hereApiKey;

    public OptimizedRealClientService(HttpClient httpClient, IConfiguration configuration, ILogger<OptimizedRealClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _hereApiKey = configuration["Here:ApiKey"] ?? "";
        
        if (!string.IsNullOrEmpty(_hereApiKey))
        {
            _logger.LogInformation("🔑 HERE API configurada - FORMATO CORRIGIDO");
        }
    }

    public async Task<IEnumerable<PlaceSearchResult>> SearchPlacesAsync(
        string query, 
        Location location, 
        int radiusMeters, 
        CancellationToken cancellationToken = default)
    {
        var allResults = new List<PlaceSearchResult>();

        try
        {
            _logger.LogInformation("🎯 Buscando CLIENTES REAIS - Coordenadas: {Lat}, {Lng}", 
                location.Latitude.ToString(CultureInfo.InvariantCulture), 
                location.Longitude.ToString(CultureInfo.InvariantCulture));

            // 1. HERE API com formato correto
            if (!string.IsNullOrEmpty(_hereApiKey))
            {
                var hereResults = await SearchHEREOptimized(location, radiusMeters, cancellationToken);
                allResults.AddRange(hereResults);
                _logger.LogInformation("🗺️ HERE API encontrou: {Count} clientes reais", hereResults.Count());
            }

            // 2. OpenStreetMap como backup
            if (allResults.Count < 10)
            {
                var osmResults = await SearchOSMOptimized(location, cancellationToken);
                allResults.AddRange(osmResults);
                _logger.LogInformation("🌍 OpenStreetMap adicionou: {Count} clientes", osmResults.Count());
            }

            var uniqueResults = RemoveDuplicatesOptimized(allResults);
            
            _logger.LogInformation("✅ Total de CLIENTES REAIS: {Count}", uniqueResults.Count());
            return uniqueResults;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "❌ Erro na busca");
            return new List<PlaceSearchResult>();
        }
    }

    private async Task<IEnumerable<PlaceSearchResult>> SearchHEREOptimized(
        Location location, 
        int radiusMeters, 
        CancellationToken cancellationToken)
    {
        var results = new List<PlaceSearchResult>();

        // Categorias mais específicas e eficientes
        var categories = new[]
        {
            "school", "gym", "fitness", "hotel", "sports"
        };

        foreach (var category in categories)
        {
            try
            {
                // FORMATO CORRETO com InvariantCulture
                var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
                var lng = location.Longitude.ToString(CultureInfo.InvariantCulture);
                
                var url = $"https://discover.search.hereapi.com/v1/discover" +
                         $"?at={lat},{lng}" +
                         $"&limit=10" +
                         $"&q={category}" +
                         $"&apiKey={_hereApiKey}";

                _logger.LogInformation("📡 HERE URL: {Url}", url.Replace(_hereApiKey, "***"));

                var response = await _httpClient.GetAsync(url, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var hereResponse = JsonSerializer.Deserialize<HereResponse>(content);

                    if (hereResponse?.items != null)
                    {
                        foreach (var item in hereResponse.items)
                        {
                            if (item.position != null && !string.IsNullOrEmpty(item.title))
                            {
                                var placeLocation = new Location(item.position.lat, item.position.lng);
                                var address = ValidateAndImproveAddress(BuildCompleteAddress(item.address, item.title), item.title);
                                var phone = ExtractPhone(item.contacts);
                                var website = ExtractWebsite(item.contacts);

                                // Log para debug
                                _logger.LogInformation("📍 {Name} - {Address} - Tel: {Phone}", 
                                    item.title, address, phone ?? "N/A");

                                results.Add(new PlaceSearchResult(
                                    TruncateName(item.title),
                                    address,
                                    placeLocation,
                                    PhoneNumber: phone,
                                    Website: website
                                ));
                            }
                        }
                    }
                    
                    _logger.LogInformation("✅ {Category}: {Count} lugares encontrados", category, hereResponse?.items?.Length ?? 0);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("⚠️ HERE erro para '{Category}': {StatusCode} - {Error}", 
                        category, response.StatusCode, errorContent.Substring(0, Math.Min(100, errorContent.Length)));
                }

                await Task.Delay(500, cancellationToken); // Rate limiting
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Erro categoria: {Category}", category);
            }
        }

        return results;
    }

    private async Task<IEnumerable<PlaceSearchResult>> SearchOSMOptimized(
        Location location, 
        CancellationToken cancellationToken)
    {
        try
        {
            var results = new List<PlaceSearchResult>();
            
            // Busca mais específica no OSM
            var queries = new[] { "escola joinville", "academia joinville", "hotel joinville" };

            foreach (var query in queries)
            {
                try
                {
                    var encodedQuery = HttpUtility.UrlEncode(query);
                    var url = $"https://nominatim.openstreetmap.org/search" +
                             $"?q={encodedQuery}" +
                             $"&format=json" +
                             $"&limit=3" +
                             $"&countrycodes=br";

                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "SyntheticGrassClientFinder/1.0");

                    var response = await _httpClient.GetAsync(url, cancellationToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(cancellationToken);
                        var places = JsonSerializer.Deserialize<NominatimPlace[]>(content);

                        if (places != null)
                        {
                            foreach (var place in places)
                            {
                                if (double.TryParse(place.lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) && 
                                    double.TryParse(place.lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                                {
                                    var placeLocation = new Location(lat, lng);
                                    var name = ExtractCleanName(place.display_name);
                                    
                                    if (IsValidClient(name))
                                    {
                                        results.Add(new PlaceSearchResult(
                                            TruncateName(name),
                                            TruncateAddress(place.display_name),
                                            placeLocation
                                        ));
                                    }
                                }
                            }
                        }
                    }

                    await Task.Delay(2000, cancellationToken); // Rate limiting OSM
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Erro OSM: {Query}", query);
                }
            }

            return results;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "❌ Erro OSM geral");
            return new List<PlaceSearchResult>();
        }
    }

    private string TruncateName(string name)
    {
        // Truncar para 50 caracteres para evitar erro de DB
        return name.Length > 50 ? name.Substring(0, 47) + "..." : name;
    }

    private string TruncateAddress(string address)
    {
        // Truncar endereço para 200 caracteres
        return address.Length > 200 ? address.Substring(0, 197) + "..." : address;
    }

    private string BuildCompleteAddress(HereAddress? address, string title)
    {
        if (address == null) return $"{title}, Brasil";

        var parts = new List<string>();
        
        // Rua com número (mais específico)
        if (!string.IsNullOrEmpty(address.street))
        {
            var streetPart = address.street;
            if (!string.IsNullOrEmpty(address.houseNumber))
            {
                streetPart = $"{address.street}, {address.houseNumber}";
            }
            parts.Add(streetPart);
        }
        
        // Bairro/Distrito
        if (!string.IsNullOrEmpty(address.district))
            parts.Add(address.district);
        
        // Cidade
        if (!string.IsNullOrEmpty(address.city))
            parts.Add(address.city);
        else
            parts.Add("Joinville"); // Fallback
        
        // Estado
        if (!string.IsNullOrEmpty(address.state))
            parts.Add(address.state);
        else
            parts.Add("SC"); // Fallback
        
        // CEP se disponível
        if (!string.IsNullOrEmpty(address.postalCode))
            parts.Add($"CEP: {address.postalCode}");

        var result = parts.Any() ? string.Join(", ", parts) + ", Brasil" : $"{title}, Joinville, SC, Brasil";
        return TruncateAddress(result);
    }

    private string ExtractCleanName(string displayName)
    {
        var parts = displayName.Split(',');
        return parts.Length > 0 ? parts[0].Trim() : displayName;
    }

    private bool IsValidClient(string name)
    {
        var lowerName = name.ToLower();
        var validKeywords = new[] { "escola", "academia", "hotel", "creche", "condominio", "clube", "centro" };
        return validKeywords.Any(keyword => lowerName.Contains(keyword));
    }

    private string? ExtractPhone(HereContact[]? contacts)
    {
        return contacts?.FirstOrDefault()?.phone?.FirstOrDefault()?.value;
    }

    private string? ExtractWebsite(HereContact[]? contacts)
    {
        return contacts?.FirstOrDefault()?.www?.FirstOrDefault()?.value;
    }

    private IEnumerable<PlaceSearchResult> RemoveDuplicatesOptimized(List<PlaceSearchResult> results)
    {
        var unique = new List<PlaceSearchResult>();
        
        foreach (var result in results)
        {
            var isDuplicate = unique.Any(u => 
                u.Name.Equals(result.Name, StringComparison.OrdinalIgnoreCase) ||
                u.Location.DistanceTo(result.Location) < 0.1);

            if (!isDuplicate)
            {
                unique.Add(result);
            }
        }

        return unique.OrderBy(r => GetPriority(r.Name)).ThenBy(r => r.Name);
    }

    private int GetPriority(string name)
    {
        var lowerName = name.ToLower();
        if (lowerName.Contains("escola") || lowerName.Contains("academia")) return 1;
        if (lowerName.Contains("hotel") || lowerName.Contains("clube")) return 2;
        return 3;
    }

    // Classes HERE API
    private class HereResponse
    {
        public HereItem[]? items { get; set; }
    }

    private class HereItem
    {
        public string? title { get; set; }
        public HerePosition? position { get; set; }
        public HereAddress? address { get; set; }
        public HereContact[]? contacts { get; set; }
    }

    private class HerePosition
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    private class HereAddress
    {
        public string? street { get; set; }
        public string? houseNumber { get; set; }
        public string? district { get; set; }
        public string? city { get; set; }
        public string? state { get; set; }
        public string? postalCode { get; set; }
        public string? countryName { get; set; }
    }

    private class HereContact
    {
        public HerePhone[]? phone { get; set; }
        public HereWebsite[]? www { get; set; }
    }

    private class HerePhone
    {
        public string? value { get; set; }
    }

    private class HereWebsite
    {
        public string? value { get; set; }
    }

    private class NominatimPlace
    {
        public string lat { get; set; } = "";
        public string lon { get; set; } = "";
        public string display_name { get; set; } = "";
    }

    private string ValidateAndImproveAddress(string address, string placeName)
    {
        // Se o endereço está muito vago, tentar melhorar
        if (address.Contains("Brasil") && address.Split(',').Length < 4)
        {
            // Endereço muito simples, adicionar mais contexto
            return $"{placeName}, Centro, Joinville, SC, Brasil";
        }
        
        // Se não tem número, tentar adicionar "s/n"
        if (!address.Contains(",") || (!Char.IsDigit(address[0]) && !address.Contains("nº") && !address.Contains("s/n")))
        {
            var parts = address.Split(',');
            if (parts.Length > 0)
            {
                parts[0] = $"{parts[0]}, s/n";
                return string.Join(", ", parts);
            }
        }
        
        return address;
    }
}
