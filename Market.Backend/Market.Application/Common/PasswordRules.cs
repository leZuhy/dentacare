namespace Market.Application.Common;

/// <summary>
/// Password rule shared by every validator that sets a new password (register, reset password, change password),
/// so the rule is defined in one place.
/// </summary>
public static class PasswordRules
{
    public const int MinLength = 8;

    /// <summary>
    /// At least <see cref="MinLength"/> characters, with at least one letter and one digit.
    /// Stops at the first failed check, so the client shows one message per field.
    /// Usage: <c>RuleFor(x => x.Password).ValidPassword();</c>
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilderInitial<T, string> rule)
    {
        return rule
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Lozinka je obavezna.")
            .MinimumLength(MinLength).WithMessage($"Lozinka mora imati najmanje {MinLength} znakova.")
            .Must(p => p.Any(char.IsLetter) && p.Any(char.IsDigit))
            .WithMessage("Lozinka mora sadržavati bar jedno slovo i jednu cifru.");
    }
}
