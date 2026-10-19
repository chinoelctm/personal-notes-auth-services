using FluentValidation;

namespace PersonalNotes.Auth.Application.Commands.Auth.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("El usuario es obligatorio.")
                .MaximumLength(100).WithMessage("El usuario no es válido.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.")
                .MaximumLength(200).WithMessage("La contraseña no es válida.");
        }
    }
}
