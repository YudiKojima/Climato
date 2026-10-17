using System.ComponentModel.DataAnnotations;

namespace Climato.Provider.Application.Options;

public sealed class OpenWeatherOptions
{
    public const string SectionName = "OpenWeather";

    [Required(ErrorMessage = "OpenWeather:BaseUrl não configurada. Ver docs/runbook.md.")]
    [Url(ErrorMessage = "OpenWeather:BaseUrl deve ser uma URL absoluta.")]
    public string BaseUrl { get; init; } = string.Empty;

    [Required(ErrorMessage = "OpenWeather:ApiKey não configurada. Ver docs/runbook.md.")]
    public string ApiKey { get; init; } = string.Empty;
}
