using System.Reflection;
using System.Text;
using PrompterLive.Core.Models.Documents;

namespace PrompterLive.Core.Services.Samples;

public static class SampleScriptCatalog
{
    public const string SeedVersion = "2026-03-29-new-design-v4";
    public const string DemoSampleId = "rsvp-tech-demo";
    public const string LeadershipSampleId = "ted-leadership";
    public const string SecuritySampleId = "security-incident";
    public const string ArchitectureSampleId = "green-architecture";
    public const string QuantumSampleId = "quantum-computing";

    public static IReadOnlyList<StoredScriptDocument> CreateSeedDocuments()
    {
        return
        [
            BuildDocument(DemoSampleId, "Product Launch", "test-script.tps", new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero)),
            BuildDocument(LeadershipSampleId, "TED: Leadership", "ted-leadership.tps", new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero)),
            BuildDocument(SecuritySampleId, "Security Incident", "security-incident.tps", new DateTimeOffset(2026, 3, 24, 8, 30, 0, TimeSpan.Zero)),
            BuildDocument(ArchitectureSampleId, "Green Architecture", "green-architecture.tps", new DateTimeOffset(2026, 3, 18, 10, 15, 0, TimeSpan.Zero)),
            BuildDocument(QuantumSampleId, "Quantum Computing", "quantum-computing.tps", new DateTimeOffset(2026, 3, 15, 16, 45, 0, TimeSpan.Zero))
        ];
    }

    public static StoredScriptDocument GetById(string sampleId) =>
        CreateSeedDocuments().First(document => string.Equals(document.Id, sampleId, StringComparison.Ordinal));

    private static StoredScriptDocument BuildDocument(string id, string title, string resourceFileName, DateTimeOffset updatedAt)
    {
        return new StoredScriptDocument(
            Id: id,
            Title: title,
            Text: ReadEmbeddedText(resourceFileName),
            DocumentName: resourceFileName,
            UpdatedAt: updatedAt);
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
