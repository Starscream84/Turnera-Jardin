using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Dtos;

/// <summary>
/// Phone that <see cref="PhoneNumber.Normalize"/> can turn into a usable number.
/// Replaces [Phone], which accepted things like "1" or "(((1)))".
/// </summary>
public sealed class ArgentinePhoneAttribute : ValidationAttribute
{
    public ArgentinePhoneAttribute()
        : base("Ingresá un WhatsApp válido con característica, por ejemplo 223 555-1234 (o con +código de país si es del exterior).")
    {
    }

    public override bool IsValid(object? value) =>
        value is null || (value is string text && PhoneNumber.Normalize(text) is not null);
}

/// <summary>
/// Stricter email than [EmailAddress]: requires a domain with a dot and an extension
/// (rejects "juan@gmail", which Brevo can't deliver to).
/// </summary>
public sealed partial class StrictEmailAttribute : ValidationAttribute
{
    public StrictEmailAttribute() : base("El email no es válido. Revisalo (por ejemplo: nombre@gmail.com).")
    {
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)*\.[A-Za-z]{2,}$")]
    private static partial Regex EmailRegex();

    public override bool IsValid(object? value) =>
        value is null || (value is string text && EmailRegex().IsMatch(text.Trim()));
}
