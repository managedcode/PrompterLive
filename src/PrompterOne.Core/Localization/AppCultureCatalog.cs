using System.Globalization;

namespace PrompterOne.Core.Localization;

public static class AppCultureCatalog
{
    public const string DefaultCultureName = EnglishCultureName;
    public const string EnglishCultureName = "en";
    public const string UkrainianCultureName = "uk";
    public const string FrenchCultureName = "fr";
    public const string SpanishCultureName = "es";
    public const string GermanCultureName = "de";
    public const string PortugueseCultureName = "pt";
    public const string ItalianCultureName = "it";

    private const char CultureSeparator = '-';
    private const char AlternateCultureSeparator = '_';
    private const string EnglishDisplayName = "English";
    private const string FrenchDisplayName = "Français";
    private const string GermanDisplayName = "Deutsch";
    private const string ItalianDisplayName = "Italiano";
    private const string PortugueseDisplayName = "Português";
    private const string RussianLanguageName = "ru";
    private const string SpanishDisplayName = "Español";
    private const string UkrainianDisplayName = "Українська";

    private static readonly IReadOnlyList<AppCultureDefinition> SupportedCultureDefinitions =
    [
        new(EnglishCultureName, EnglishDisplayName),
        new(UkrainianCultureName, UkrainianDisplayName),
        new(FrenchCultureName, FrenchDisplayName),
        new(SpanishCultureName, SpanishDisplayName),
        new(ItalianCultureName, ItalianDisplayName),
        new(GermanCultureName, GermanDisplayName),
        new(PortugueseCultureName, PortugueseDisplayName)
    ];

    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        EnglishCultureName,
        UkrainianCultureName,
        FrenchCultureName,
        SpanishCultureName,
        GermanCultureName,
        PortugueseCultureName,
        ItalianCultureName
    };

    public static IReadOnlyList<AppCultureDefinition> SupportedCultureDefinitionsInDisplayOrder => SupportedCultureDefinitions;

    public static IReadOnlyCollection<string> SupportedCultureNames => SupportedCultures;

    public static string ResolvePreferredCulture(IEnumerable<string?> requestedCultures)
    {
        foreach (var requestedCulture in requestedCultures)
        {
            var supportedCulture = ResolveSupportedCultureOrNull(requestedCulture);
            if (!string.IsNullOrEmpty(supportedCulture))
            {
                return supportedCulture;
            }
        }

        return DefaultCultureName;
    }

    public static string ResolveSupportedCulture(string? requestedCulture) =>
        ResolveSupportedCultureOrNull(requestedCulture) ?? DefaultCultureName;

    public static CultureInfo CreateCulture(string? requestedCulture) =>
        CultureInfo.GetCultureInfo(ResolveSupportedCulture(requestedCulture));

    private static string? ResolveSupportedCultureOrNull(string? requestedCulture)
    {
        if (string.IsNullOrWhiteSpace(requestedCulture))
        {
            return null;
        }

        var normalized = requestedCulture
            .Trim()
            .Replace(AlternateCultureSeparator, CultureSeparator)
            .ToLowerInvariant();

        var languageName = normalized.Split(CultureSeparator, StringSplitOptions.RemoveEmptyEntries)[0];
        if (string.Equals(languageName, RussianLanguageName, StringComparison.Ordinal))
        {
            return DefaultCultureName;
        }

        return SupportedCultures.Contains(languageName) ? languageName : null;
    }
}
