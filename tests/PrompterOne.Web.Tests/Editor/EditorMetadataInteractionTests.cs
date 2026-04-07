using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PrompterOne.Core.Models.Workspace;
using PrompterOne.Shared.Contracts;
using PrompterOne.Shared.Pages;
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

            Assert.NotNull(metadata);
            Assert.Equal(EditorMetadataTestSource.RetitledScript, metadata!["title"]);
            Assert.Equal(EditorMetadataTestSource.ProfileRsvp, metadata!["profile"]);
            Assert.Equal(EditorMetadataTestSource.BaseWpm210, metadata["base_wpm"]);
            Assert.Equal(AppTestData.Editor.DisplayDuration, metadata["duration"]);
            Assert.Equal(AppTestData.Editor.TestSpeaker, metadata["author"]);
            Assert.Equal(AppTestData.Editor.CreatedDate, metadata["created"]);
            Assert.Equal(AppTestData.Editor.Version, metadata["version"]);
            Assert.Equal(EditorMetadataTestSource.RetitledScript, _harness.Session.State.Title);
            Assert.Contains(EditorMetadataTestSource.WpmSummary, cut.Markup);
            Assert.Contains(AppTestData.Editor.DisplayDuration, cut.Markup);
            Assert.Contains(EditorMetadataTestSource.VersionSummary, cut.Markup);
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
            Assert.Equal(EditorMetadataTestSource.RightChevronDirection, toggle.GetAttribute(EditorMetadataTestSource.ChevronDirectionAttribute));
            Assert.NotNull(cut.FindByTestId(UiTestIds.Editor.CreatedIcon));
            Assert.Equal(EditorMetadataTestSource.FalseText, rail.GetAttribute("data-collapsed"));
        });

        cut.FindByTestId(UiTestIds.Editor.MetadataRailToggle).Click();

        cut.WaitForAssertion(() =>
        {
            var toggle = cut.FindByTestId(UiTestIds.Editor.MetadataRailToggle);
            var rail = cut.FindByTestId(UiTestIds.Editor.MetadataRail);

            Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
            Assert.Equal(EditorMetadataTestSource.LeftChevronDirection, toggle.GetAttribute(EditorMetadataTestSource.ChevronDirectionAttribute));
            Assert.Equal(EditorMetadataTestSource.TrueText, rail.GetAttribute("data-collapsed"));
            Assert.True(cut.FindByTestId(UiTestIds.Editor.MetadataRailBody).HasAttribute("hidden"));
        });

        cut.FindByTestId(UiTestIds.Editor.MetadataRailToggle).Click();

        cut.WaitForAssertion(() =>
        {
            var toggle = cut.FindByTestId(UiTestIds.Editor.MetadataRailToggle);
            var rail = cut.FindByTestId(UiTestIds.Editor.MetadataRail);

            Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
            Assert.Equal(EditorMetadataTestSource.RightChevronDirection, toggle.GetAttribute(EditorMetadataTestSource.ChevronDirectionAttribute));
            Assert.Equal(EditorMetadataTestSource.FalseText, rail.GetAttribute("data-collapsed"));
            Assert.False(cut.FindByTestId(UiTestIds.Editor.MetadataRailBody).HasAttribute("hidden"));
        });
    }

    private static class EditorMetadataTestSource
    {
        public const string AuthorField = "author:";
        public const string AuthorPersistenceLine = "author: \"Test Speaker\"";
        public const string BaseWpm210 = "210";
        public const string BlankTitle = "   ";
        public const string ChevronDirectionAttribute = "data-chevron-direction";
        public const string DurationField = "duration:";
        public const string DurationPersistenceLine = "duration: \"12:34\"";
        public const string FalseText = "false";
        public const string LeftChevronDirection = "left";
        public const string ProfileRsvp = "RSVP";
        public const string RetitledScript = "Renamed Product Launch";
        public const string RightChevronDirection = "right";
        public const string TitleField = "title:";
        public const string TitlePersistenceLine = "title: \"Renamed Product Launch\"";
        public const string TrueText = "true";
        public const string UntitledTitlePersistenceLine = "title: \"Untitled Script\"";
        public const string VersionField = "version:";
        public const string VersionPersistenceLine = "version: \"2.0\"";
        public const string VersionSummary = "TPS v2.0";
        public const string WpmSummary = "210 WPM";
    }
}
