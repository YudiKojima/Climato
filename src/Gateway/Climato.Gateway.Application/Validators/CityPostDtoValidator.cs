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
            .MaximumLength(100).WithMessage("O nome da cidade deve ter no máximo 100 caracteres.");
    }
}
