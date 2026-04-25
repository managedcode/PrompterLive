using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Components.Library;

public enum LibraryOrganizationMode
{
    Folders,
    Projects,
    Shows,
    Episodes,
    Sequences
}

public sealed record LibraryOrganizationTerminology(
    LibraryOrganizationMode Mode,
    string Value,
    UiTextKey OptionLabelKey,
    UiTextKey SectionTitleKey,
    UiTextKey NewItemLabelKey,
    UiTextKey CreateTitleKey,
    UiTextKey CreateDescriptionKey,
    UiTextKey PlaceholderKey,
    UiTextKey CreateTooltipKey,
    UiTextKey NoItemKey,
    UiTextKey ParentLabelKey);

public static class LibraryOrganizationTerminologyCatalog
{
    public static IReadOnlyList<LibraryOrganizationTerminology> All { get; } =
    [
        new(
            LibraryOrganizationMode.Folders,
            "folders",
            UiTextKey.LibraryOrganizationFolders,
            UiTextKey.LibraryFolders,
            UiTextKey.LibraryNewFolder,
            UiTextKey.LibraryCreateFolderTitle,
            UiTextKey.LibraryCreateFolderDescription,
            UiTextKey.LibraryFolderPlaceholder,
            UiTextKey.TooltipCreateFolder,
            UiTextKey.CommonNoFolder,
            UiTextKey.CommonParent),
        new(
            LibraryOrganizationMode.Projects,
            "projects",
            UiTextKey.LibraryOrganizationProjects,
            UiTextKey.LibraryProjects,
            UiTextKey.LibraryNewProject,
            UiTextKey.LibraryCreateProjectTitle,
            UiTextKey.LibraryCreateProjectDescription,
            UiTextKey.LibraryProjectPlaceholder,
            UiTextKey.TooltipCreateProject,
            UiTextKey.CommonNoProject,
            UiTextKey.CommonParentProject),
        new(
            LibraryOrganizationMode.Shows,
            "shows",
            UiTextKey.LibraryOrganizationShows,
            UiTextKey.LibraryShows,
            UiTextKey.LibraryNewShow,
            UiTextKey.LibraryCreateShowTitle,
            UiTextKey.LibraryCreateShowDescription,
            UiTextKey.LibraryShowPlaceholder,
            UiTextKey.TooltipCreateShow,
            UiTextKey.CommonNoShow,
            UiTextKey.CommonParentShow),
        new(
            LibraryOrganizationMode.Episodes,
            "episodes",
            UiTextKey.LibraryOrganizationEpisodes,
            UiTextKey.LibraryEpisodes,
            UiTextKey.LibraryNewEpisode,
            UiTextKey.LibraryCreateEpisodeTitle,
            UiTextKey.LibraryCreateEpisodeDescription,
            UiTextKey.LibraryEpisodePlaceholder,
            UiTextKey.TooltipCreateEpisode,
            UiTextKey.CommonNoEpisode,
            UiTextKey.CommonParentEpisode),
        new(
            LibraryOrganizationMode.Sequences,
            "sequences",
            UiTextKey.LibraryOrganizationSequences,
            UiTextKey.LibrarySequences,
            UiTextKey.LibraryNewSequence,
            UiTextKey.LibraryCreateSequenceTitle,
            UiTextKey.LibraryCreateSequenceDescription,
            UiTextKey.LibrarySequencePlaceholder,
            UiTextKey.TooltipCreateSequence,
            UiTextKey.CommonNoSequence,
            UiTextKey.CommonParentSequence)
    ];

    public static LibraryOrganizationTerminology Resolve(LibraryOrganizationMode mode) =>
        All.FirstOrDefault(item => item.Mode == mode) ?? All[0];
}
