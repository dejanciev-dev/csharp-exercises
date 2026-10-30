using System.Diagnostics.CodeAnalysis;

namespace CSharpExercises.Formats;

/// <summary>
/// Semantic versions (semver.org): MAJOR.MINOR.PATCH, an optional -prerelease and +build metadata. Precedence has
/// details people get wrong: 1.0.0-alpha &lt; 1.0.0, numeric identifiers compare as numbers (beta.2 &lt; beta.11),
/// and build metadata is ignored.
/// </summary>
public sealed record SemanticVersion(int Major, int Minor, int Patch, string Prerelease = "", string Build = "")
    : IComparable<SemanticVersion>, IParsable<SemanticVersion>
{
    public bool IsPrerelease => Prerelease.Length > 0;

    public static SemanticVersion Parse(string s, IFormatProvider? provider = null) =>
        TryParse(s, provider, out var version) ? version : throw new FormatException($"'{s}' is not a valid semantic version.");

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out SemanticVersion result)
    {
        result = null;
        if (string.IsNullOrEmpty(s)) return false;

        var (core, build) = Split(s, '+');
        var (numbers, prerelease) = Split(core, '-');
        if (numbers.Split('.') is not [var major, var minor, var patch]) return false;
        if (!TryNumber(major, out var ma) || !TryNumber(minor, out var mi) || !TryNumber(patch, out var pa)) return false;
        if (prerelease is not null && !ValidIdentifiers(prerelease, numericNoLeadingZero: true)) return false;
        if (build is not null && !ValidIdentifiers(build, numericNoLeadingZero: false)) return false;

        result = new SemanticVersion(ma, mi, pa, prerelease ?? "", build ?? "");
        return true;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null) return 1;
        var core = (Major, Minor, Patch).CompareTo((other.Major, other.Minor, other.Patch));
        if (core != 0) return core;

        // A pre-release sorts before the release it leads up to.
        if (IsPrerelease != other.IsPrerelease) return IsPrerelease ? -1 : 1;

        var mine = Prerelease.Split('.');
        var theirs = other.Prerelease.Split('.');
        for (var i = 0; i < Math.Min(mine.Length, theirs.Length); i++)
        {
            var cmp = CompareIdentifier(mine[i], theirs[i]);
            if (cmp != 0) return cmp;
        }
        return mine.Length.CompareTo(theirs.Length); // alpha < alpha.1
    }

    /// <summary>npm-style caret range: ^1.2.3 allows anything from 1.2.3 below 2.0.0, without crossing the major.</summary>
    public bool SatisfiesCaret(SemanticVersion minimum) =>
        CompareTo(minimum) >= 0 &&
        (minimum.Major > 0 ? Major == minimum.Major
            : minimum.Minor > 0 ? Major == 0 && Minor == minimum.Minor
            : Major == 0 && Minor == 0 && Patch == minimum.Patch) &&
        (!IsPrerelease || (Major, Minor, Patch) == (minimum.Major, minimum.Minor, minimum.Patch));

    public SemanticVersion BumpMajor() => new(Major + 1, 0, 0);
    public SemanticVersion BumpMinor() => new(Major, Minor + 1, 0);
    public SemanticVersion BumpPatch() => IsPrerelease ? this with { Prerelease = "", Build = "" } : new(Major, Minor, Patch + 1);

    public override string ToString() =>
        $"{Major}.{Minor}.{Patch}{(IsPrerelease ? "-" + Prerelease : "")}{(Build.Length > 0 ? "+" + Build : "")}";

    public static bool operator <(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) < 0;
    public static bool operator >(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) > 0;
    public static bool operator <=(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) <= 0;
    public static bool operator >=(SemanticVersion a, SemanticVersion b) => a.CompareTo(b) >= 0;

    private static int CompareIdentifier(string a, string b)
    {
        var aNumeric = int.TryParse(a, out var x);
        var bNumeric = int.TryParse(b, out var y);
        if (aNumeric && bNumeric) return x.CompareTo(y);
        if (aNumeric != bNumeric) return aNumeric ? -1 : 1; // numbers sort before words
        return string.CompareOrdinal(a, b);
    }

    private static (string Before, string? After) Split(string s, char separator)
    {
        var index = s.IndexOf(separator);
        return index < 0 ? (s, null) : (s[..index], s[(index + 1)..]);
    }

    private static bool TryNumber(string s, out int value) =>
        int.TryParse(s, System.Globalization.NumberStyles.None, null, out value) && (s == "0" || s[0] != '0');

    private static bool ValidIdentifiers(string s, bool numericNoLeadingZero) =>
        s.Split('.').All(id =>
            id.Length > 0 &&
            id.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') &&
            !(numericNoLeadingZero && id.All(char.IsAsciiDigit) && id.Length > 1 && id[0] == '0'));
}
