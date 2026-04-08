namespace PrompterOne.Web.UITests;

internal sealed class EditorMonacoState
{
    public List<string> DecorationClasses { get; set; } = [];

    public string Engine { get; set; } = string.Empty;

    public EditorMonacoLayout Layout { get; set; } = new();

    public string LanguageId { get; set; } = string.Empty;

    public int LineCount { get; set; }

    public bool Ready { get; set; }

    public double ScrollTop { get; set; }

    public EditorMonacoSelection Selection { get; set; } = new();

    public string Text { get; set; } = string.Empty;

    public EditorMonacoVisibleRange? VisibleRange { get; set; }
}

internal sealed class EditorMonacoLayout
{
    public double ContentLeft { get; set; }

    public double ContentWidth { get; set; }

    public double EditorWidth { get; set; }

    public double MinimapLeft { get; set; }

    public double MinimapWidth { get; set; }
}

internal sealed class EditorMonacoSelection
{
    public int Column { get; set; }

    public string Direction { get; set; } = string.Empty;

    public int End { get; set; }

    public int Line { get; set; }

    public int Start { get; set; }

    public double? ToolbarLeft { get; set; }

    public double? ToolbarTop { get; set; }
}

internal sealed class EditorMonacoVisibleRange
{
    public int EndLineNumber { get; set; }

    public int StartLineNumber { get; set; }
}

internal sealed class EditorMonacoCompletionList
{
    public List<EditorMonacoCompletionItem> Suggestions { get; set; } = [];
}

internal sealed class EditorMonacoCompletionItem
{
    public string Detail { get; set; } = string.Empty;

    public string Documentation { get; set; } = string.Empty;

    public string InsertText { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

internal sealed class EditorMonacoHoverResult
{
    public List<string> Contents { get; set; } = [];

    public EditorMonacoHoverRange? Range { get; set; }
}

internal sealed class EditorMonacoHoverRange
{
    public int EndColumn { get; set; }

    public int EndLineNumber { get; set; }

    public int StartColumn { get; set; }

    public int StartLineNumber { get; set; }
}

internal sealed class EditorMonacoTokenizedLine
{
    public string LineText { get; set; } = string.Empty;

    public List<EditorMonacoToken> Tokens { get; set; } = [];
}

internal sealed class EditorMonacoToken
{
    public int Offset { get; set; }

    public string Type { get; set; } = string.Empty;
}
