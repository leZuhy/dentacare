namespace Market.Application.Modules.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email je obavezan.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(ResetPasswordCommandHandler.InvalidTokenMessage);

        RuleFor(x => x.NewPassword)
            .ValidPassword();
    }
}
