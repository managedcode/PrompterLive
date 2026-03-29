using System.Globalization;

namespace PrompterLive.Core.Models.Documents;

public sealed partial record StoredScriptSummary(
    string Id,
    string Title,
    string DocumentName,
    DateTimeOffset UpdatedAt,
    int WordCount,
    string? FolderId = null)
{
    private const string LandingUpdatedLabelFormat = "MMM dd • hh:mm tt";

    public string UpdatedLabel => UpdatedAt.ToLocalTime().ToString(LandingUpdatedLabelFormat, CultureInfo.CurrentCulture);

    public string RowAutomationId => $"library-script-{Id}";

    public string TitleAutomationId => $"library-script-title-{Id}";

    public string LoadAutomationId => $"library-script-load-{Id}";
}
