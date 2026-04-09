using PrompterOne.Core.Models.Workspace;

namespace PrompterOne.Core.Services.Editor;

public static class ScriptDocumentFileTypes
{
    public const string CanonicalImportedExtension = ".tps.md";
    public const string DefaultExtension = ".tps";
    public const string SavePickerDescription = "PrompterOne script";
    public const string TextMimeType = "text/plain";

    public static string AcceptValue => PickerAcceptValue;
    public static string PickerAcceptValue => string.Join(',', PickerSupportedFileNameSuffixes);

    public static IReadOnlyList<string> SaveSupportedFileNameSuffixes { get; } =
    [
        ".tps.md",
        ".md.tps",
        ".tps",
        ".md",
        ".txt"
    ];

    public static IReadOnlyList<string> EditorDropSupportedFileNameSuffixes { get; } = SaveSupportedFileNameSuffixes;

    public static IReadOnlyList<string> PickerSupportedFileNameSuffixes { get; } =
    [
        ".tps.md",
        ".md.tps",
        ".markdown",
        ".docx",
        ".html",
        ".jsonl",
        ".ndjson",
        ".ipynb",
        ".epub",
        ".eml",
        ".htm",
        ".pdf",
        ".csv",
        ".xml",
        ".rst",
        ".adoc",
        ".asciidoc",
        ".org",
        ".tps",
        ".md",
        ".txt"
    ];

    public static string NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        return Path.GetFileName(fileName.Trim());
    }

    public static bool CanImportFromPicker(string? fileName) =>
        ResolvePickerSupportedSuffix(fileName) is not null;

    public static bool CanDropIntoEditor(string? fileName) =>
        ResolveEditorDropSupportedSuffix(fileName) is not null;

    public static bool CanReadAsText(string? fileName) =>
        ResolveEditorDropSupportedSuffix(fileName) is not null;

    public static bool PreservesNativeDocumentName(string? fileName)
    {
        var suffix = ResolveSaveSupportedSuffix(fileName);
        return suffix is ".tps" or ".tps.md" or ".md.tps";
    }

    public static string ResolvePickerTitle(string? fileName) =>
        ResolveTitle(fileName, ResolvePickerSupportedSuffix(fileName));

    public static string ResolveSaveTitle(string? fileName) =>
        ResolveTitle(fileName, ResolveSaveSupportedSuffix(fileName));

    public static string BuildImportedDocumentName(string? fileName)
    {
        var normalizedFileName = NormalizeFileName(fileName);
        if (string.IsNullOrWhiteSpace(normalizedFileName))
        {
            return BuildUntitledImportedDocumentName();
        }

        if (PreservesNativeDocumentName(normalizedFileName))
        {
            return normalizedFileName;
        }

        var sourceSuffix = ResolvePickerSupportedSuffix(normalizedFileName) ?? throw new ArgumentException("Only supported script and document files can be imported.", nameof(fileName));

        var stem = normalizedFileName[..^sourceSuffix.Length].Trim();
        if (string.IsNullOrWhiteSpace(stem))
        {
            return BuildUntitledImportedDocumentName();
        }

        return string.Concat(stem, CanonicalImportedExtension);
    }

    public static string? ResolvePickerSupportedSuffix(string? fileName) =>
        ResolveMatchingSuffix(fileName, PickerSupportedFileNameSuffixes);

    public static string? ResolveEditorDropSupportedSuffix(string? fileName) =>
        ResolveMatchingSuffix(fileName, EditorDropSupportedFileNameSuffixes);

    public static string? ResolveSaveSupportedSuffix(string? fileName) =>
        ResolveMatchingSuffix(fileName, SaveSupportedFileNameSuffixes);

    private static string BuildUntitledImportedDocumentName() =>
        string.Concat(Path.GetFileNameWithoutExtension(ScriptWorkspaceState.UntitledScriptDocumentName), CanonicalImportedExtension);

    private static string ResolveTitle(string? fileName, string? suffix)
    {
        var normalizedFileName = NormalizeFileName(fileName);
        if (string.IsNullOrWhiteSpace(normalizedFileName) || string.IsNullOrWhiteSpace(suffix))
        {
            return ScriptWorkspaceState.UntitledScriptTitle;
        }

        var stem = normalizedFileName[..^suffix.Length].Trim();
        return string.IsNullOrWhiteSpace(stem)
            ? ScriptWorkspaceState.UntitledScriptTitle
            : stem;
    }

    private static string? ResolveMatchingSuffix(string? fileName, IReadOnlyList<string> suffixes)
    {
        var normalizedFileName = NormalizeFileName(fileName);
        if (string.IsNullOrWhiteSpace(normalizedFileName))
        {
            return null;
        }

        return suffixes.FirstOrDefault(
            suffix => normalizedFileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }
}
