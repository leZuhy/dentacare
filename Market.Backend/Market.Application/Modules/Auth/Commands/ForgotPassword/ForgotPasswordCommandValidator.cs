namespace Market.Application.Modules.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        // Only the format is checked: whether the account exists must not change the response.
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email je obavezan.")
            .MaximumLength(MarketUserEntity.Constraints.EmailMaxLength)
            .WithMessage($"Email može imati najviše {MarketUserEntity.Constraints.EmailMaxLength} znakova.")
            .EmailAddress().WithMessage("Email nije ispravan.");
    }
}
