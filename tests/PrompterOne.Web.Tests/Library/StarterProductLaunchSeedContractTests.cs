namespace PrompterOne.Web.Tests;

public sealed class StarterProductLaunchSeedContractTests
{
    private const string ReadableVisionPronunciationCue = "[pronunciation:VI-zhun]vision[/pronunciation]";
    private const string LegacyVisionPronunciationCue = "[phonetic:ˈviʒən]vision[/phonetic]";
    private const string StarterProductLaunchSeedRelativePath = "src/PrompterOne.Shared/Library/SeedData/starter-product-launch.tps";
    private const string StarterProductLaunchFixtureRelativePath = "tests/TestData/Scripts/test-product-launch.tps";

    [Test]
    [Arguments(StarterProductLaunchSeedRelativePath)]
    [Arguments(StarterProductLaunchFixtureRelativePath)]
    public void ProductLaunchVisionSample_UsesReadablePronunciationCue(string relativePath)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../", relativePath));
        var scriptText = File.ReadAllText(absolutePath);

        Assert.Contains(ReadableVisionPronunciationCue, scriptText, StringComparison.Ordinal);
        Assert.DoesNotContain(LegacyVisionPronunciationCue, scriptText, StringComparison.Ordinal);
        Assert.DoesNotContain("ˈ", scriptText, StringComparison.Ordinal);
    }
}
