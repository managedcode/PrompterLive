using PrompterOne.Shared.Services;

namespace PrompterOne.Web.Tests;

public sealed class LegacyLibrarySeedCatalogTests
{
    [Test]
    public void IsLegacyDocument_DoesNotTreatTestScopedSeedFilesAsRuntimeLegacyContent()
    {
        var document = new BrowserStoredScriptDocumentDto
        {
            Id = "test-security-incident-script",
            DocumentName = "test-security-incident.tps",
            Title = "Security Incident"
        };

        Assert.False(LegacyLibrarySeedCatalog.IsLegacyDocument(document));
    }

    [Test]
    public void IsLegacyDocument_TreatsRemovedRuntimeSeedIdsAsLegacyContent()
    {
        var document = new BrowserStoredScriptDocumentDto
        {
            Id = "security-incident",
            DocumentName = "security-incident.tps",
            Title = "Security Incident"
        };

        Assert.True(LegacyLibrarySeedCatalog.IsLegacyDocument(document));
    }
}
