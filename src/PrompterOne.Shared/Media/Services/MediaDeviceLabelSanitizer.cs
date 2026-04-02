using System.Text.RegularExpressions;

namespace PrompterOne.Shared.Services;

internal static partial class MediaDeviceLabelSanitizer
{
    public static string Sanitize(string? rawLabel)
    {
        if (string.IsNullOrWhiteSpace(rawLabel))
        {
            return string.Empty;
        }

        var cleaned = VendorProductCodeSuffixPattern().Replace(rawLabel, string.Empty).Trim();
        return string.IsNullOrWhiteSpace(cleaned)
            ? string.Empty
            : cleaned;
    }

    [GeneratedRegex(@"\s*\([0-9a-fA-F]{4}:[0-9a-fA-F]{4}\)\s*$")]
    private static partial Regex VendorProductCodeSuffixPattern();
}
