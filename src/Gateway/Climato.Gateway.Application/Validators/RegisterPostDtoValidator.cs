using Climato.Gateway.Application.Model.Auth;
using FluentValidation;

namespace Climato.Gateway.Application.Validators;

public class RegisterPostDtoValidator : AbstractValidator<RegisterPostDto>
{
    public RegisterPostDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o e-mail.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(255).WithMessage("O e-mail deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe o nome.")
            .MaximumLength(255).WithMessage("O nome deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Informe a senha.")
            .Length(8, 128).WithMessage("A senha deve ter entre 8 e 128 caracteres.");
    }
}
