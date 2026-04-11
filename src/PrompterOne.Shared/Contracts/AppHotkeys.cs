using Microsoft.AspNetCore.Components.Web;

namespace PrompterOne.Shared.Contracts;

public enum AppHotkeySurface
{
    Global,
    Editor,
    Learn,
    Teleprompter,
    GoLive
}

public enum AppHotkeyAction
{
    GlobalOpenAssistant,
    EditorUndo,
    EditorRedo,
    EditorSelectAll,
    LearnBack,
    LearnPlayPause,
    LearnSpeedDown,
    LearnSpeedUp,
    LearnStepBackward,
    LearnStepBackwardLarge,
    LearnStepForward,
    LearnStepForwardLarge,
    LearnToggleLoop,
    TeleprompterBack,
    TeleprompterPlayPause,
    TeleprompterPreviousBlock,
    TeleprompterNextBlock,
    TeleprompterMirrorHorizontal,
    TeleprompterMirrorVertical,
    TeleprompterOrientation,
    TeleprompterFullscreen,
    TeleprompterAlignmentLeft,
    TeleprompterAlignmentCenter,
    TeleprompterAlignmentRight,
    TeleprompterAlignmentJustify,
    TeleprompterCamera,
    GoLiveDirectorMode,
    GoLiveStudioMode,
    GoLiveToggleLeftRail,
    GoLiveToggleRightRail,
    GoLiveToggleFullProgram,
    GoLiveTakeToAir,
    GoLiveToggleRecording,
    GoLiveToggleStream
}

public readonly record struct AppKeyboardShortcut(
    string DisplayText,
    string Key,
    bool Shift = false,
    bool Alt = false,
    bool Ctrl = false)
{
    public bool Matches(KeyboardEventArgs args) =>
        string.Equals(UiKeyboardKeys.Normalize(args.Key), Key, StringComparison.Ordinal)
        && args.ShiftKey == Shift
        && args.AltKey == Alt
        && args.CtrlKey == Ctrl;
}

public sealed record AppHotkeyDefinition(
    string Id,
    AppHotkeyAction Action,
    IReadOnlyList<AppKeyboardShortcut> Shortcuts);

public sealed record AppHotkeyGroup(
    AppHotkeySurface Surface,
    string Id,
    IReadOnlyList<AppHotkeyDefinition> Definitions);

public static class AppHotkeys
{
    private static class ShortcutDisplayText
    {
        public const string BracketLeft = "[";
        public const string BracketRight = "]";
        public const string Camera = "C";
        public const string CommandA = "Ctrl/Cmd+A";
        public const string CommandK = "Ctrl/Cmd+K";
        public const string CommandShiftZ = "Ctrl/Cmd+Shift+Z";
        public const string CommandZ = "Ctrl/Cmd+Z";
        public const string Digit1 = "1";
        public const string Digit2 = "2";
        public const string Digit3 = "3";
        public const string Digit4 = "4";
        public const string Down = "Down";
        public const string Enter = "Enter";
        public const string Escape = "Esc";
        public const string Fullscreen = "F";
        public const string Left = "Left";
        public const string Loop = "L";
        public const string MirrorHorizontal = "H";
        public const string MirrorVertical = "V";
        public const string Orientation = "O";
        public const string PageDown = "PageDown";
        public const string PageUp = "PageUp";
        public const string Recording = "R";
        public const string Right = "Right";
        public const string Space = "Space";
        public const string Streaming = "S";
        public const string Up = "Up";
    }

