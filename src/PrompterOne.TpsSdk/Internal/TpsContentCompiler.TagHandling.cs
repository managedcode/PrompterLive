using System.Text;
using ManagedCode.Tps.Models;

namespace ManagedCode.Tps.Internal;

internal sealed partial class TpsContentCompiler
{
    private static bool HandleTagToken(
        TagToken tag,
        List<string> literalScopes,
        List<InlineScope> scopes,
        List<WordSeed> words,
        List<PhraseSeed> phrases,
        List<WordSeed> currentPhrase,
        InheritedFormattingState inherited,
        int absoluteOffset,
        IReadOnlyList<int> lineStarts,
        List<TpsDiagnostic> diagnostics)
    {
        if (tag.IsClosing)
        {
            return HandleClosingTag(tag, literalScopes, scopes, absoluteOffset, lineStarts, diagnostics);
        }

        if (string.Equals(tag.Name, TpsSpec.Tags.Pause, StringComparison.Ordinal))
        {
            var pauseDuration = TpsSupport.TryResolvePauseMilliseconds(tag.Argument);
            if (pauseDuration is null)
            {
                diagnostics.Add(TpsSupport.CreateDiagnostic(
                    TpsSpec.DiagnosticCodes.InvalidPause,
                    "Pause duration must use Ns or Nms syntax.",
                    absoluteOffset,
                    absoluteOffset + tag.Raw.Length,
                    lineStarts));
                return false;
            }

            FlushPhrase(phrases, currentPhrase);
            words.Add(CreateControlWord(TpsSpec.WordKinds.Pause, inherited, pauseDuration));
            return true;
        }

        if (string.Equals(tag.Name, TpsSpec.Tags.Breath, StringComparison.Ordinal))
        {
            FlushPhrase(phrases, currentPhrase);
            words.Add(CreateControlWord(TpsSpec.WordKinds.Breath, inherited));
            return true;
        }

        if (string.Equals(tag.Name, TpsSpec.Tags.EditPoint, StringComparison.Ordinal))
        {
            if (tag.Argument is not null && !TpsSpec.EditPointPriorities.Contains(tag.Argument, StringComparer.OrdinalIgnoreCase))
            {
                diagnostics.Add(TpsSupport.CreateDiagnostic(
                    TpsSpec.DiagnosticCodes.InvalidTagArgument,
                    $"Edit point priority '{tag.Argument}' is not supported.",
                    absoluteOffset,
                    absoluteOffset + tag.Raw.Length,
                    lineStarts));
                return false;
            }

            FlushPhrase(phrases, currentPhrase);
            words.Add(CreateControlWord(TpsSpec.WordKinds.EditPoint, inherited, null, tag.Argument));
            return true;
        }

        var scope = CreateScope(tag, inherited.SpeedOffsets, absoluteOffset, lineStarts, diagnostics);
        if (scope is null)
        {
            literalScopes.Add(tag.Name);
            return false;
        }

        scopes.Add(scope);
        return true;
    }

    private static bool HandleClosingTag(
        TagToken tag,
        List<string> literalScopes,
        List<InlineScope> scopes,
        int absoluteOffset,
        IReadOnlyList<int> lineStarts,
        List<TpsDiagnostic> diagnostics)
    {
        var literalIndex = literalScopes.FindLastIndex(name => string.Equals(name, tag.Name, StringComparison.Ordinal));
        if (literalIndex >= 0)
        {
            literalScopes.RemoveAt(literalIndex);
            return false;
        }

        var scopeIndex = scopes.FindLastIndex(scope => string.Equals(scope.Name, tag.Name, StringComparison.Ordinal));
        if (scopeIndex < 0)
        {
            diagnostics.Add(TpsSupport.CreateDiagnostic(
                TpsSpec.DiagnosticCodes.MismatchedClosingTag,
                $"Closing tag '{tag.Name}' does not match any open scope.",
                absoluteOffset,
                absoluteOffset + tag.Raw.Length,
                lineStarts));
            return false;
        }

        scopes.RemoveAt(scopeIndex);
        return true;
    }

