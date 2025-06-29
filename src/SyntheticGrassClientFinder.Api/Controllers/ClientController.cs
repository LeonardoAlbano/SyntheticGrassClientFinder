using Microsoft.AspNetCore.Mvc;
using SyntheticGrassClientFinder.Application.UseCases;
using SyntheticGrassClientFinder.Communication.Requests;
using SyntheticGrassClientFinder.Communication.Responses;
using SyntheticGrassClientFinder.Domain.Repositories;

namespace SyntheticGrassClientFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly ISearchClientsUseCase _searchClientsUseCase;
    private readonly IClientRepository _clientRepository;

    public ClientsController(ISearchClientsUseCase searchClientsUseCase, IClientRepository clientRepository)
    {
        _searchClientsUseCase = searchClientsUseCase;
        _clientRepository = clientRepository;
    }

    /// <summary>
    /// 🎯 Busca clientes potenciais para grama sintética usando dados públicos brasileiros
    /// </summary>
    /// <param name="request">Dados da busca (cidade, estado, raio)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de clientes potenciais encontrados</returns>
    /// <response code="200">Busca realizada com sucesso</response>
    /// <response code="400">Dados de entrada inválidos</response>
    /// <response code="500">Erro interno do servidor</response>
    [HttpPost("search")]
    [ProducesResponseType(typeof(SearchResultResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SearchResultResponse>> SearchClients(
        [FromBody] SearchClientsRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _searchClientsUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { 
                error = "Erro interno do servidor", 
                message = ex.Message,
                suggestion = "Tente com: Joinville/SC, Brusque/SC, Florianópolis/SC, Curitiba/PR"
            });
        }
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClientResponse>), 200)]
    public async Task<ActionResult<IEnumerable<ClientResponse>>> GetAllClients(CancellationToken cancellationToken)
    {
        var clients = await _clientRepository.GetAllAsync(cancellationToken);
        var response = clients.Select(client => new ClientResponse
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
        });

        return Ok(response);
    }
    
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<ActionResult<object>> GetStatistics(CancellationToken cancellationToken)
    {
        var totalClients = (await _clientRepository.GetAllAsync(cancellationToken)).Count();
        var prospects = await _clientRepository.CountByStatusAsync(Domain.Entities.ClientStatus.Prospect, cancellationToken);
        var contacted = await _clientRepository.CountByStatusAsync(Domain.Entities.ClientStatus.Contacted, cancellationToken);
        var converted = await _clientRepository.CountByStatusAsync(Domain.Entities.ClientStatus.Converted, cancellationToken);

        var statistics = new
        {
            TotalClients = totalClients,
            Prospects = prospects,
            Contacted = contacted,
            Converted = converted,
            ConversionRate = totalClients > 0 ? Math.Round((double)converted / totalClients * 100, 2) : 0,
            LastUpdated = DateTime.UtcNow,
            DataSources = new[] { 
                "OpenStreetMap", 
                "Dados Públicos Brasileiros", 
                "Base de Conhecimento SC/PR" 
            },
            SupportedCities = new[] { 
                "Joinville/SC", "Brusque/SC", "Florianópolis/SC", "Curitiba/PR" 
            },
            TargetAudience = new[] { 
                "Campos de Futebol", 
                "Quadras Society", 
                "Academias", 
                "Clubes Esportivos",
                "Escolas com Quadras",
                "Centros Esportivos"
            }
        };

        return Ok(statistics);
    }
    
    [HttpGet("demo")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult<object> GetDemoExamples()
    {
        var examples = new
        {
            message = "🇧🇷 Exemplos que FUNCIONAM AGORA (sem configuração)",
            readyToUse = new[]
            {
                new {
                    city = "brusque",
                    state = "sc",
                    radiusKm = 50,
                    description = "✅ Busca em Brusque/SC - FUNCIONA!"
                },
                new {
                    city = "joinville",
                    state = "sc",
                    radiusKm = 30,
                    description = "✅ Busca em Joinville/SC - FUNCIONA!"
                },
                new {
                    city = "florianópolis",
                    state = "sc",
                    radiusKm = 25,
                    description = "✅ Busca em Florianópolis/SC - FUNCIONA!"
                },
                new {
                    city = "curitiba",
                    state = "pr",
                    radiusKm = 40,
                    description = "✅ Busca em Curitiba/PR - FUNCIONA!"
                }
            },
            whatYouWillFind = new[] {
                "🏟️ Campos de Futebol reais",
                "⚽ Quadras Society existentes",
                "💪 Academias da região",
                "🏫 Escolas com quadras esportivas",
                "🏆 Clubes e centros esportivos",
                "📍 Endereços verificáveis"
            },
            dataSources = new[] {
                "OpenStreetMap (dados colaborativos mundiais)",
                "Dados públicos de estabelecimentos brasileiros",
                "Base de conhecimento local SC/PR"
            }
        };

        return Ok(examples);
    }
}