    public static IReadOnlyList<AppHotkeyGroup> Groups { get; } =
    [
        new(
            AppHotkeySurface.Global,
            AppHotkeyIds.Groups.Global,
            [
                new(AppHotkeyIds.Definitions.GlobalOpenAssistant, AppHotkeyAction.GlobalOpenAssistant, [new(ShortcutDisplayText.CommandK, UiKeyboardKeys.KLower, Ctrl: true)])
            ]),
        new(
            AppHotkeySurface.Editor,
            AppHotkeyIds.Groups.Editor,
            [
                new(AppHotkeyIds.Definitions.EditorUndo, AppHotkeyAction.EditorUndo, [new(ShortcutDisplayText.CommandZ, UiKeyboardKeys.ZLower, Ctrl: true)]),
                new(AppHotkeyIds.Definitions.EditorRedo, AppHotkeyAction.EditorRedo, [new(ShortcutDisplayText.CommandShiftZ, UiKeyboardKeys.ZLower, Shift: true, Ctrl: true)]),
                new(AppHotkeyIds.Definitions.EditorSelectAll, AppHotkeyAction.EditorSelectAll, [new(ShortcutDisplayText.CommandA, UiKeyboardKeys.ALower, Ctrl: true)])
            ]),
        new(
            AppHotkeySurface.Learn,
            AppHotkeyIds.Groups.Learn,
            [
                new(AppHotkeyIds.Definitions.LearnBack, AppHotkeyAction.LearnBack, [new(ShortcutDisplayText.Escape, UiKeyboardKeys.Escape)]),
                new(AppHotkeyIds.Definitions.LearnPlayPause, AppHotkeyAction.LearnPlayPause, [new(ShortcutDisplayText.Space, UiKeyboardKeys.Space)]),
                new(AppHotkeyIds.Definitions.LearnSpeedDown, AppHotkeyAction.LearnSpeedDown, [new(ShortcutDisplayText.Down, UiKeyboardKeys.ArrowDown)]),
                new(AppHotkeyIds.Definitions.LearnSpeedUp, AppHotkeyAction.LearnSpeedUp, [new(ShortcutDisplayText.Up, UiKeyboardKeys.ArrowUp)]),
                new(AppHotkeyIds.Definitions.LearnStepBackward, AppHotkeyAction.LearnStepBackward, [new(ShortcutDisplayText.Left, UiKeyboardKeys.ArrowLeft)]),
                new(AppHotkeyIds.Definitions.LearnStepBackwardLarge, AppHotkeyAction.LearnStepBackwardLarge, [new(ShortcutDisplayText.PageUp, UiKeyboardKeys.PageUp)]),
                new(AppHotkeyIds.Definitions.LearnStepForward, AppHotkeyAction.LearnStepForward, [new(ShortcutDisplayText.Right, UiKeyboardKeys.ArrowRight)]),
                new(AppHotkeyIds.Definitions.LearnStepForwardLarge, AppHotkeyAction.LearnStepForwardLarge, [new(ShortcutDisplayText.PageDown, UiKeyboardKeys.PageDown)]),
                new(AppHotkeyIds.Definitions.LearnToggleLoop, AppHotkeyAction.LearnToggleLoop, [new(ShortcutDisplayText.Loop, UiKeyboardKeys.LLower)])
            ]),
        new(
            AppHotkeySurface.Teleprompter,
            AppHotkeyIds.Groups.Teleprompter,
            [
                new(AppHotkeyIds.Definitions.TeleprompterBack, AppHotkeyAction.TeleprompterBack, [new(ShortcutDisplayText.Escape, UiKeyboardKeys.Escape)]),
                new(AppHotkeyIds.Definitions.TeleprompterPlayPause, AppHotkeyAction.TeleprompterPlayPause, [new(ShortcutDisplayText.Space, UiKeyboardKeys.Space)]),
                new(AppHotkeyIds.Definitions.TeleprompterPreviousBlock, AppHotkeyAction.TeleprompterPreviousBlock, [new(ShortcutDisplayText.Left, UiKeyboardKeys.ArrowLeft), new(ShortcutDisplayText.PageUp, UiKeyboardKeys.PageUp)]),
                new(AppHotkeyIds.Definitions.TeleprompterNextBlock, AppHotkeyAction.TeleprompterNextBlock, [new(ShortcutDisplayText.Right, UiKeyboardKeys.ArrowRight), new(ShortcutDisplayText.PageDown, UiKeyboardKeys.PageDown)]),
                new(AppHotkeyIds.Definitions.TeleprompterMirrorHorizontal, AppHotkeyAction.TeleprompterMirrorHorizontal, [new(ShortcutDisplayText.MirrorHorizontal, UiKeyboardKeys.HLower)]),
                new(AppHotkeyIds.Definitions.TeleprompterMirrorVertical, AppHotkeyAction.TeleprompterMirrorVertical, [new(ShortcutDisplayText.MirrorVertical, UiKeyboardKeys.VLower)]),
                new(AppHotkeyIds.Definitions.TeleprompterOrientation, AppHotkeyAction.TeleprompterOrientation, [new(ShortcutDisplayText.Orientation, UiKeyboardKeys.OLower)]),
                new(AppHotkeyIds.Definitions.TeleprompterFullscreen, AppHotkeyAction.TeleprompterFullscreen, [new(ShortcutDisplayText.Fullscreen, UiKeyboardKeys.FLower)]),
                new(AppHotkeyIds.Definitions.TeleprompterAlignmentLeft, AppHotkeyAction.TeleprompterAlignmentLeft, [new(ShortcutDisplayText.Digit1, UiKeyboardKeys.Digit1)]),
                new(AppHotkeyIds.Definitions.TeleprompterAlignmentCenter, AppHotkeyAction.TeleprompterAlignmentCenter, [new(ShortcutDisplayText.Digit2, UiKeyboardKeys.Digit2)]),
                new(AppHotkeyIds.Definitions.TeleprompterAlignmentRight, AppHotkeyAction.TeleprompterAlignmentRight, [new(ShortcutDisplayText.Digit3, UiKeyboardKeys.Digit3)]),
                new(AppHotkeyIds.Definitions.TeleprompterAlignmentJustify, AppHotkeyAction.TeleprompterAlignmentJustify, [new(ShortcutDisplayText.Digit4, UiKeyboardKeys.Digit4)]),
                new(AppHotkeyIds.Definitions.TeleprompterCamera, AppHotkeyAction.TeleprompterCamera, [new(ShortcutDisplayText.Camera, UiKeyboardKeys.CLower)])
            ]),
        new(
            AppHotkeySurface.GoLive,
            AppHotkeyIds.Groups.GoLive,
            [
                new(AppHotkeyIds.Definitions.GoLiveDirectorMode, AppHotkeyAction.GoLiveDirectorMode, [new(ShortcutDisplayText.Digit1, UiKeyboardKeys.Digit1)]),
                new(AppHotkeyIds.Definitions.GoLiveStudioMode, AppHotkeyAction.GoLiveStudioMode, [new(ShortcutDisplayText.Digit2, UiKeyboardKeys.Digit2)]),
                new(AppHotkeyIds.Definitions.GoLiveToggleLeftRail, AppHotkeyAction.GoLiveToggleLeftRail, [new(ShortcutDisplayText.BracketLeft, UiKeyboardKeys.BracketLeft)]),
                new(AppHotkeyIds.Definitions.GoLiveToggleRightRail, AppHotkeyAction.GoLiveToggleRightRail, [new(ShortcutDisplayText.BracketRight, UiKeyboardKeys.BracketRight)]),
                new(AppHotkeyIds.Definitions.GoLiveToggleFullProgram, AppHotkeyAction.GoLiveToggleFullProgram, [new(ShortcutDisplayText.Fullscreen, UiKeyboardKeys.FLower)]),
                new(AppHotkeyIds.Definitions.GoLiveTakeToAir, AppHotkeyAction.GoLiveTakeToAir, [new(ShortcutDisplayText.Enter, UiKeyboardKeys.Enter)]),
                new(AppHotkeyIds.Definitions.GoLiveToggleRecording, AppHotkeyAction.GoLiveToggleRecording, [new(ShortcutDisplayText.Recording, UiKeyboardKeys.RLower)]),
                new(AppHotkeyIds.Definitions.GoLiveToggleStream, AppHotkeyAction.GoLiveToggleStream, [new(ShortcutDisplayText.Streaming, UiKeyboardKeys.SLower)])
            ])
    ];

    public static IReadOnlyList<AppHotkeyDefinition> GetDefinitions(AppHotkeySurface surface) =>
        Groups.First(group => group.Surface == surface).Definitions;

    public static bool TryResolve(AppHotkeySurface surface, KeyboardEventArgs args, out AppHotkeyAction action)
    {
        foreach (var definition in GetDefinitions(surface))
        {
            if (definition.Shortcuts.Any(shortcut => shortcut.Matches(args)))
            {
                action = definition.Action;
                return true;
            }
        }

        action = default;
        return false;
    }
}
