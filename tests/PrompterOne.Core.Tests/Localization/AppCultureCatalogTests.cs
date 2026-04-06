using PrompterOne.Core.Localization;

namespace PrompterOne.Core.Tests;

public sealed class AppCultureCatalogTests
{
    [Test]
    [Arguments("fr-FR", AppCultureCatalog.FrenchCultureName)]
    [Arguments("uk-UA", AppCultureCatalog.UkrainianCultureName)]
    [Arguments("pt-BR", AppCultureCatalog.PortugueseCultureName)]
    [Arguments("de-DE", AppCultureCatalog.GermanCultureName)]
    [Arguments("ru-RU", AppCultureCatalog.EnglishCultureName)]
    [Arguments("", AppCultureCatalog.EnglishCultureName)]
    public void ResolveSupportedCulture_NormalizesBrowserCultureNames(string requestedCulture, string expectedCulture)
    {
        var actualCulture = AppCultureCatalog.ResolveSupportedCulture(requestedCulture);

        Assert.Equal(expectedCulture, actualCulture);
    }

    [Test]
    public void ResolvePreferredCulture_UsesFirstSupportedCulture_AndBlocksRussian()
    {
        var actualCulture = AppCultureCatalog.ResolvePreferredCulture(["ru-RU", "es-ES", "it-IT"]);

        Assert.Equal(AppCultureCatalog.EnglishCultureName, actualCulture);
    }

    [Test]
    public void SupportedCultureDefinitionsInDisplayOrder_ContainsGerman()
    {
        var german = AppCultureCatalog.SupportedCultureDefinitionsInDisplayOrder
            .Single(culture => string.Equals(culture.CultureName, AppCultureCatalog.GermanCultureName, StringComparison.Ordinal));

        Assert.Equal("Deutsch", german.DisplayName);
    }
}
