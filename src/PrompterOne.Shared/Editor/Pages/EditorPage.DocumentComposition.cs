using System.Globalization;
using PrompterOne.Core.Services.Editor;

namespace PrompterOne.Shared.Pages;

public partial class EditorPage
{
    private string BuildPersistedDocument(string bodyText) =>
        _frontMatterService.Build(BuildMetadata(), bodyText);

    private Dictionary<string, string> BuildMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [TpsFrontMatterDocumentService.MetadataKeys.Title] = _screenTitle,
            [TpsFrontMatterDocumentService.MetadataKeys.Author] = string.IsNullOrWhiteSpace(_author) ? DefaultAuthor : _author,
            [TpsFrontMatterDocumentService.MetadataKeys.Profile] = _profile,
            [TpsFrontMatterDocumentService.MetadataKeys.BaseWpm] = _baseWpm.ToString(CultureInfo.InvariantCulture),
            [TpsFrontMatterDocumentService.MetadataKeys.SpeedOffsetXslow] = _xslowOffset.ToString(CultureInfo.InvariantCulture),
            [TpsFrontMatterDocumentService.MetadataKeys.SpeedOffsetSlow] = _slowOffset.ToString(CultureInfo.InvariantCulture),
            [TpsFrontMatterDocumentService.MetadataKeys.SpeedOffsetFast] = _fastOffset.ToString(CultureInfo.InvariantCulture),
            [TpsFrontMatterDocumentService.MetadataKeys.SpeedOffsetXfast] = _xfastOffset.ToString(CultureInfo.InvariantCulture),
            [TpsFrontMatterDocumentService.MetadataKeys.Version] = string.IsNullOrWhiteSpace(_version) ? DefaultVersion : _version
        };

        if (!string.IsNullOrWhiteSpace(_createdDate))
        {
            metadata[TpsFrontMatterDocumentService.MetadataKeys.Created] = _createdDate;
        }

        if (!string.IsNullOrWhiteSpace(_displayDuration))
        {
            metadata[TpsFrontMatterDocumentService.MetadataKeys.Duration] = _displayDuration;
        }

        return metadata;
    }
}
