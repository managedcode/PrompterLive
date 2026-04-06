using PrompterOne.Core.Services;
using PrompterOne.Core.Services.Editor;
using PrompterOne.Shared.Services.Editor;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class EditorOutlineBuilderTests
{
    private readonly EditorOutlineBuilder _builder = new();
    private readonly TpsFrontMatterDocumentService _frontMatter = new();
    private readonly TpsScriptDataFactory _scriptDataFactory = new();

    [Test]
    public void Build_MapsOutlineNavigationToRawSourceCharacterOffsets()
    {
        var source = AppTestLibrarySeedData.CreateDocuments()
            .Single(document => string.Equals(document.Id, AppTestData.Scripts.DemoId, StringComparison.Ordinal))
            .Text;
        var document = _frontMatter.Parse(source);
        var script = _scriptDataFactory.Build(source);

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
