using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Tools;

internal static class AiSpotlightNavigationToolCatalog
{
    public static void AddTo(List<AiSpotlightTool> tools, string? documentId)
    {
        tools.Add(AiSpotlightToolFactory.NavigationTool(
            AiSpotlightToolNames.NavLibrary,
            UiTextKey.AiSpotlightSuggestionOpenLibrary,
            UiTextKey.AiSpotlightSuggestionOpenLibraryDetail,
            AiSpotlightToolText.OpenLibrary,
            AppRoutes.Library));
        tools.Add(AiSpotlightToolFactory.NavigationTool(
            AiSpotlightToolNames.NavEditor,
            UiTextKey.AiSpotlightSuggestionOpenEditor,
            UiTextKey.AiSpotlightSuggestionOpenEditorDetail,
            AiSpotlightToolText.OpenEditor,
            DocumentRoute(AppRoutes.Editor, documentId)));
        tools.Add(AiSpotlightToolFactory.NavigationTool(
            AiSpotlightToolNames.NavLearn,
            UiTextKey.OnboardingOpenLearn,
            UiTextKey.OnboardingLearnBody,
            AiSpotlightToolText.OpenLearn,
            DocumentRoute(AppRoutes.Learn, documentId)));
        tools.Add(AiSpotlightToolFactory.NavigationTool(
            AiSpotlightToolNames.NavPrep,
            UiTextKey.HeaderPrep,
            UiTextKey.OnboardingLearnBody,
            AiSpotlightToolText.OpenPrep,
            DocumentRoute(AppRoutes.Prep, documentId)));
        tools.Add(AiSpotlightToolFactory.NavigationTool(
            AiSpotlightToolNames.NavTeleprompter,
            UiTextKey.OnboardingOpenTeleprompter,
            UiTextKey.OnboardingTeleprompterBody,
            AiSpotlightToolText.OpenTeleprompter,
            DocumentRoute(AppRoutes.Teleprompter, documentId)));
        tools.Add(AiSpotlightToolFactory.NavigationTool(
            AiSpotlightToolNames.NavGoLive,
            UiTextKey.OnboardingOpenGoLive,
            UiTextKey.OnboardingGoLiveBody,
            AiSpotlightToolText.OpenGoLive,
            DocumentRoute(AppRoutes.GoLive, documentId)));
        tools.Add(AiSpotlightToolFactory.NavigationTool(
            AiSpotlightToolNames.NavSettings,
            UiTextKey.AiSpotlightSuggestionOpenSettings,
            UiTextKey.AiSpotlightSuggestionOpenSettingsDetail,
            AiSpotlightToolText.OpenSettings,
            AppRoutes.Settings));
        tools.Add(AiSpotlightToolFactory.NavigationTool(
            AiSpotlightToolNames.ScriptNew,
            UiTextKey.HeaderNewScript,
            UiTextKey.AiSpotlightSuggestionOpenEditorDetail,
            AiSpotlightToolText.StartNewScript,
            AppRoutes.Editor));
    }

    private static string DocumentRoute(string route, string? documentId) =>
        route switch
        {
            AppRoutes.Editor when !string.IsNullOrWhiteSpace(documentId) => AppRoutes.EditorWithId(documentId),
            AppRoutes.Learn when !string.IsNullOrWhiteSpace(documentId) => AppRoutes.LearnWithId(documentId),
            AppRoutes.Prep when !string.IsNullOrWhiteSpace(documentId) => AppRoutes.PrepWithId(documentId),
            AppRoutes.Teleprompter when !string.IsNullOrWhiteSpace(documentId) => AppRoutes.TeleprompterWithId(documentId),
            AppRoutes.GoLive when !string.IsNullOrWhiteSpace(documentId) => AppRoutes.GoLiveWithId(documentId),
            _ => route
        };
}
