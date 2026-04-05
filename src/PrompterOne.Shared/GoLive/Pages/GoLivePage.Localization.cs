using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using PrompterOne.Shared.Localization;

namespace PrompterOne.Shared.Pages;

public partial class GoLivePage
{
    [Inject] private IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

    private string Text(string key) => Localizer[key];
}
