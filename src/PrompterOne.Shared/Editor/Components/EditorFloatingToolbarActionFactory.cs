namespace PrompterOne.Shared.Components.Editor;

internal static class EditorFloatingToolbarActionFactory
{
    public static EditorToolbarActionDescriptor ClearColor(string key, string tooltip, string testId) =>
        new(
            key,
            EditorToolbarActionType.Command,
            "efb-menu-item",
            EditorActionContents.Label("RESET", "Remove cues", "unwrap", EditorActionContentTone.Reset),
            tooltip,
            testId,
            Command: new EditorCommandRequest(EditorCommandKind.ClearColor, string.Empty),
            PreventMouseDown: true);

    public static EditorToolbarActionDescriptor Insert(
        string key,
        EditorActionContentDescriptor content,
        string tooltip,
        string testId,
        string token,
        string cssClass = "efb-btn") =>
        new(
            key,
            EditorToolbarActionType.Command,
            cssClass,
            content,
            tooltip,
            testId,
            Command: new EditorCommandRequest(EditorCommandKind.Insert, token),
            PreventMouseDown: true);

    public static EditorToolbarActionDescriptor Toggle(
        string menuId,
        EditorActionContentDescriptor content,
        string tooltip,
        string testId,
        string cssClass = "efb-btn") =>
        new(menuId, EditorToolbarActionType.ToggleMenu, cssClass, content, tooltip, testId, MenuId: menuId, PreventMouseDown: true);

    public static EditorToolbarActionDescriptor Wrap(
        string key,
        EditorActionContentDescriptor content,
        string tooltip,
        string testId,
        string openingToken,
        string closingToken,
        string placeholder = "text",
        string cssClass = "efb-btn") =>
        new(
            key,
            EditorToolbarActionType.Command,
            cssClass,
            content,
            tooltip,
            testId,
            Command: new EditorCommandRequest(EditorCommandKind.Wrap, openingToken, closingToken, placeholder),
            PreventMouseDown: true);
}
