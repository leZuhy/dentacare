namespace Market.Application.Modules.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Trenutna lozinka je obavezna.");

        RuleFor(x => x.NewPassword)
            .ValidPassword();
    }
}
