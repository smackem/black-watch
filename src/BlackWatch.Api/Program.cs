using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace BlackWatch.Api;

public static class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging((_, builder) =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ssZ ";
                    options.SingleLine = true;
                    options.UseUtcTimestamp = true;
                    options.ColorBehavior = LoggerColorBehavior.Disabled;
                    options.IncludeScopes = true;
                });
            })
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
}
