using System.ComponentModel.DataAnnotations;

namespace SyntheticGrassClientFinder.Communication.Requests;

public class SearchClientsRequest
{
    [Required(ErrorMessage = "Cidade é obrigatória")]
    [StringLength(100, ErrorMessage = "Cidade deve ter no máximo 100 caracteres")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Estado é obrigatório")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Estado deve ter exatamente 2 caracteres")]
    public string State { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Raio deve estar entre 1 e 100 km")]
    public int RadiusKm { get; set; } = 50;

    public List<string> Keywords { get; set; } = new();
}