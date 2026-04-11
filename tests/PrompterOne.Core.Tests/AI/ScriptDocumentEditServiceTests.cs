using PrompterOne.Core.AI.Models;
using PrompterOne.Core.AI.Services;

namespace PrompterOne.Core.Tests;

public sealed class ScriptDocumentEditServiceTests
{
    private readonly ScriptDocumentEditService _service = new();

    [Test]
    public void Apply_Replace_OnlyMutatesApprovedRange()
    {
        const string source = "Keep this.\nRewrite this paragraph.\nKeep that.";
        var start = source.IndexOf("Rewrite", StringComparison.Ordinal);
        var end = start + "Rewrite".Length;
        var plan = new ScriptDocumentEditPlan(
            ScriptDocumentRevision.Create(source),
            [ScriptDocumentEditOperation.Replace(new ScriptDocumentRange(start, end), "Polish")]);

        var result = _service.Apply(source, plan);

        Assert.Equal("Keep this.\nPolish this paragraph.\nKeep that.", result.Text);
        Assert.Equal(new ScriptDocumentRange(start, start + "Polish".Length), result.AppliedEdits[0].UpdatedRange);
    }

    [Test]
    public void Apply_Insert_AddsTextAtOffset()
    {
        const string source = "Hello world";
        var plan = new ScriptDocumentEditPlan(
            ScriptDocumentRevision.Create(source),
            [ScriptDocumentEditOperation.Insert("Hello ".Length, "clear ")]);

        var result = _service.Apply(source, plan);

        Assert.Equal("Hello clear world", result.Text);
    }

    [Test]
    public void Apply_Delete_RemovesApprovedRange()
    {
        const string source = "Keep noisy text.";
        var start = source.IndexOf(" noisy", StringComparison.Ordinal);
        var plan = new ScriptDocumentEditPlan(
            ScriptDocumentRevision.Create(source),
            [ScriptDocumentEditOperation.Delete(new ScriptDocumentRange(start, start + " noisy".Length))]);

        var result = _service.Apply(source, plan);

        Assert.Equal("Keep text.", result.Text);
    }

    [Test]
    public void ReadRange_ReturnsRequestedSliceWithoutMutationPlan()
    {
        const string source = "Line one\nLine two";

        var result = _service.ReadRange(source, new ScriptDocumentRange("Line one\n".Length, source.Length));

        Assert.Equal("Line two", result);
    }

    [Test]
    public void Apply_RejectsStaleRevision()
    {
        const string source = "Fresh text.";
        var plan = new ScriptDocumentEditPlan(
            ScriptDocumentRevision.Create("Old text."),
            [ScriptDocumentEditOperation.Replace(new ScriptDocumentRange(0, 5), "New")]);

        Assert.Throws<InvalidOperationException>(() => _service.Apply(source, plan));
    }

    [Test]
    public void Apply_RejectsOutOfBoundsRange()
    {
        const string source = "Short";
        var plan = new ScriptDocumentEditPlan(
            ScriptDocumentRevision.Create(source),
            [ScriptDocumentEditOperation.Delete(new ScriptDocumentRange(0, source.Length + 1))]);

        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Apply(source, plan));
    }

    [Test]
    public void Apply_RejectsOverlappingRanges()
    {
        const string source = "Replace these words.";
        var plan = new ScriptDocumentEditPlan(
            ScriptDocumentRevision.Create(source),
            [
                ScriptDocumentEditOperation.Replace(new ScriptDocumentRange(0, 7), "Keep"),
                ScriptDocumentEditOperation.Delete(new ScriptDocumentRange(4, 13))
            ]);

        Assert.Throws<InvalidOperationException>(() => _service.Apply(source, plan));
    }
}