    private static InlineScope? CreateScope(
        TagToken tag,
        IReadOnlyDictionary<string, int> speedOffsets,
        int absoluteOffset,
        IReadOnlyList<int> lineStarts,
        List<TpsDiagnostic> diagnostics)
    {
        if (tag.Name is TpsSpec.Tags.Phonetic or TpsSpec.Tags.Pronunciation)
        {
            if (tag.Argument is null)
            {
                diagnostics.Add(TpsSupport.CreateDiagnostic(
                    TpsSpec.DiagnosticCodes.InvalidTagArgument,
                    $"Tag '{tag.Name}' requires a pronunciation parameter.",
                    absoluteOffset,
                    absoluteOffset + tag.Raw.Length,
                    lineStarts));
                return null;
            }

            return new InlineScope(
                tag.Name,
                PhoneticGuide: string.Equals(tag.Name, TpsSpec.Tags.Phonetic, StringComparison.Ordinal) ? tag.Argument : null,
                PronunciationGuide: string.Equals(tag.Name, TpsSpec.Tags.Pronunciation, StringComparison.Ordinal) ? tag.Argument : null);
        }

        if (string.Equals(tag.Name, TpsSpec.Tags.Stress, StringComparison.Ordinal))
        {
            return new InlineScope(tag.Name, StressGuide: tag.Argument, StressWrap: tag.Argument is null);
        }

        if (string.Equals(tag.Name, TpsSpec.Tags.Emphasis, StringComparison.Ordinal))
        {
            return new InlineScope(tag.Name, EmphasisLevel: 1, EmphasisStyle: TpsSpec.EmphasisStyles.Emphasis);
        }

        if (string.Equals(tag.Name, TpsSpec.Tags.Highlight, StringComparison.Ordinal))
        {
            return new InlineScope(tag.Name, Highlight: true);
        }

        if (TpsSpec.VolumeLevels.Contains(tag.Name, StringComparer.OrdinalIgnoreCase))
        {
            return new InlineScope(tag.Name, VolumeLevel: tag.Name);
        }

        if (TpsSpec.DeliveryModes.Contains(tag.Name, StringComparer.OrdinalIgnoreCase))
        {
            return new InlineScope(tag.Name, DeliveryMode: tag.Name);
        }

        if (TpsSpec.ArticulationStyles.Contains(tag.Name, StringComparer.OrdinalIgnoreCase))
        {
            return new InlineScope(tag.Name, ArticulationStyle: tag.Name);
        }

        if (string.Equals(tag.Name, TpsSpec.Tags.Energy, StringComparison.Ordinal))
        {
            if (tag.Argument is null || !int.TryParse(tag.Argument, out var energyLevel) || energyLevel < TpsSpec.EnergyLevelMin || energyLevel > TpsSpec.EnergyLevelMax)
            {
                diagnostics.Add(TpsSupport.CreateDiagnostic(
                    TpsSpec.DiagnosticCodes.InvalidEnergyLevel,
                    $"Energy level must be an integer between {TpsSpec.EnergyLevelMin} and {TpsSpec.EnergyLevelMax}.",
                    absoluteOffset,
                    absoluteOffset + tag.Raw.Length,
                    lineStarts));
                return null;
            }

            return new InlineScope(tag.Name, EnergyLevel: energyLevel);
        }

        if (string.Equals(tag.Name, TpsSpec.Tags.Melody, StringComparison.Ordinal))
        {
            if (tag.Argument is null || !int.TryParse(tag.Argument, out var melodyLevel) || melodyLevel < TpsSpec.MelodyLevelMin || melodyLevel > TpsSpec.MelodyLevelMax)
            {
                diagnostics.Add(TpsSupport.CreateDiagnostic(
                    TpsSpec.DiagnosticCodes.InvalidMelodyLevel,
                    $"Melody level must be an integer between {TpsSpec.MelodyLevelMin} and {TpsSpec.MelodyLevelMax}.",
                    absoluteOffset,
                    absoluteOffset + tag.Raw.Length,
                    lineStarts));
                return null;
            }

            return new InlineScope(tag.Name, MelodyLevel: melodyLevel);
        }

        if (TpsSpec.Emotions.Contains(tag.Name, StringComparer.OrdinalIgnoreCase))
        {
            return new InlineScope(tag.Name, InlineEmotion: tag.Name);
        }

        if (TpsSupport.TryParseAbsoluteWpm(tag.Name) is int absoluteSpeed)
        {
            return new InlineScope(tag.Name, AbsoluteSpeed: absoluteSpeed);
        }

        if (TpsSupport.ResolveSpeedMultiplier(tag.Name, speedOffsets) is double multiplier)
        {
            return new InlineScope(tag.Name, RelativeSpeedMultiplier: multiplier);
        }

        if (string.Equals(tag.Name, TpsSpec.Tags.Normal, StringComparison.Ordinal))
        {
            return new InlineScope(tag.Name, ResetSpeed: true);
        }

        diagnostics.Add(TpsSupport.CreateDiagnostic(
            TpsSpec.DiagnosticCodes.UnknownTag,
            $"Tag '{tag.Name}' is not part of the TPS specification.",
            absoluteOffset,
            absoluteOffset + tag.Raw.Length,
            lineStarts));
        return null;
    }

