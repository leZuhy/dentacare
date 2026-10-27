namespace Market.Application.Modules.Auth.Commands.Refresh;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token je obavezan.");

        RuleFor(x => x.Fingerprint)
            .MaximumLength(256).WithMessage("Fingerprint može imati najviše 256 znakova.")
            .When(x => x.Fingerprint is not null);
    }
}