using System.ComponentModel.DataAnnotations;

namespace Climato.Gateway.Application.Options;

public sealed class ProviderOptions
{
    public const string SectionName = "Provider";

    [Required(ErrorMessage = "Provider:BaseUrl não configurada. Ver docs/runbook.md.")]
    [Url(ErrorMessage = "Provider:BaseUrl deve ser uma URL absoluta.")]
    public string BaseUrl { get; init; } = string.Empty;
}
