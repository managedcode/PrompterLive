using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PrompterLive.App;
using PrompterLive.App.Services;
using PrompterLive.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddPrompterLiveShared();

await builder.Build().RunAsync();
