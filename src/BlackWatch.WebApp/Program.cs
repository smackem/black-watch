using System;
using System.Threading.Tasks;
using BlackWatch.WebApp.Features.Api;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BlackWatch.WebApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddLogging();
            builder.Services.AddHttpClient<IApiClient, ApiClient>(http =>
            {
                http.BaseAddress = new Uri(builder.Configuration["Api:Uri"]);
            });

            await builder.Build().RunAsync();
        }
    }
}
