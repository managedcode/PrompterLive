using PrompterOne.Shared.Components.Editor;

namespace PrompterOne.Web.UITests;

public enum EditorScenarioSelectionMode
{
    WrapSelection,
    InsertAtCaret,
    ClearColorSelection
}

public sealed record EditorCommandScenario(
    string TestId,
    string? MenuTriggerTestId,
    EditorCommandRequest Command,
    EditorScenarioSelectionMode SelectionMode);

public sealed record EditorMenuScenario(
    string TriggerTestId,
    string PanelTestId);

public sealed record EditorAiScenario(
    string TestId,
    bool RequiresSelection,
    string SourceText,
    string ExpectedFragment);

internal static class EditorToolbarCoverageScenarios
{
    public static IReadOnlyList<EditorCommandScenario> ToolbarCommandScenarios { get; } = BuildToolbarCommandScenarios();

    public static IReadOnlyList<EditorCommandScenario> FloatingCommandScenarios { get; } = BuildFloatingCommandScenarios();

    public static IReadOnlyList<EditorMenuScenario> MenuScenarios { get; } = BuildMenuScenarios();

    public static IReadOnlyList<EditorMenuScenario> FloatingMenuScenarios { get; } = BuildFloatingMenuScenarios();

    public static IReadOnlyList<EditorAiScenario> AiScenarios { get; } = BuildAiScenarios();

    private static IReadOnlyList<EditorAiScenario> BuildAiScenarios()
    {
        var scenarios = new List<EditorAiScenario>();

        scenarios.AddRange(
            EditorToolbarCatalog.Sections
                .SelectMany(section => section.MainActions)
                .Where(action => action.ActionType == EditorToolbarActionType.Ai && !string.IsNullOrWhiteSpace(action.TestId))
                .Select(action => new EditorAiScenario(
                    action.TestId!,
                    RequiresSelection: true,
                    BrowserTestSource.SimplifySource,
                    BrowserTestSource.SimplifiedToken)));

        scenarios.AddRange(
            EditorToolbarCatalog.FloatingActionGroups
                .SelectMany(group => group)
                .Where(action => action.ActionType == EditorToolbarActionType.Ai && !string.IsNullOrWhiteSpace(action.TestId))
                .Select(action => new EditorAiScenario(
                    action.TestId!,
                    RequiresSelection: true,
                    BrowserTestSource.SimplifySource,
                    BrowserTestSource.SimplifiedToken)));

        return scenarios;
    }

    private static IReadOnlyList<EditorCommandScenario> BuildFloatingCommandScenarios() =>
        BuildFloatingDirectCommandScenarios()
            .Concat(BuildFloatingMenuCommandScenarios())
            .ToArray();

    private static IReadOnlyList<EditorMenuScenario> BuildFloatingMenuScenarios() =>
        EditorToolbarCatalog.FloatingMenus
            .Select(menu => new EditorMenuScenario(menu.TriggerTestId, menu.PanelTestId))
            .ToArray();

    private static IReadOnlyList<EditorMenuScenario> BuildMenuScenarios() =>
        EditorToolbarCatalog.Sections
            .Where(section => !string.IsNullOrWhiteSpace(section.DropdownTestId))
            .SelectMany(section => section.MainActions
                .Where(action => action.ActionType == EditorToolbarActionType.ToggleMenu && !string.IsNullOrWhiteSpace(action.TestId))
                .Select(action => new EditorMenuScenario(action.TestId!, section.DropdownTestId!)))
            .ToArray();

    private static IEnumerable<EditorCommandScenario> BuildFloatingDirectCommandScenarios() =>
        EditorToolbarCatalog.FloatingActionGroups
            .SelectMany(group => group)
            .Where(action => action.ActionType == EditorToolbarActionType.Command && action.Command is not null && !string.IsNullOrWhiteSpace(action.TestId))
            .Select(action => new EditorCommandScenario(
                action.TestId!,
                null,
                action.Command!,
                GetFloatingSelectionMode(action.Command!)));

    private static IEnumerable<EditorCommandScenario> BuildFloatingMenuCommandScenarios() =>
        EditorToolbarCatalog.FloatingMenus
            .SelectMany(menu => menu.DropdownGroups
                .SelectMany(group => group.Actions
                    .Where(action => action.ActionType == EditorToolbarActionType.Command && action.Command is not null && !string.IsNullOrWhiteSpace(action.TestId))
                    .Select(action => new EditorCommandScenario(
                        action.TestId!,
                        menu.TriggerTestId,
                        action.Command!,
                        GetFloatingSelectionMode(action.Command!)))));

    private static IReadOnlyList<EditorCommandScenario> BuildToolbarCommandScenarios()
    {
        var scenarios = new List<EditorCommandScenario>();

        foreach (var section in EditorToolbarCatalog.Sections)
        {
            scenarios.AddRange(BuildSectionCommandScenarios(section.MainActions, null));

            if (!string.IsNullOrWhiteSpace(section.MainActions.FirstOrDefault(action => action.ActionType == EditorToolbarActionType.ToggleMenu)?.TestId))
            {
                var menuTriggerTestId = section.MainActions
                    .First(action => action.ActionType == EditorToolbarActionType.ToggleMenu)
                    .TestId;

                foreach (var group in section.DropdownGroups)
                {
                    scenarios.AddRange(BuildSectionCommandScenarios(group.Actions, menuTriggerTestId));
                }
            }
        }

        return scenarios;
    }

    private static IEnumerable<EditorCommandScenario> BuildSectionCommandScenarios(
        IReadOnlyList<EditorToolbarActionDescriptor> actions,
        string? menuTriggerTestId) =>
        actions
            .Where(action => action.ActionType == EditorToolbarActionType.Command && action.Command is not null && !string.IsNullOrWhiteSpace(action.TestId))
            .Select(action => new EditorCommandScenario(
                action.TestId!,
                menuTriggerTestId,
                action.Command!,
                GetSelectionMode(action.Command!)));

    private static EditorScenarioSelectionMode GetSelectionMode(EditorCommandRequest command) =>
        command.Kind switch
        {
            EditorCommandKind.Wrap => EditorScenarioSelectionMode.WrapSelection,
            EditorCommandKind.ClearColor => EditorScenarioSelectionMode.ClearColorSelection,
            _ => EditorScenarioSelectionMode.InsertAtCaret
        };

    private static EditorScenarioSelectionMode GetFloatingSelectionMode(EditorCommandRequest command) =>
        command.Kind == EditorCommandKind.ClearColor
            ? EditorScenarioSelectionMode.ClearColorSelection
            : EditorScenarioSelectionMode.WrapSelection;

    private static class BrowserTestSource
    {
        public const string SimplifiedToken = "clear moment";
        public const string SimplifySource = """
            ## [Intro|140WPM|warm]
            ### [Opening Block|140WPM]
            transformative moment
            """;
    }
}
