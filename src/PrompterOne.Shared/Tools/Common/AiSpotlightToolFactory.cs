using PrompterOne.Core.AI.Models;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Tools;

internal static class AiSpotlightToolScopes
{
    public const string Global = "global";
    public const string Agent = "agent";
    public const string Editor = "editor";
    public const string Graph = "graph";
    public const string Hotkey = "hotkey";
    public const string Learn = "learn";
    public const string Library = "library";
    public const string Media = "media";
    public const string Navigation = "navigation";
    public const string Settings = "settings";
    public const string Streaming = "streaming";
    public const string Teleprompter = "teleprompter";
}

internal static class AiSpotlightToolParameters
{
    private const string BooleanType = "boolean";
    private const string IntegerType = "integer";
    private const string JsonType = "json";
    private const string StringType = "string";

    public static readonly IReadOnlyList<ScriptAgentAppToolParameter> None = [];

    public static ScriptAgentAppToolParameter Boolean(string name, string description, bool required = true) =>
        new(name, BooleanType, description, required);

    public static ScriptAgentAppToolParameter Integer(string name, string description, bool required = true) =>
        new(name, IntegerType, description, required);

    public static ScriptAgentAppToolParameter Json(string name, string description, bool required = true) =>
        new(name, JsonType, description, required);

    public static ScriptAgentAppToolParameter String(string name, string description, bool required = true) =>
        new(name, StringType, description, required);

    public static IReadOnlyList<ScriptAgentAppToolParameter> Of(params ScriptAgentAppToolParameter[] parameters) =>
        parameters;
}

internal static class AiSpotlightToolFactory
{
    public static AiSpotlightTool AgentTool(
        string name,
        UiTextKey label,
        UiTextKey detail,
        string prompt,
        string scope,
        IReadOnlyList<ScriptAgentAppToolParameter>? parameters = null,
        bool readOnly = true,
        bool idempotent = true,
        bool destructive = false,
        bool openWorld = false,
        bool requiresApproval = false) =>
        new(
            name,
            AiSpotlightSuggestionKind.Command,
            label,
            detail,
            prompt,
            AiSpotlightToolDispatchKinds.Agent,
            scope,
            ReadOnly: readOnly,
            Idempotent: idempotent,
            Destructive: destructive,
            OpenWorld: openWorld,
            RequiresApproval: requiresApproval,
            Parameters: parameters ?? AiSpotlightToolParameters.None);

    public static AiSpotlightTool NavigationTool(
        string name,
        UiTextKey label,
        UiTextKey detail,
        string prompt,
        string route,
        string scope = AiSpotlightToolScopes.Navigation) =>
        new(
            name,
            AiSpotlightSuggestionKind.Navigation,
            label,
            detail,
            prompt,
            AiSpotlightToolDispatchKinds.Navigation,
            scope,
            route,
            ReadOnly: false);

    public static AiSpotlightTool SettingsTool(
        string name,
        UiTextKey label,
        UiTextKey detail,
        string prompt,
        IReadOnlyList<ScriptAgentAppToolParameter>? parameters = null,
        bool readOnly = false,
        bool idempotent = true,
        bool openWorld = false,
        bool requiresApproval = false) =>
        AgentTool(
            name,
            label,
            detail,
            prompt,
            AiSpotlightToolScopes.Settings,
            parameters,
            readOnly,
            idempotent,
            destructive: false,
            openWorld,
            requiresApproval);

    public static AiSpotlightTool SensitiveMutationTool(
        string name,
        UiTextKey label,
        UiTextKey detail,
        string prompt,
        string scope,
        IReadOnlyList<ScriptAgentAppToolParameter>? parameters = null,
        bool destructive = false,
        bool openWorld = false) =>
        AgentTool(
            name,
            label,
            detail,
            prompt,
            scope,
            parameters,
            readOnly: false,
            idempotent: false,
            destructive,
            openWorld,
            requiresApproval: true);
}
