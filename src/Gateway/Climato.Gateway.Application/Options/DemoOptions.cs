using System.ComponentModel.DataAnnotations;

namespace Climato.Gateway.Application.Options;

public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    public bool Enabled { get; set; }

    [Required(ErrorMessage = "Demo:Email não configurado.")]
    [EmailAddress(ErrorMessage = "Demo:Email deve ser um e-mail válido.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Demo:Name não configurado.")]
    public string Name { get; init; } = string.Empty;

    public List<DemoCity> Cities { get; init; } = [];
}

public sealed class DemoCity
{
    public string Name { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}