    private static bool TryHandleMarkdownMarker(string text, ref int index, List<InlineScope> scopes)
    {
        if (text[index] != '*')
        {
            return false;
        }

        var markerLength = index + 1 < text.Length && text[index + 1] == '*' ? 2 : 1;
        var marker = new string('*', markerLength);
        var scopeName = markerLength == 2
            ? TpsSpec.Markers.MarkdownStrongScope
            : TpsSpec.Markers.MarkdownItalicScope;
        var emphasisStyle = markerLength == 2
            ? TpsSpec.EmphasisStyles.MarkdownStrong
            : TpsSpec.EmphasisStyles.MarkdownItalic;
        var existingIndex = scopes.FindLastIndex(scope => string.Equals(scope.Name, scopeName, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            scopes.RemoveAt(existingIndex);
            index += markerLength - 1;
            return true;
        }

        if (text.IndexOf(marker, index + markerLength, StringComparison.Ordinal) < 0)
        {
            return false;
        }

        scopes.Add(new InlineScope(scopeName, EmphasisLevel: markerLength == 2 ? 2 : 1, EmphasisStyle: emphasisStyle));
        index += markerLength - 1;
        return true;
    }

    private static TagToken? ReadTagToken(string text, int index)
    {
        var endIndex = text.IndexOf(']', index + 1);
        if (endIndex < 0)
        {
            return null;
        }

        var raw = text[index..(endIndex + 1)];
        var inner = TpsEscaping.Restore(raw[1..^1]).Trim();
        var isClosing = inner.StartsWith(TpsSpec.Markers.ClosingTagPrefix, StringComparison.Ordinal);
        var body = isClosing ? inner[TpsSpec.Markers.ClosingTagPrefix.Length..].Trim() : inner;
        var separatorIndex = body.IndexOf(TpsSpec.Markers.TagArgumentSeparator);
        var name = (separatorIndex >= 0 ? body[..separatorIndex] : body).Trim().ToLowerInvariant();
        var argument = separatorIndex >= 0 ? TpsSupport.NormalizeValue(body[(separatorIndex + 1)..]) : null;
        return new TagToken(raw, name, argument, isClosing);
    }

    private static bool RequiresTokenBoundary(string tagName) =>
        tagName is TpsSpec.Tags.Pause or TpsSpec.Tags.Breath or TpsSpec.Tags.EditPoint;

    private static bool TryHandleSlashPause(string text, int index, StringBuilder builder, TokenAccumulator? token)
    {
        var nextCharacter = index + 1 < text.Length ? text[index + 1] : '\0';
        var previousCharacter = index > 0 ? text[index - 1] : '\0';
        var nextIndex = nextCharacter == TpsSpec.Markers.ShortPauseMarker ? index + 2 : index + 1;
        var previousIsBoundary = index == 0 || char.IsWhiteSpace(previousCharacter);
        var nextIsBoundary = nextIndex >= text.Length || char.IsWhiteSpace(text[nextIndex]);
        return text[index] == TpsSpec.Markers.ShortPauseMarker && builder.Length == 0 && token is null && previousIsBoundary && nextIsBoundary;
    }
}
