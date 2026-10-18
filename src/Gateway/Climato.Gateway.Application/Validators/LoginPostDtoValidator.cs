using Climato.Gateway.Application.Model.Auth;
using FluentValidation;

namespace Climato.Gateway.Application.Validators;

public class LoginPostDtoValidator : AbstractValidator<LoginPostDto>
{
    public LoginPostDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Informe o e-mail.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Informe a senha.");
    }
}
