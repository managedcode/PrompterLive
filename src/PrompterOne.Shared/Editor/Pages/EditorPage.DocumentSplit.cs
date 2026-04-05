using PrompterOne.Core.Models.Documents;
using PrompterOne.Core.Models.Editor;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Components.Editor;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;
using PrompterOne.Shared.Services;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private const int SplitFeedbackPreviewLimit = 3;
    private const string SplitDocumentNameExtension = ".tps";
    private const string SplitDocumentNameSeparator = "-";
    private const string SplitDocumentNameToken = "split";
    private const int MinimumSplitDocumentCount = 2;

    private Task OnSplitRequestedAsync(TpsDocumentSplitMode mode)
    {
        var persistedText = BuildPersistedDocument(_sourceText);
        var splitDocuments = DocumentSplitService.Split(persistedText, mode);
        if (splitDocuments.Count < MinimumSplitDocumentCount)
        {
            Diagnostics.ReportRecoverable(
                SplitDraftOperation,
                SplitDraftNoMatchesMessage,
                ResolveSplitRequirementDetail(mode));
            _splitFeedback = null;
            StateHasChanged();
            return Task.CompletedTask;
        }

        return Diagnostics.RunAsync(
            SplitDraftOperation,
            SplitDraftMessage,
            async () =>
            {
                var sourceDocument = await ResolveCurrentScriptDocumentAsync();
                var baseDocumentName = ResolveBaseDocumentName(sourceDocument);

                foreach (var splitDocument in splitDocuments)
                {
                    _ = await ScriptRepository.SaveAsync(
                        splitDocument.Title,
                        splitDocument.Text,
                        documentName: BuildSplitDocumentName(baseDocumentName, splitDocument),
                        existingId: null,
                        folderId: sourceDocument?.FolderId);
                }

                _splitFeedback = BuildSplitFeedback(splitDocuments, sourceDocument, mode);
                await InvokeAsync(StateHasChanged);
            });
    }

    private void OnOpenLibraryRequested()
    {
        Navigation.NavigateTo(AppRoutes.Library);
    }

    private Task<StoredScriptDocument?> ResolveCurrentScriptDocumentAsync()
    {
        if (string.IsNullOrWhiteSpace(ScriptId))
        {
            return Task.FromResult<StoredScriptDocument?>(null);
        }

        return ScriptRepository.GetAsync(ScriptId);
    }

    private static string ResolveBaseDocumentName(StoredScriptDocument? sourceDocument) =>
        string.IsNullOrWhiteSpace(sourceDocument?.DocumentName)
            ? ScriptWorkspaceState.UntitledScriptDocumentName
            : sourceDocument.DocumentName;

    private static string BuildSplitDocumentName(string baseDocumentName, TpsDocumentSplitDocument splitDocument)
    {
        var baseStem = BrowserStorageSlugifier.Slugify(Path.GetFileNameWithoutExtension(baseDocumentName));
        var childStem = BrowserStorageSlugifier.Slugify(splitDocument.Title);
        return string.Join(
                   SplitDocumentNameSeparator,
                   baseStem,
                   SplitDocumentNameToken,
                   splitDocument.Sequence.ToString("00", System.Globalization.CultureInfo.InvariantCulture),
                   childStem)
               + SplitDocumentNameExtension;
    }

    private EditorSplitFeedbackViewModel BuildSplitFeedback(
        IReadOnlyList<TpsDocumentSplitDocument> splitDocuments,
        StoredScriptDocument? sourceDocument,
        TpsDocumentSplitMode mode)
    {
        var previewTitles = splitDocuments
            .Take(SplitFeedbackPreviewLimit)
            .Select(document => document.Title)
            .ToArray();
        var additionalCount = Math.Max(0, splitDocuments.Count - previewTitles.Length);

        return new EditorSplitFeedbackViewModel(
            Title: SplitFeedbackTitle,
            Summary: BuildSplitSummary(splitDocuments.Count),
            HeadingBadge: BuildSplitHeadingBadge(mode),
            DestinationNote: BuildSplitDestinationNote(sourceDocument),
            DraftNote: Text(UiTextKey.EditorSplitDraftStayedOpen),
            OpenLibraryLabel: Text(UiTextKey.CommonOpenInLibrary),
            CreatedTitles: previewTitles,
            AdditionalCount: additionalCount);
    }

    private string ResolveSplitRequirementDetail(TpsDocumentSplitMode mode) =>
        mode == TpsDocumentSplitMode.TopLevelHeading
            ? Text(UiTextKey.EditorSplitNeedTopLevelHeadings)
            : Text(UiTextKey.EditorSplitNeedSegmentHeadings);

    private string BuildSplitDestinationNote(StoredScriptDocument? sourceDocument)
    {
        if (sourceDocument is null)
        {
            return Text(UiTextKey.EditorSplitSavedToLibrary);
        }

        return string.IsNullOrWhiteSpace(sourceDocument.FolderId)
            ? Text(UiTextKey.EditorSplitSavedToSibling)
            : Text(UiTextKey.EditorSplitSavedToSiblingFolder);
    }

    private string BuildSplitHeadingBadge(TpsDocumentSplitMode mode) =>
        Format(UiTextKey.EditorSplitHeadingBadgeFormat, ResolveHeadingLabel(mode));

    private string BuildSplitSummary(int count) =>
        Format(UiTextKey.EditorSplitSummaryFormat, count);

    private static string ResolveHeadingLabel(TpsDocumentSplitMode mode) =>
        mode == TpsDocumentSplitMode.TopLevelHeading ? "#" : "##";

    private string Format(UiTextKey key, params object[] args) => Localizer[key.ToString(), args];

    private string Text(UiTextKey key) => Localizer[key.ToString()];

    private string SplitFeedbackTitle => Text(UiTextKey.EditorSplitCompleteTitle);
}
