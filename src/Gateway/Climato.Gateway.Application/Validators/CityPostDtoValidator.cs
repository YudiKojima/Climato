using Climato.Gateway.Application.Model.Cities;
using FluentValidation;

namespace Climato.Gateway.Application.Validators;

public class CityPostDtoValidator : AbstractValidator<CityPostDto>
{
    public CityPostDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o nome da cidade.")
            .MaximumLength(255).WithMessage("O nome da cidade deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Country)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o país.")
            .MaximumLength(255).WithMessage("O país deve ter no máximo 255 caracteres.");
    }
}
