using PrompterOne.Shared.Services;

namespace PrompterOne.Web.Tests;

public sealed class RuntimeLibrarySeedCatalogTests
{
    private const string PronunciationCue = "[pronunciation:/prəˌnʌnsiˈeɪʃən/]pronunciation[/pronunciation]";
    private const string PhoneticCue = "[phonetic:/fəˈnɛtɪk/]phonetic[/phonetic]";
    private const string PhraseLegatoCue = "[legato]legato cadence[/legato]";
    private const string DeprecatedColorCue = "[red]";

    [Test]
    public void RuntimeSeedCatalog_IncludesFullTpsCueMatrixStarterScript()
    {
        var document = Assert.Single(
            RuntimeLibrarySeedCatalog.CreateDocuments(),
            candidate => string.Equals(candidate.Id, AppTestData.Scripts.RuntimeTpsCueMatrixId, StringComparison.Ordinal));

        Assert.Equal(AppTestData.Scripts.TpsCueMatrixTitle, document.Title);
        Assert.Equal(AppTestData.Scripts.RuntimeTpsCueMatrixDocumentName, document.DocumentName);
        Assert.Equal(AppTestData.Folders.RuntimeInternalId, document.FolderId);
        Assert.Contains(PronunciationCue, document.Text, StringComparison.Ordinal);
        Assert.Contains(PhoneticCue, document.Text, StringComparison.Ordinal);
        Assert.Contains(PhraseLegatoCue, document.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(DeprecatedColorCue, document.Text, StringComparison.Ordinal);
    }
}
