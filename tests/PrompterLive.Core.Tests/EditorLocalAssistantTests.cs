using PrompterLive.Core.Models.Editor;
using PrompterLive.Core.Services.Editor;

namespace PrompterLive.Core.Tests;

public sealed class EditorLocalAssistantTests
{
    private readonly EditorLocalAssistant _assistant = new();

    [Fact]
    public void Apply_Simplify_RewritesSelectedText()
    {
        const string source = "A transformative workflow can utilize focus.";
        var selection = new EditorSelectionRange(2, source.Length - 7);

        var result = _assistant.Apply(source, selection, EditorAiAssistAction.Simplify);

        Assert.Contains("clear workflow can use", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_AddDeliveryPauses_ReflowsFullTextWhenNoSelection()
    {
        const string source = "Welcome, everyone. We are ready; stay focused.";

        var result = _assistant.Apply(source, EditorSelectionRange.Empty, EditorAiAssistAction.AddDeliveryPauses);

        Assert.Contains(", /", result.Text, StringComparison.Ordinal);
        Assert.Contains(". //", result.Text, StringComparison.Ordinal);
        Assert.Contains("; /", result.Text, StringComparison.Ordinal);
    }
}
