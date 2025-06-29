using System.Text.Json;
using System.Web;
using SyntheticGrassClientFinder.Application.Services;
using SyntheticGrassClientFinder.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace SyntheticGrassClientFinder.Infrastructure.Services;

public class FixedOpenStreetMapGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FixedOpenStreetMapGeocodingService> _logger;

    public FixedOpenStreetMapGeocodingService(HttpClient httpClient, ILogger<FixedOpenStreetMapGeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "SyntheticGrassClientFinder/1.0 (contato@exemplo.com)");
    }

    public async Task<Location?> GetLocationAsync(string address, CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedAddress = HttpUtility.UrlEncode(address);
            var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=1&countrycodes=br";

            _logger.LogInformation("🔍 Geocoding request for address: {Address}", address);
            _logger.LogInformation("📡 URL: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Geocoding HTTP Error - StatusCode: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("📡 Raw response: {Content}", content.Substring(0, Math.Min(200, content.Length)));

            var results = JsonSerializer.Deserialize<NominatimResult[]>(content);

            if (results != null && results.Length > 0)
            {
                var result = results[0];
                
                _logger.LogInformation("🔍 Raw coordinates from Nominatim: lat='{Lat}', lng='{Lng}'", result.lat, result.lon);
                
                if (double.TryParse(result.lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(result.lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                {
                    _logger.LogInformation("✅ Parsed coordinates: lat={Lat}, lng={Lng}", lat, lng);
                    
                    if (lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180)
                    {
                        _logger.LogInformation("✅ Coordinates are valid for {Address}: {Lat}, {Lng}", address, lat, lng);
                        return new Location(lat, lng);
                    }
                    else
                    {
                        _logger.LogError("❌ Invalid coordinate range: lat={Lat}, lng={Lng}", lat, lng);
                        return null;
                    }
                }
                else
                {
                    _logger.LogError("❌ Failed to parse coordinates: lat='{Lat}', lng='{Lng}'", result.lat, result.lon);
                    return null;
                }
            }

            _logger.LogWarning("⚠️ No location found for address: {Address}", address);
            return null;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "❌ Exception in geocoding service for address: {Address}", address);
            throw new InvalidOperationException($"Erro ao buscar localização para {address}: {ex.Message}");
        }
    }

    private class NominatimResult
    {
        public string lat { get; set; } = "";
        public string lon { get; set; } = "";
        public string display_name { get; set; } = "";
    }
}
