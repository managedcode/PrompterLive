using PrompterLive.Core.Services;
using PrompterLive.Core.Services.Editor;
using PrompterLive.Core.Samples;
using PrompterLive.Shared.Services.Editor;

namespace PrompterLive.App.Tests;

public sealed class EditorOutlineBuilderTests
{
    private readonly EditorOutlineBuilder _builder = new();
    private readonly TpsFrontMatterDocumentService _frontMatter = new();
    private readonly TpsParser _parser = new();

    [Fact]
    public void Build_MapsOutlineNavigationToRawSourceCharacterOffsets()
    {
        var source = SampleScriptCatalog.GetById(SampleScriptCatalog.DemoSampleId).Text;
        var document = _frontMatter.Parse(source);
        var script = _parser.ParseTps(source);

        var segments = _builder.Build(script, document.Body, document.BodyStartIndex);

        var solutionSegment = segments[2];
        var benefitsBlock = solutionSegment.Blocks[1];
        var expectedSegmentIndex = source.IndexOf("## [Solution|160WPM|focused]", StringComparison.Ordinal);
        var expectedBlockIndex = source.IndexOf("### [Benefits Block|160WPM|excited]", StringComparison.Ordinal);

        Assert.Equal(expectedSegmentIndex, solutionSegment.StartIndex);
        Assert.Equal(expectedBlockIndex, benefitsBlock.StartIndex);
        Assert.Equal("Benefits Block", benefitsBlock.Name);
        Assert.True(benefitsBlock.EndIndex > benefitsBlock.StartIndex);
    }
}
