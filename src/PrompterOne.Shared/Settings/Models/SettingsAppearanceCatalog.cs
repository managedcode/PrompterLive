using PrompterOne.Shared.Components;
using PrompterOne.Shared.Components.Settings;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Settings.Components;

namespace PrompterOne.Shared.Settings.Models;

public static class SettingsAppearanceCatalog
{
    public static IReadOnlyList<SettingsAppearanceOption> DensityOptions { get; } =
    [
        new(SettingsAppearanceValues.CompactDensity, UiTextKey.SettingsAppearanceDensityCompact, SettingsOptionPreviewKind.DensityCompact),
        new(SettingsAppearanceValues.DefaultDensity, UiTextKey.SettingsAppearanceDensityDefault, SettingsOptionPreviewKind.DensityDefault),
        new(SettingsAppearanceValues.SpaciousDensity, UiTextKey.SettingsAppearanceDensitySpacious, SettingsOptionPreviewKind.DensitySpacious)
    ];

    public static IReadOnlyList<SettingsAppearanceSwatchOption> AccentOptions { get; } =
    [
        new("gold", SettingsAppearanceValues.AccentColors.Gold, UiTextKey.TooltipAccentGold, UiColorSwatchTone.Gold),
        new("blue", SettingsAppearanceValues.AccentColors.Blue, UiTextKey.TooltipAccentBlue, UiColorSwatchTone.Blue),
        new("emerald", SettingsAppearanceValues.AccentColors.Emerald, UiTextKey.TooltipAccentEmerald, UiColorSwatchTone.Emerald),
        new("pink", SettingsAppearanceValues.AccentColors.Pink, UiTextKey.TooltipAccentPink, UiColorSwatchTone.Pink),
        new("purple", SettingsAppearanceValues.AccentColors.Purple, UiTextKey.TooltipAccentPurple, UiColorSwatchTone.Purple),
        new("orange", SettingsAppearanceValues.AccentColors.Orange, UiTextKey.TooltipAccentOrange, UiColorSwatchTone.Orange)
    ];

    public static IReadOnlyList<SettingsAppearanceSwatchOption> TextColorOptions { get; } =
    [
        new("white", SettingsAppearanceValues.TextColors.White, UiTextKey.TooltipTextColorWhite, UiColorSwatchTone.White),
        new("warm-white", SettingsAppearanceValues.TextColors.WarmWhite, UiTextKey.TooltipTextColorWarmWhite, UiColorSwatchTone.WarmWhite),
        new("amber", SettingsAppearanceValues.TextColors.Amber, UiTextKey.TooltipTextColorAmber, UiColorSwatchTone.Amber),
        new("mint", SettingsAppearanceValues.TextColors.Mint, UiTextKey.TooltipTextColorMint, UiColorSwatchTone.Mint)
    ];

    public static IReadOnlyList<SettingsSelectOption> TeleprompterFontOptions { get; } =
    [
        new(SettingsAppearanceValues.TeleprompterFonts.InterDefault, SettingsAppearanceValues.TeleprompterFonts.InterDefault),
        new(SettingsAppearanceValues.TeleprompterFonts.PlayfairDisplay, SettingsAppearanceValues.TeleprompterFonts.PlayfairDisplay),
        new(SettingsAppearanceValues.TeleprompterFonts.JetBrainsMono, SettingsAppearanceValues.TeleprompterFonts.JetBrainsMono),
        new(SettingsAppearanceValues.TeleprompterFonts.Georgia, SettingsAppearanceValues.TeleprompterFonts.Georgia),
        new(SettingsAppearanceValues.TeleprompterFonts.SystemSerif, SettingsAppearanceValues.TeleprompterFonts.SystemSerif)
    ];

    public static IReadOnlyList<SettingsAppearanceOption> ThemeOptions { get; } =
    [
        new(SettingsAppearanceValues.DarkColorScheme, UiTextKey.SettingsAppearanceThemeDark, SettingsOptionPreviewKind.ThemeDark),
        new(SettingsAppearanceValues.LightColorScheme, UiTextKey.SettingsAppearanceThemeLight, SettingsOptionPreviewKind.ThemeLight),
        new(SettingsAppearanceValues.SystemColorScheme, UiTextKey.SettingsAppearanceThemeSystem, SettingsOptionPreviewKind.ThemeSystem)
    ];
}

public sealed record SettingsAppearanceOption(
    string Value,
    UiTextKey LabelKey,
    SettingsOptionPreviewKind PreviewKind);

public sealed record SettingsAppearanceSwatchOption(
    string Id,
    string Value,
    UiTextKey TooltipKey,
    UiColorSwatchTone Tone);
