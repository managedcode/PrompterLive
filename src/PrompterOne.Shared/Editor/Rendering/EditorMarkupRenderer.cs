using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using PrompterOne.Shared.Contracts;

namespace PrompterOne.Shared.Rendering;

public static class EditorMarkupRenderer
{
    private static readonly Regex TagRegex = new(@"\[[^\[\]]+\]", RegexOptions.Compiled);
    private static readonly Regex NumericWpmRegex = new(@"^(?<wpm>\d+)\s*WPM$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, string> EmotionClasses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["warm"] = "mk-emo-warm",
        ["concerned"] = "mk-emo-concerned",
        ["focused"] = "mk-emo-focused",
        ["motivational"] = "mk-emo-motivational",
        ["neutral"] = "mk-emo-neutral",
        ["urgent"] = "mk-emo-urgent",
        ["happy"] = "mk-emo-happy",
        ["excited"] = "mk-emo-excited",
        ["sad"] = "mk-emo-sad",
        ["calm"] = "mk-emo-calm",
        ["energetic"] = "mk-emo-energetic",
        ["professional"] = "mk-emo-professional"
    };

    private static readonly IReadOnlyDictionary<string, string> VolumeClasses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["loud"] = "mk-vol-loud",
        ["soft"] = "mk-vol-soft",
        ["whisper"] = "mk-vol-whisper"
    };

    private static readonly IReadOnlyDictionary<string, string> DeliveryClasses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["aside"] = "mk-del-aside",
        ["rhetorical"] = "mk-del-rhetorical",
        ["sarcasm"] = "mk-del-sarcasm",
        ["building"] = "mk-del-building"
    };

    private static readonly IReadOnlyDictionary<string, string?> SpeedClasses = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
    {
        ["xslow"] = "mk-xslow",
        ["slow"] = "mk-slow",
        ["normal"] = null,
        ["fast"] = "mk-fast",
        ["xfast"] = "mk-xfast"
    };

    public static MarkupString Render(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (MarkupString)"<span class=\"mk-muted\">Add script content here.</span>";
        }

        var renderer = new Renderer();
        return (MarkupString)renderer.Render(content.Replace("\r\n", "\n", StringComparison.Ordinal).Trim());
    }

    private sealed class Renderer
    {
        private readonly StringBuilder _builder = new();
        private readonly Stack<ScopeFrame> _scopes = new();

        public Renderer()
        {
            _scopes.Push(new ScopeFrame("root", ScopeKind.Root, RenderState.Default));
        }

        public string Render(string content)
        {
            var index = 0;
            foreach (Match match in TagRegex.Matches(content))
            {
                if (match.Index > index)
                {
                    AppendText(content.Substring(index, match.Index - index));
                }

                HandleTag(match.Value);
                index = match.Index + match.Length;
            }

            if (index < content.Length)
            {
                AppendText(content[index..]);
            }

            return _builder.ToString();
        }

        private void AppendText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (_scopes.Peek().Kind == ScopeKind.Pronunciation)
            {
                _scopes.Peek().Buffer.Append(text);
                return;
            }

            var chunkStart = 0;
            for (var index = 0; index < text.Length; index++)
            {
                var pauseLength = GetPauseLength(text, index);
                if (pauseLength == 0)
                {
                    continue;
                }

                AppendStyledText(text.Substring(chunkStart, index - chunkStart));
                _builder.Append("<span class=\"mk-pause\">")
                    .Append(WebUtility.HtmlEncode(text.Substring(index, pauseLength)))
                    .Append("</span>");

                index += pauseLength - 1;
                chunkStart = index + 1;
            }

            if (chunkStart < text.Length)
            {
                AppendStyledText(text[chunkStart..]);
            }
        }

        private void AppendStyledText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var encoded = WebUtility.HtmlEncode(text)
                .Replace("\n", "<br>", StringComparison.Ordinal);

            if (!CurrentState.RequiresSpan())
            {
                _builder.Append(encoded);
                return;
            }

            AppendStyledSpan(encoded, CurrentState);
        }

        private void HandleTag(string rawTag)
        {
            var inner = rawTag[1..^1].Trim();
            if (string.IsNullOrWhiteSpace(inner))
            {
                return;
            }

            if (inner[0] == '/')
            {
                CloseTag(rawTag, inner[1..].Trim());
                return;
            }

            var separatorIndex = inner.IndexOf(':');
            var name = separatorIndex >= 0 ? inner[..separatorIndex].Trim() : inner;
            var argument = separatorIndex >= 0 ? inner[(separatorIndex + 1)..].Trim() : null;

            if (NumericWpmRegex.Match(name) is { Success: true } wpmMatch)
            {
                AppendTag(rawTag);
                _builder.Append("<span class=\"mk-wpm-badge\">")
                    .Append(WebUtility.HtmlEncode(wpmMatch.Groups["wpm"].Value))
                    .Append("WPM</span> ");
                PushScope(name, ScopeKind.Wpm, CurrentState);
                return;
            }

            if (string.Equals(name, "pause", StringComparison.OrdinalIgnoreCase))
            {
                AppendStandalone("mk-special", rawTag);
                return;
            }

            if (string.Equals(name, "breath", StringComparison.OrdinalIgnoreCase))
            {
                AppendStandalone("mk-breath", rawTag);
                return;
            }

            if (string.Equals(name, "edit_point", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "editpoint", StringComparison.OrdinalIgnoreCase))
            {
                AppendStandalone("mk-edit", rawTag);
                return;
            }

            if (string.Equals(name, "phonetic", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "pronunciation", StringComparison.OrdinalIgnoreCase))
            {
                AppendTag(rawTag);
                PushScope(name, ScopeKind.Pronunciation, CurrentState, argument);
                return;
            }

            if (string.Equals(name, "highlight", StringComparison.OrdinalIgnoreCase))
            {
                AppendTag(rawTag);
                PushScope(name, ScopeKind.Style, CurrentState with { IsHighlighted = true });
                return;
            }

            if (string.Equals(name, "emphasis", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "strong", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "bold", StringComparison.OrdinalIgnoreCase))
            {
                AppendTag(rawTag);
                PushScope(name, ScopeKind.Style, CurrentState with { IsEmphasis = true });
                return;
            }

            if (string.Equals(name, "stress", StringComparison.OrdinalIgnoreCase))
            {
                AppendTag(rawTag);
                PushScope(name, ScopeKind.Style, CurrentState with { IsStress = true });
                return;
            }

            if (SpeedClasses.TryGetValue(name, out var speedClass))
            {
                AppendTag(rawTag);
                PushScope(
                    name,
                    ScopeKind.Style,
                    CurrentState with
                    {
                        SpeedClass = speedClass,
                        SpeedValue = speedClass is null ? null : name.ToLowerInvariant()
                    });
                return;
            }

            if (VolumeClasses.TryGetValue(name, out var volumeClass))
            {
                AppendTag(rawTag);
                PushScope(
                    name,
                    ScopeKind.Style,
                    CurrentState with
                    {
                        VolumeClass = volumeClass,
                        VolumeValue = name.ToLowerInvariant()
                    });
                return;
            }

            if (DeliveryClasses.TryGetValue(name, out var deliveryClass))
            {
                AppendTag(rawTag);
                PushScope(
                    name,
                    ScopeKind.Style,
                    CurrentState with
                    {
                        DeliveryClass = deliveryClass,
                        DeliveryValue = name.ToLowerInvariant()
                    });
                return;
            }

            if (EmotionClasses.TryGetValue(name, out var emotionClass))
            {
                AppendTag(rawTag);
                PushScope(name, ScopeKind.Style, CurrentState with { EmotionClass = emotionClass });
                return;
            }

            AppendTag(rawTag);
            PushScope(name, ScopeKind.Neutral, CurrentState);
        }

        private void CloseTag(string rawTag, string closingName)
        {
            if (_scopes.Count <= 1)
            {
                AppendTag(rawTag);
                return;
            }

            var matched = PopScope(closingName);
            if (matched?.Kind == ScopeKind.Pronunciation)
            {
                AppendPronunciationPayload(matched, CurrentState);
            }

            AppendTag(rawTag);
        }

        private ScopeFrame? PopScope(string closingName)
        {
            if (_scopes.Count <= 1)
            {
                return null;
            }

            var popped = new List<ScopeFrame>();
            ScopeFrame? matched = null;

            while (_scopes.Count > 1)
            {
                var current = _scopes.Pop();
                popped.Add(current);
                if (string.Equals(current.Name, closingName, StringComparison.OrdinalIgnoreCase))
                {
                    matched = current;
                    break;
                }
            }

            if (matched is null)
            {
                for (var index = popped.Count - 1; index >= 0; index--)
                {
                    _scopes.Push(popped[index]);
                }
            }

            return matched;
        }

        private void AppendPronunciationPayload(ScopeFrame scope, RenderState parentState)
        {
            var guide = string.IsNullOrWhiteSpace(scope.Argument) ? string.Empty : WebUtility.HtmlEncode(scope.Argument);
            var spoken = scope.Buffer.Length == 0
                ? string.Empty
                : WebUtility.HtmlEncode(NormalizePronunciationText(scope.Buffer.ToString()));

            _builder.Append("<span class=\"mk-phonetic\">")
                .Append(guide)
                .Append("</span> ");

            AppendStyledSpan(spoken, parentState, "mk-phonetic-word");
        }

        private void AppendStyledSpan(string encodedText, RenderState state, params string[] extraClasses)
        {
            if (!state.RequiresSpan(extraClasses))
            {
                _builder.Append(encodedText);
                return;
            }

            _builder.Append("<span");
            state.AppendHtmlAttributes(_builder, extraClasses);
            _builder.Append('>')
                .Append(encodedText)
                .Append("</span>");
        }

        private void AppendTag(string value)
        {
            _builder.Append("<span class=\"mk-tag\">")
                .Append(WebUtility.HtmlEncode(value))
                .Append("</span>");
        }

        private void AppendStandalone(string cssClass, string value)
        {
            _builder.Append("<span class=\"")
                .Append(cssClass)
                .Append("\">")
                .Append(WebUtility.HtmlEncode(value))
                .Append("</span>");
        }

        private void PushScope(string name, ScopeKind kind, RenderState state, string? argument = null)
        {
            _scopes.Push(new ScopeFrame(name, kind, state, argument));
        }

        private RenderState CurrentState => _scopes.Peek().State;

        private static int GetPauseLength(string text, int index)
        {
            if (text[index] != '/')
            {
                return 0;
            }

            var length = index + 1 < text.Length && text[index + 1] == '/' ? 2 : 1;
            var previous = index == 0 ? '\0' : text[index - 1];
            var nextIndex = index + length;
            var next = nextIndex >= text.Length ? '\0' : text[nextIndex];

            if ((previous != '\0' && !char.IsWhiteSpace(previous)) ||
                (next != '\0' && !char.IsWhiteSpace(next)))
            {
                return 0;
            }

            return length;
        }

        private static string NormalizePronunciationText(string value) =>
            Regex.Replace(value.Trim(), @"\s+", " ");
    }

    private sealed class ScopeFrame(string name, EditorMarkupRenderer.ScopeKind kind, EditorMarkupRenderer.RenderState state, string? argument = null)
    {
        public string Name { get; } = name;

        public ScopeKind Kind { get; } = kind;

        public RenderState State { get; } = state;

        public string? Argument { get; } = argument;

        public StringBuilder Buffer { get; } = new();
    }

    private sealed record RenderState(
        string? EmotionClass,
        string? VolumeValue,
        string? VolumeClass,
        string? DeliveryValue,
        string? DeliveryClass,
        string? SpeedValue,
        string? SpeedClass,
        bool IsEmphasis,
        bool IsHighlighted,
        bool IsStress)
    {
        public static readonly RenderState Default = new(null, null, null, null, null, null, null, false, false, false);

        public bool RequiresSpan(params string[] extraClasses) =>
            !string.IsNullOrWhiteSpace(EmotionClass) ||
            !string.IsNullOrWhiteSpace(VolumeClass) ||
            !string.IsNullOrWhiteSpace(DeliveryClass) ||
            !string.IsNullOrWhiteSpace(SpeedClass) ||
            IsEmphasis ||
            IsHighlighted ||
            IsStress ||
            extraClasses.Any(static value => !string.IsNullOrWhiteSpace(value));

        public string BuildCssClass(params string[] extraClasses)
        {
            var classes = new List<string>(8);

            if (IsEmphasis)
            {
                classes.Add("mk-em");
            }

            if (IsHighlighted)
            {
                classes.Add("mk-hl");
            }

            if (IsStress)
            {
                classes.Add("mk-stress");
            }

            if (!string.IsNullOrWhiteSpace(EmotionClass))
            {
                classes.Add(EmotionClass);
            }

            if (!string.IsNullOrWhiteSpace(VolumeClass))
            {
                classes.Add(VolumeClass);
            }

            if (!string.IsNullOrWhiteSpace(DeliveryClass))
            {
                classes.Add(DeliveryClass);
            }

            if (!string.IsNullOrWhiteSpace(SpeedClass))
            {
                classes.Add(SpeedClass);
            }

            classes.AddRange(extraClasses.Where(static value => !string.IsNullOrWhiteSpace(value)));
            return string.Join(" ", classes);
        }

        public void AppendHtmlAttributes(StringBuilder builder, params string[] extraClasses)
        {
            var classes = BuildCssClass(extraClasses);
            if (!string.IsNullOrWhiteSpace(classes))
            {
                AppendAttribute(builder, "class", classes);
            }

            if (!string.IsNullOrWhiteSpace(VolumeValue))
            {
                AppendAttribute(builder, TpsVisualCueContracts.VolumeAttributeName, VolumeValue);
            }

            if (!string.IsNullOrWhiteSpace(DeliveryValue))
            {
                AppendAttribute(builder, TpsVisualCueContracts.DeliveryAttributeName, DeliveryValue);
            }

            if (!string.IsNullOrWhiteSpace(SpeedValue))
            {
                AppendAttribute(builder, TpsVisualCueContracts.SpeedAttributeName, SpeedValue);
            }

            if (IsStress)
            {
                AppendAttribute(builder, TpsVisualCueContracts.StressAttributeName, TpsVisualCueContracts.StressAttributeValue);
            }
        }

        private static void AppendAttribute(StringBuilder builder, string name, string value)
        {
            builder.Append(' ')
                .Append(name)
                .Append("=\"")
                .Append(WebUtility.HtmlEncode(value))
                .Append('"');
        }
    }

    private enum ScopeKind
    {
        Root,
        Neutral,
        Style,
        Wpm,
        Pronunciation
    }
}
