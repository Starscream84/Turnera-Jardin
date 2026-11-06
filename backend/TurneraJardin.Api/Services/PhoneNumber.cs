namespace TurneraJardin.Api.Services;

/// <summary>
/// Normalizes the phone numbers families type to a single format: E.164 without "+".
/// For Argentina that is 549 + area code + number (13 digits), the format wa.me and the WhatsApp API expect.
///
/// Accepted inputs (all become 5492235551234):
///   2235551234 · 0223 555-1234 · (223) 555-1234 · 223 15 555-1234 · 0223 15 555 1234
///   +54 9 223 555-1234 · +54 223 555 1234 · 5492235551234 · 00 54 9 223 555 1234
/// A number from abroad must start with "+" (or "00") and is kept as is (8 to 15 digits).
/// Anything else (too short, letters, non-ASCII digits, an Argentine number without area code) is invalid.
///
/// The frontend has the same logic in shared/phone.util.ts: keep both in sync.
/// </summary>
public static class PhoneNumber
{
    private const string AllowedSymbols = " +-().";

    /// <summary>Returns the normalized number, or null if it is not a valid phone.</summary>
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim();
        foreach (var c in text)
        {
            // char.IsDigit would also accept Arabic-Indic and other Unicode digits: only 0-9 are valid.
            if (!char.IsAsciiDigit(c) && !AllowedSymbols.Contains(c))
            {
                return null;
            }
        }

        var international = text.StartsWith('+');
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());

        if (digits.StartsWith("00"))
        {
            digits = digits[2..];
            international = true;
        }

        string national;
        if (digits.StartsWith("54") && (international || digits.Length >= 12))
        {
            national = digits[2..];
            if (national.StartsWith('9'))
            {
                national = national[1..];
            }
        }
        else if (international)
        {
            // Number from another country: we can't validate its format, only its length.
            return digits.Length is >= 8 and <= 15 ? digits : null;
        }
        else
        {
            national = digits;
        }

        if (national.StartsWith('0'))
        {
            national = national[1..];
        }

        // Mobile written the old way: area code + "15" + number (12 digits). The "15" is dropped.
        // Area codes in Argentina have 2 (11), 3 (223) or 4 (2291) digits.
        if (national.Length == 12)
        {
            for (var areaLength = 2; areaLength <= 4; areaLength++)
            {
                if (national.Substring(areaLength, 2) == "15")
                {
                    national = national[..areaLength] + national[(areaLength + 2)..];
                    break;
                }
            }
        }

        return national.Length == 10 ? $"549{national}" : null;
    }

    /// <summary>
    /// True if both numbers are the same phone. Compares the normalized form and, for numbers saved
    /// before normalization existed that can't be normalized, falls back to comparing the raw digits.
    /// </summary>
    public static bool SameNumber(string? entered, string? stored)
    {
        var a = Normalize(entered);
        var b = Normalize(stored);
        if (a is not null && b is not null)
        {
            return a == b;
        }

        var digitsA = new string((entered ?? "").Where(char.IsAsciiDigit).ToArray());
        var digitsB = new string((stored ?? "").Where(char.IsAsciiDigit).ToArray());
        return digitsA.Length > 0 && digitsA == digitsB;
    }
}
