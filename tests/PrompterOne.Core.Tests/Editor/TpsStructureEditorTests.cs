using PrompterOne.Core.Models.Editor;
using PrompterOne.Core.Services.Editor;

namespace PrompterOne.Core.Tests;

public sealed class TpsStructureEditorTests
{
    private readonly TpsStructureEditor _editor = new();

    [Test]
    public void TryRead_ReadsOrderIndependentSegmentHeaderFields()
    {
        const string source = """
                              ## [Introduction|neutral|Speaker:Alex|280WPM|0:00-0:30]
                              ### [Overview Block|280WPM|focused|Speaker:Jordan]
                              Copy.
                              """;

        var success = _editor.TryRead(source, 0, out var snapshot);

        Assert.True(success);
        Assert.Equal(TpsStructureHeaderKind.Segment, snapshot.Kind);
        Assert.Equal("Introduction", snapshot.Name);
        Assert.Equal(280, snapshot.TargetWpm);
        Assert.Equal("neutral", snapshot.EmotionKey);
        Assert.Equal("Alex", snapshot.Speaker);
        Assert.Equal("0:00-0:30", snapshot.Timing);
    }

    [Test]
    public void Update_RewritesBlockHeaderInCanonicalOrder()
    {
        const string source = """
                              ## [Introduction|Speaker:Alex|neutral|0:00-0:30]
                              ### [Overview Block|focused|Speaker:Jordan]
                              Copy.
                              """;

        var startIndex = source.IndexOf("### [Overview Block|focused|Speaker:Jordan]", StringComparison.Ordinal);
        var success = _editor.TryRead(source, startIndex, out var snapshot);

        Assert.True(success);

        var mutation = _editor.Update(
            source,
            snapshot with
            {
                Name = "Closing Block",
                TargetWpm = 320,
                EmotionKey = "professional",
                Speaker = "Casey"
            });

        Assert.Contains("### [Closing Block|Speaker:Casey|320WPM|professional]", mutation.Text, StringComparison.Ordinal);
        Assert.Contains("Copy.", mutation.Text, StringComparison.Ordinal);
        Assert.True(mutation.Selection.Start < mutation.Selection.End);
    }
}
