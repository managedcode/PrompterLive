using PrompterLive.Core.Models.Editor;
using PrompterLive.Shared.Components.Editor;

namespace PrompterLive.Shared.Pages;

public partial class EditorPage
{
    private const int DefaultFastOffset = 25;
    private const int DefaultSlowOffset = -20;
    private const int DefaultXfastOffset = 50;
    private const int DefaultXslowOffset = -40;

    private EditorStructureHeaderEditorViewModel _activeBlockEditor = EditorStructureHeaderEditorViewModel.Empty("Block", false);
    private EditorStructureHeaderEditorViewModel _activeSegmentEditor = EditorStructureHeaderEditorViewModel.Empty("Segment", true);
    private int _fastOffset = DefaultFastOffset;
    private int _slowOffset = DefaultSlowOffset;
    private int _xfastOffset = DefaultXfastOffset;
    private int _xslowOffset = DefaultXslowOffset;

    private Task OnFastOffsetChangedAsync(int value) => UpdateSpeedOffsetAsync(value, SpeedOffsetKind.Fast);

    private Task OnSlowOffsetChangedAsync(int value) => UpdateSpeedOffsetAsync(value, SpeedOffsetKind.Slow);

    private Task OnXfastOffsetChangedAsync(int value) => UpdateSpeedOffsetAsync(value, SpeedOffsetKind.Xfast);

    private Task OnXslowOffsetChangedAsync(int value) => UpdateSpeedOffsetAsync(value, SpeedOffsetKind.Xslow);

    private void RefreshStructureAuthoringState()
    {
        _activeSegmentEditor = BuildStructureEditor(_segments.ElementAtOrDefault(_activeSegmentIndex), true);
        var activeSegment = _segments.ElementAtOrDefault(_activeSegmentIndex);
        var activeBlock = activeSegment?.Blocks.ElementAtOrDefault(_activeBlockIndex ?? 0);
        _activeBlockEditor = BuildStructureEditor(activeBlock, false);
    }

    private EditorStructureHeaderEditorViewModel BuildStructureEditor(EditorOutlineSegmentViewModel? segment, bool isSegment)
    {
        if (segment is null)
        {
            return EditorStructureHeaderEditorViewModel.Empty("Segment", true);
        }

        if (StructureEditor.TryRead(_sourceText, segment.StartIndex, out var snapshot))
        {
            return MapSnapshot(snapshot, "Segment");
        }

        return new EditorStructureHeaderEditorViewModel(
            "Segment",
            segment.StartIndex,
            segment.Name,
            segment.TargetWpm,
            EditorEmotionCatalog.GetLabel(segment.EmotionKey),
            segment.DurationLabel,
            true);
    }

    private EditorStructureHeaderEditorViewModel BuildStructureEditor(EditorOutlineBlockViewModel? block, bool isSegment)
    {
        if (block is null)
        {
            return EditorStructureHeaderEditorViewModel.Empty("Block", false);
        }

        if (StructureEditor.TryRead(_sourceText, block.StartIndex, out var snapshot))
        {
            return MapSnapshot(snapshot, "Block");
        }

        return new EditorStructureHeaderEditorViewModel(
            "Block",
            block.StartIndex,
            block.Name,
            block.TargetWpm,
            block.EmotionLabel,
            string.Empty,
            isSegment);
    }

    private static EditorStructureHeaderEditorViewModel MapSnapshot(TpsStructureHeaderSnapshot snapshot, string label) =>
        new(
            label,
            snapshot.LineStartIndex,
            snapshot.Name,
            snapshot.TargetWpm,
            EditorEmotionCatalog.GetLabel(snapshot.EmotionKey),
            snapshot.Timing,
            snapshot.SupportsTiming);

    private async Task UpdateSpeedOffsetAsync(int value, SpeedOffsetKind kind)
    {
        switch (kind)
        {
            case SpeedOffsetKind.Xslow:
                _xslowOffset = value;
                break;
            case SpeedOffsetKind.Slow:
                _slowOffset = value;
                break;
            case SpeedOffsetKind.Fast:
                _fastOffset = value;
                break;
            case SpeedOffsetKind.Xfast:
                _xfastOffset = value;
                break;
        }

        await PersistMetadataAsync();
    }

    private enum SpeedOffsetKind
    {
        Xslow,
        Slow,
        Fast,
        Xfast
    }
}
