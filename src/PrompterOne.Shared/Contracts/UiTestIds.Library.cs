namespace PrompterOne.Shared.Contracts;

public static partial class UiTestIds
{
    public static class Library
    {
        public const string Sidebar = "library-sidebar";
        public const string SidebarClose = "library-sidebar-close";
        public const string SidebarScrim = "library-sidebar-scrim";
        public const string SidebarToggle = "library-sidebar-toggle";
        public const string Page = "library-page";
        public const string CreateScript = "library-create-script";
        public const string CreateScriptSurface = "library-create-script-surface";
        public const string FolderAll = "library-folder-all";
        public const string FolderFavorites = "library-folder-favorites";
        public const string FolderChips = "library-folder-chips";
        public const string FolderCreateStart = "library-folder-create-start";
        public const string FolderCreateTile = "library-folder-create-tile";
        public const string FolderCreateTileSurface = "library-folder-create-tile-surface";
        public const string NewFolderCard = "library-new-folder-card";
        public const string NewFolderCancel = "library-new-folder-cancel";
        public const string NewFolderName = "library-new-folder-name";
        public const string NewFolderOverlay = "library-new-folder-overlay";
        public const string NewFolderParent = "library-new-folder-parent";
        public const string NewFolderSubmit = "library-new-folder-submit";
        public const string NewFolderTitle = "library-new-folder-title";
        public const string OpenSettings = "library-open-settings";
        public const string OrganizationModeGroup = "library-organization-mode-group";
        public const string SectionFoldersTitle = "library-section-folders-title";
        public const string SortLabel = "library-sort-label";
        public const string SortDate = "library-sort-date";
        public const string SortDuration = "library-sort-duration";
        public const string SortName = "library-sort-name";
        public const string SortWpm = "library-sort-wpm";
        public const string SortAuthor = "library-sort-author";
        public const string SortProject = "library-sort-project";
        public const string ToneMetadataToggle = "library-tone-metadata-toggle";
        public const string CardsGrid = "library-cards-grid";

        public static string BreadcrumbCurrent(string folderId) => $"library-breadcrumb-{folderId}";

        public static string Card(string scriptId) => $"library-card-{scriptId}";

        public static string CardSurface(string scriptId) => $"library-card-surface-{scriptId}";

        public static string CardDuplicate(string scriptId) => $"library-card-duplicate-{scriptId}";

        public static string CardDelete(string scriptId) => $"library-card-delete-{scriptId}";

        public static string CardLearn(string scriptId) => $"library-card-learn-{scriptId}";

        public static string CardPrep(string scriptId) => $"library-card-prep-{scriptId}";

        public static string CardFavorite(string scriptId) => $"library-card-favorite-{scriptId}";

        public static string CardMenu(string scriptId) => $"library-card-menu-{scriptId}";

        public static string CardMenuDropdown(string scriptId) => $"library-card-menu-dropdown-{scriptId}";

        public static string CardMenuWrap(string scriptId) => $"library-card-menu-wrap-{scriptId}";

        public static string CardRead(string scriptId) => $"library-card-read-{scriptId}";

        public static string CardDuration(string scriptId) => $"library-card-duration-{scriptId}";

        public static string CardSegmentCount(string scriptId) => $"library-card-segment-count-{scriptId}";

        public static string CardTone(string scriptId) => $"library-card-tone-{scriptId}";

        public static string CardWordCount(string scriptId) => $"library-card-word-count-{scriptId}";

        public static string CardWpm(string scriptId) => $"library-card-wpm-{scriptId}";

        public static string Folder(string folderId) => $"library-folder-{folderId}";

        public static string FolderChip(string folderId) => $"library-folder-chip-{folderId}";

        public static string Move(string scriptId, string folderId) => $"library-move-{scriptId}-{folderId}";

        public static string OrganizationModeOption(string mode) => $"library-organization-mode-{mode}";
    }
}
