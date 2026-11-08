using System;
using System.Globalization;

namespace AntiCheat.Core;

/// <summary>Strict SemVer precedence for release tags, with an optional lowercase v prefix.</summary>
public sealed class ReleaseVersion : IComparable<ReleaseVersion>
{
    private readonly string[] prereleaseIdentifiers;

    private ReleaseVersion(string originalTag, int major, int minor, int patch, string[] prerelease)
    {
        OriginalTag = originalTag;
        Major = major;
        Minor = minor;
        Patch = patch;
        prereleaseIdentifiers = prerelease;
    }

    public string OriginalTag { get; }
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public bool IsPrerelease => prereleaseIdentifiers.Length != 0;

    public static bool TryParse(string? value, out ReleaseVersion? version)
    {
        version = null;
        if (string.IsNullOrEmpty(value)) return false;

        string semanticVersion = value![0] == 'v' ? value.Substring(1) : value;
        int metadataStart = semanticVersion.IndexOf('+');
        if (metadataStart >= 0)
        {
            if (!TryIdentifiers(semanticVersion.Substring(metadataStart + 1), false, out _))
                return false;
            semanticVersion = semanticVersion.Substring(0, metadataStart);
        }

        string[] prerelease = Array.Empty<string>();
        int prereleaseStart = semanticVersion.IndexOf('-');
        if (prereleaseStart >= 0)
        {
            if (!TryIdentifiers(semanticVersion.Substring(prereleaseStart + 1), true, out prerelease))
                return false;
            semanticVersion = semanticVersion.Substring(0, prereleaseStart);
        }

        string[] fields = semanticVersion.Split('.');
        if (fields.Length != 3 || !TryNumber(fields[0], out int major) ||
            !TryNumber(fields[1], out int minor) || !TryNumber(fields[2], out int patch))
            return false;

        version = new ReleaseVersion(value, major, minor, patch, prerelease);
        return true;
    }

    public int CompareTo(ReleaseVersion? other)
    {
        if (other == null) return 1;
        int comparison = Major.CompareTo(other.Major);
        if (comparison != 0) return comparison;
        comparison = Minor.CompareTo(other.Minor);
        if (comparison != 0) return comparison;
        comparison = Patch.CompareTo(other.Patch);
        if (comparison != 0) return comparison;
        if (!IsPrerelease) return other.IsPrerelease ? 1 : 0;
        if (!other.IsPrerelease) return -1;

        int count = Math.Min(prereleaseIdentifiers.Length, other.prereleaseIdentifiers.Length);
        for (int i = 0; i < count; i++)
        {
            string left = prereleaseIdentifiers[i];
            string right = other.prereleaseIdentifiers[i];
            bool leftNumeric = IsNumeric(left);
            bool rightNumeric = IsNumeric(right);
            if (leftNumeric && rightNumeric)
            {
                // Numeric identifiers can exceed Int64; valid identifiers have no leading zeroes.
                comparison = left.Length.CompareTo(right.Length);
                if (comparison == 0) comparison = string.CompareOrdinal(left, right);
            }
            else if (leftNumeric != rightNumeric)
                comparison = leftNumeric ? -1 : 1;
            else
                comparison = string.CompareOrdinal(left, right);
            if (comparison != 0) return comparison;
        }
        return prereleaseIdentifiers.Length.CompareTo(other.prereleaseIdentifiers.Length);
    }

    public override string ToString() => OriginalTag;

    private static bool TryNumber(string value, out int number)
    {
        number = 0;
        return value.Length != 0 && (value.Length == 1 || value[0] != '0') &&
            IsNumeric(value) && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }

    private static bool TryIdentifiers(string value, bool rejectLeadingZeroes, out string[] identifiers)
    {
        identifiers = value.Split('.');
        foreach (string identifier in identifiers)
        {
            if (identifier.Length == 0) return false;
            foreach (char character in identifier)
            {
                if (character is not (>= '0' and <= '9') and not (>= 'a' and <= 'z') and
                    not (>= 'A' and <= 'Z') and not '-')
                    return false;
            }
            if (rejectLeadingZeroes && identifier.Length > 1 && identifier[0] == '0' && IsNumeric(identifier))
                return false;
        }
        return true;
    }

    private static bool IsNumeric(string value)
    {
        if (value.Length == 0) return false;
        foreach (char character in value)
            if (character is < '0' or > '9') return false;
        return true;
    }
}
