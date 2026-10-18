using Climato.Gateway.Application.Model.Auth;
using FluentValidation;

namespace Climato.Gateway.Application.Validators;

public class LoginPostDtoValidator : AbstractValidator<LoginPostDto>
{
    public LoginPostDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o e-mail.")
            .MaximumLength(255).WithMessage("O e-mail deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe a senha.")
            .MaximumLength(128).WithMessage("A senha deve ter no máximo 128 caracteres.");
    }
}
