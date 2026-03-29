using System.Reflection;
using System.Text;
using PrompterLive.Core.Models.Documents;

namespace PrompterLive.Core.Services.Samples;

public static class SampleScriptCatalog
{
    public const string SeedVersion = "2026-03-29-new-design";
    public const string DemoSampleId = "rsvp-tech-demo";
    public const string ComprehensiveSampleId = "comprehensive-rsvp-demo";

    public static IReadOnlyList<StoredScriptDocument> CreateSeedDocuments()
    {
        return
        [
            BuildDocument(DemoSampleId, "RSVP Technology Demo", "test-script.tps"),
            BuildDocument(ComprehensiveSampleId, "Comprehensive RSVP Demo", "comprehensive-demo.tps")
        ];
    }

    public static StoredScriptDocument GetById(string sampleId) =>
        CreateSeedDocuments().First(document => string.Equals(document.Id, sampleId, StringComparison.Ordinal));

    private static StoredScriptDocument BuildDocument(string id, string title, string resourceFileName)
    {
        return new StoredScriptDocument(
            Id: id,
            Title: title,
            Text: ReadEmbeddedText(resourceFileName),
            DocumentName: resourceFileName,
            UpdatedAt: DateTimeOffset.UtcNow);
    }

    private static string ReadEmbeddedText(string resourceFileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly
            .GetManifestResourceNames()
            .First(name => name.EndsWith(resourceFileName, StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded sample '{resourceFileName}'.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
