using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
using PrompterOne.Shared.Services;
using PrompterOne.Shared.Tests;

namespace PrompterOne.Web.Tests;

public sealed class EditorMetadataInteractionTests : BunitContext
{
    private readonly AppHarness _harness;

    public EditorMetadataInteractionTests()
    {
        _harness = TestHarnessFactory.Create(this);
    }

    [Test]
    public void EditorPage_UpdatesFrontMatterWhenMetadataChanges()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        cut.FindByTestId(UiTestIds.Editor.Title).Change(EditorMetadataTestSource.RetitledScript);
        cut.FindByTestId(UiTestIds.Editor.Profile).Change(EditorMetadataTestSource.ProfileRsvp);
        cut.FindByTestId(UiTestIds.Editor.BaseWpm).Change(EditorMetadataTestSource.BaseWpm210);
        cut.FindByTestId(UiTestIds.Editor.Duration).Change(AppTestData.Editor.DisplayDuration);
        cut.FindByTestId(UiTestIds.Editor.Author).Change(AppTestData.Editor.TestSpeaker);
        cut.FindByTestId(UiTestIds.Editor.Created).Change(AppTestData.Editor.CreatedDate);
        cut.FindByTestId(UiTestIds.Editor.Version).Change(AppTestData.Editor.Version);

