using PrompterOne.Core.Models.Workspace;
using PrompterOne.Core.Services.Editor;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private bool TryImportFrontMatterFromSource(string? text, out string bodyText)
    {
        var document = _frontMatterService.Parse(text);
        bodyText = document.Body;
        if (document.BodyStartIndex == 0)
        {
            return false;
        }

        ApplyImportedMetadata(document.Metadata);
        return true;
    }

    private void ApplyLoadedMetadata(IReadOnlyDictionary<string, string> metadata, ScriptWorkspaceState state)
    {
        var computedDuration = FormatDuration(state.EstimatedDuration);
        ApplyResolvedMetadata(
            _frontMatterService.ResolveTitle(state.Text, state.Title),
            GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Author, DefaultAuthor),
            TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.BaseWpm, state.ScriptData?.TargetWpm ?? 140),
            NormalizeProfile(GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Profile, _baseWpm >= 250 ? DefaultProfileRsvp : DefaultProfileActor)),
            GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Version, DefaultVersion),
            GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Created, _createdDate),
            GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.DisplayDuration, computedDuration),
            TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.XslowOffset, DefaultXslowOffset),
            TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.SlowOffset, DefaultSlowOffset),
            TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.FastOffset, DefaultFastOffset),
            TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.XfastOffset, DefaultXfastOffset));
    }

    private void ApplyImportedMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        ApplyResolvedMetadata(
            GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Title, _screenTitle),
            GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Author, _author),
            TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.BaseWpm, _baseWpm),
            NormalizeProfile(GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Profile, _profile)),
            GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Version, _version),
            GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.Created, _createdDate),
            GetMetadata(metadata, TpsFrontMatterDocumentService.MetadataKeys.DisplayDuration, _displayDuration),
            TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.XslowOffset, _xslowOffset),
            TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.SlowOffset, _slowOffset),
            TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.FastOffset, _fastOffset),
            TryGetInt(metadata, TpsFrontMatterDocumentService.MetadataKeys.XfastOffset, _xfastOffset));
    }

    private void ApplyResolvedMetadata(
        string title,
        string author,
        int baseWpm,
        string profile,
        string version,
        string createdDate,
        string displayDuration,
        int xslowOffset,
        int slowOffset,
        int fastOffset,
        int xfastOffset)
    {
        _screenTitle = title;
        _author = author;
        _baseWpm = baseWpm;
        _profile = profile;
        _version = version;
        _createdDate = createdDate;
        _displayDuration = displayDuration;
        _xslowOffset = xslowOffset;
        _slowOffset = slowOffset;
        _fastOffset = fastOffset;
        _xfastOffset = xfastOffset;
        UpdateStatus();
        Shell.ShowEditor(_screenTitle, SessionService.State.ScriptId);
    }

    private static string NormalizeProfile(string profile) =>
        string.Equals(profile, DefaultProfileRsvp, StringComparison.OrdinalIgnoreCase)
            ? DefaultProfileRsvp
            : DefaultProfileActor;
}
