namespace PrompterOne.Shared.Services;

internal static class LegacyLibrarySeedCatalog
{
    public const string CleanupVersion = "2026-04-19-cue-matrix-only-v2";

    private static readonly HashSet<string> LegacyDocumentIds = new(StringComparer.Ordinal)
    {
        "rsvp-tech-demo",
        "ted-leadership",
        "security-incident",
        "green-architecture",
        "quantum-computing",
        "comprehensive-demo",
        "starter-product-launch-script",
        "starter-security-incident-script",
        "starter-ted-leadership-script",
        "starter-green-architecture-script",
        "starter-quantum-computing-script"
    };

    private static readonly HashSet<string> LegacyFolderIds = new(StringComparer.Ordinal)
    {
        "presentations",
        "product",
        "investors",
        "podcasts",
        "news",
        "ted",
        "internal",
        "starter-presentations",
        "starter-product",
        "starter-investors",
        "starter-podcasts",
        "starter-news-reports",
        "starter-ted-talks"
    };

    public static bool IsLegacyDocument(BrowserStoredScriptDocumentDto document)
    {
        return LegacyDocumentIds.Contains(document.Id ?? string.Empty);
    }

    public static bool IsLegacyFolder(BrowserStoredLibraryFolderDto folder)
    {
        return LegacyFolderIds.Contains(folder.Id ?? string.Empty);
    }
}