        cut.WaitForAssertion(() =>
        {
            var metadata = _harness.Session.State.CompiledScript?.Metadata;
            var visibleSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value") ?? string.Empty;
            var persistedText = _harness.Session.State.Text;
            var baseWpmStatus = cut.FindByTestId(UiTestIds.Editor.StatusBaseWpm).TextContent;
            var durationStatus = cut.FindByTestId(UiTestIds.Editor.StatusDuration).TextContent;

            Assert.NotNull(metadata);
            Assert.Equal(EditorMetadataTestSource.RetitledScript, metadata!["title"]);
            Assert.Equal(EditorMetadataTestSource.ProfileRsvp, metadata!["profile"]);
            Assert.Equal(EditorMetadataTestSource.BaseWpm210, metadata["base_wpm"]);
            Assert.Equal(AppTestData.Editor.DisplayDuration, metadata["duration"]);
            Assert.Equal(AppTestData.Editor.TestSpeaker, metadata["author"]);
            Assert.Equal(AppTestData.Editor.CreatedDate, metadata["created"]);
            Assert.Equal(AppTestData.Editor.Version, metadata["version"]);
            Assert.Equal(EditorMetadataTestSource.RetitledScript, _harness.Session.State.Title);
            Assert.Contains(EditorMetadataTestSource.BaseWpm210, baseWpmStatus, StringComparison.Ordinal);
            Assert.Contains(AppTestData.Editor.DisplayDuration, durationStatus, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorMetadataTestSource.TitleField, visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorMetadataTestSource.AuthorField, visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorMetadataTestSource.DurationField, visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorMetadataTestSource.VersionField, visibleSource, StringComparison.Ordinal);
            Assert.Contains(EditorMetadataTestSource.TitlePersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorMetadataTestSource.DurationPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorMetadataTestSource.AuthorPersistenceLine, persistedText, StringComparison.Ordinal);
            Assert.Contains(EditorMetadataTestSource.VersionPersistenceLine, persistedText, StringComparison.Ordinal);
        });
    }

    [Test]
    public void EditorPage_BlankMetadataTitleFallsBackToUntitledScript()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var source = cut.FindByTestId(UiTestIds.Editor.SourceInput);
            Assert.Contains(AppTestData.Editor.BodyHeading, source.GetAttribute("value"));
        });

        cut.FindByTestId(UiTestIds.Editor.Title).Change(EditorMetadataTestSource.BlankTitle);

        cut.WaitForAssertion(() =>
        {
            var metadata = _harness.Session.State.CompiledScript?.Metadata;
            var persistedText = _harness.Session.State.Text;

            Assert.NotNull(metadata);
            Assert.Equal(ScriptWorkspaceState.UntitledScriptTitle, metadata!["title"]);
            Assert.Equal(ScriptWorkspaceState.UntitledScriptTitle, _harness.Session.State.Title);
            Assert.Contains(EditorMetadataTestSource.UntitledTitlePersistenceLine, persistedText, StringComparison.Ordinal);
        });
    }

    [Test]
    public void EditorPage_MetadataRailToggleCollapsesAndExpands()
    {
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppTestData.Routes.EditorDemo);
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var toggle = cut.FindByTestId(UiTestIds.Editor.MetadataRailToggle);
            var rail = cut.FindByTestId(UiTestIds.Editor.MetadataRail);

            Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
            Assert.Equal(EditorMetadataTestSource.RightSidebarIcon, toggle.GetAttribute(EditorMetadataTestSource.SidebarIconAttribute));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.CreatedIcon));
            Assert.Equal(EditorMetadataTestSource.FalseText, rail.GetAttribute("data-collapsed"));
        });

        cut.FindByTestId(UiTestIds.Editor.MetadataRailToggle).Click();

        cut.WaitForAssertion(() =>
        {
            var toggle = cut.FindByTestId(UiTestIds.Editor.MetadataRailToggle);
            var rail = cut.FindByTestId(UiTestIds.Editor.MetadataRail);

            Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
            Assert.Equal(EditorMetadataTestSource.RightSidebarIcon, toggle.GetAttribute(EditorMetadataTestSource.SidebarIconAttribute));
            Assert.Equal(EditorMetadataTestSource.TrueText, rail.GetAttribute("data-collapsed"));
            Assert.True(cut.FindByTestId(UiTestIds.Editor.MetadataRailBody).HasAttribute("hidden"));
        });

        cut.FindByTestId(UiTestIds.Editor.MetadataRailToggle).Click();

        cut.WaitForAssertion(() =>
        {
            var toggle = cut.FindByTestId(UiTestIds.Editor.MetadataRailToggle);
            var rail = cut.FindByTestId(UiTestIds.Editor.MetadataRail);

            Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
            Assert.Equal(EditorMetadataTestSource.RightSidebarIcon, toggle.GetAttribute(EditorMetadataTestSource.SidebarIconAttribute));
            Assert.Equal(EditorMetadataTestSource.FalseText, rail.GetAttribute("data-collapsed"));
            Assert.False(cut.FindByTestId(UiTestIds.Editor.MetadataRailBody).HasAttribute("hidden"));
        });
    }

    [Test]
    public async Task EditorPage_LoadUsesSessionTitle_WhenPersistedFrontMatterTitleDiffers()
    {
        var document = await _harness.Repository.SaveAsync(
            EditorMetadataTestSource.ImportedFileStemTitle,
            EditorMetadataTestSource.ConflictingImportedDocument,
            EditorMetadataTestSource.ImportedDocumentName);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo(AppRoutes.EditorWithId(document.Id));
        var cut = Render<EditorPage>();

        cut.WaitForAssertion(() =>
        {
            var visibleSource = cut.FindByTestId(UiTestIds.Editor.SourceInput).GetAttribute("value") ?? string.Empty;
            var shellState = Services.GetRequiredService<AppShellService>().State;

            Assert.Equal(
                EditorMetadataTestSource.ImportedFileStemTitle,
                cut.FindByTestId(UiTestIds.Editor.Title).GetAttribute("value"));
            Assert.Equal(EditorMetadataTestSource.ImportedFileStemTitle, shellState.Title);
            Assert.Contains(EditorMetadataTestSource.ImportedHeading, visibleSource, StringComparison.Ordinal);
            Assert.DoesNotContain(EditorMetadataTestSource.TitleField, visibleSource, StringComparison.Ordinal);
        });
    }

    private static class EditorMetadataTestSource
    {
        public const string AuthorField = "author:";
        public const string AuthorPersistenceLine = "author: \"Test Speaker\"";
        public const string BaseWpm210 = "210";
        public const string BlankTitle = "   ";
        public const string DurationField = "duration:";
        public const string DurationPersistenceLine = "duration: \"12:34\"";
        public const string FalseText = "false";
        public const string ProfileRsvp = "RSVP";
        public const string RetitledScript = "Renamed Product Launch";
        public const string RightSidebarIcon = "right";
        public const string SidebarIconAttribute = "data-sidebar-icon";
        public const string ImportedDocumentName = "Imported Design Review From File Name With A Long Header Title That Should Clamp Cleanly In Editor.tps.md";
        public const string ImportedFileStemTitle = "Imported Design Review From File Name With A Long Header Title That Should Clamp Cleanly In Editor";
        public const string ImportedHeading = "Converted heading should stay inside the editor body instead of becoming the shell title";
        public const string TitleField = "title:";
        public const string TitlePersistenceLine = "title: \"Renamed Product Launch\"";
        public const string TrueText = "true";
        public const string UntitledTitlePersistenceLine = "title: \"Untitled Script\"";
        public const string VersionField = "version:";
        public const string VersionPersistenceLine = "version: \"2.0\"";
        public const string ConflictingImportedDocument =
            """
            ---
            title: "Converted heading should stay inside the editor body instead of becoming the shell title"
            ---

            # Converted heading should stay inside the editor body instead of becoming the shell title

            MarkItDown should convert this DOCX paragraph into Markdown for the editor.
            """;
    }
}
