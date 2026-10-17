using System.ComponentModel.DataAnnotations;

namespace Climato.Provider.Application.Options;

public sealed class PexelsOptions
{
    public const string SectionName = "Pexels";

    [Required(ErrorMessage = "Pexels:BaseUrl não configurada. Ver docs/runbook.md.")]
    [Url(ErrorMessage = "Pexels:BaseUrl deve ser uma URL absoluta.")]
    public string BaseUrl { get; init; } = string.Empty;

    [Required(ErrorMessage = "Pexels:ApiKey não configurada. Ver docs/runbook.md.")]
    public string ApiKey { get; init; } = string.Empty;
}
