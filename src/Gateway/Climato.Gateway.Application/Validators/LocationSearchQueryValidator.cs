using Climato.Gateway.Application.Model.Locations;
using FluentValidation;

namespace Climato.Gateway.Application.Validators;

public class LocationSearchQueryValidator : AbstractValidator<LocationSearchQuery>
{
    public LocationSearchQueryValidator()
    {
        RuleFor(x => x.Query)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Parâmetro 'query' deve ter entre 2 e 100 caracteres.")
            .Must(query => query!.Trim().Length is >= 2 and <= 100).WithMessage("Parâmetro 'query' deve ter entre 2 e 100 caracteres.");
    }
}
