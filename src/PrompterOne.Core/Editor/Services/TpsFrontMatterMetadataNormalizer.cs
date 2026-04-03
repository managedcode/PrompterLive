using System.Globalization;

namespace PrompterOne.Core.Services.Editor;

internal static class TpsFrontMatterMetadataNormalizer
{
    private const string DurationAliasKey = "duration";
    private const string PresetFastKey = "presets.fast";
    private const string PresetSlowKey = "presets.slow";
    private const string PresetXfastKey = "presets.xfast";
    private const string PresetXslowKey = "presets.xslow";
    private const string SpecFastKey = "speed_offsets.fast";
    private const string SpecSlowKey = "speed_offsets.slow";
    private const string SpecXfastKey = "speed_offsets.xfast";
    private const string SpecXslowKey = "speed_offsets.xslow";

    public static Dictionary<string, string> Normalize(IReadOnlyDictionary<string, string> metadata)
    {
        var normalized = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        ApplyAlias(metadata, normalized, DurationAliasKey, TpsFrontMatterDocumentService.MetadataKeys.DisplayDuration);

        ApplySpeedAlias(metadata, normalized, TpsFrontMatterDocumentService.MetadataKeys.XslowOffset, SpecXslowKey, PresetXslowKey);
        ApplySpeedAlias(metadata, normalized, TpsFrontMatterDocumentService.MetadataKeys.SlowOffset, SpecSlowKey, PresetSlowKey);
        ApplySpeedAlias(metadata, normalized, TpsFrontMatterDocumentService.MetadataKeys.FastOffset, SpecFastKey, PresetFastKey);
        ApplySpeedAlias(metadata, normalized, TpsFrontMatterDocumentService.MetadataKeys.XfastOffset, SpecXfastKey, PresetXfastKey);

        return normalized;
    }

    private static void ApplyAlias(
        IReadOnlyDictionary<string, string> metadata,
        IDictionary<string, string> normalized,
        string aliasKey,
        string targetKey)
    {
        if (normalized.ContainsKey(targetKey) || !metadata.TryGetValue(aliasKey, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        normalized[targetKey] = value.Trim();
    }

    private static void ApplySpeedAlias(
        IReadOnlyDictionary<string, string> metadata,
        IDictionary<string, string> normalized,
        string targetKey,
        string specKey,
        string presetKey)
    {
        if (normalized.ContainsKey(targetKey))
        {
            return;
        }

        if (metadata.TryGetValue(specKey, out var specValue) && !string.IsNullOrWhiteSpace(specValue))
        {
            normalized[targetKey] = specValue.Trim();
            return;
        }

        if (!metadata.TryGetValue(presetKey, out var presetValue) ||
            !int.TryParse(presetValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var presetWpm) ||
            !metadata.TryGetValue(TpsFrontMatterDocumentService.MetadataKeys.BaseWpm, out var baseWpmValue) ||
            !int.TryParse(baseWpmValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var baseWpm) ||
            baseWpm <= 0)
        {
            return;
        }

        var offset = (int)Math.Round(((presetWpm - baseWpm) * 100d) / baseWpm, MidpointRounding.AwayFromZero);
        normalized[targetKey] = offset.ToString(CultureInfo.InvariantCulture);
    }
}
