using System.ComponentModel.DataAnnotations;

namespace Climato.Gateway.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "Jwt:Issuer não configurado. Ver docs/runbook.md.")]
    public string Issuer { get; init; } = string.Empty;

    [Required(ErrorMessage = "Jwt:Audience não configurado. Ver docs/runbook.md.")]
    public string Audience { get; init; } = string.Empty;

    [Required(ErrorMessage = "Jwt:SecretKey não configurada. Ver docs/runbook.md.")]
    [MinLength(32, ErrorMessage = "Jwt:SecretKey deve ter pelo menos 32 caracteres.")]
    public string SecretKey { get; init; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Jwt:ExpirationMinutes deve estar entre 1 e 1440.")]
    public int ExpirationMinutes { get; init; } = 60;
}
