using System;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Daemon.Cron;
using BlackWatch.Daemon.Features.CronActions;
using BlackWatch.Daemon.Features.Messari;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.Features.Requests;
using BlackWatch.Daemon.RequestEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace BlackWatch.Daemon
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
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
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<IDataStore, RedisDataStore>();
                    services.AddOptions<RedisOptions>()
                        .Bind(ctx.Configuration.GetSection("Redis"))
                        .ValidateDataAnnotations();

                    services.AddHttpClient<IMessariApiClient, MessariApiClient>(http =>
                    {
                        http.BaseAddress = new Uri(ctx.Configuration["Messari:BaseAddress"]);
                    });
                    services.AddOptions<MessariApiClientOptions>()
                        .Bind(ctx.Configuration.GetSection("Messari"))
                        .ValidateDataAnnotations();

                    services.AddHostedService<MessariRequestRunner>();
                    services.AddOptions<MessariRequestRunnerOptions>()
                        .Bind(ctx.Configuration.GetSection("Messari"))
                        .ValidateDataAnnotations();

                    services.AddHttpClient<IPolygonApiClient, PolygonApiClient>(http =>
                    {
                        http.BaseAddress = new Uri(ctx.Configuration["Polygon:BaseAddress"]);
                    });
                    services.AddOptions<PolygonApiClientOptions>()
                        .Bind(ctx.Configuration.GetSection("Polygon"))
                        .ValidateDataAnnotations();

                    services.AddHostedService<PolygonRequestRunner>();
                    services.AddOptions<PolygonRequestRunnerOptions>()
                        .Bind(ctx.Configuration.GetSection("Polygon"))
                        .ValidateDataAnnotations();

                    services.AddSingleton<IRequestFactory, RequestFactory>();

                    services.AddHostedService<CronActionRunner>();
                    services.AddSingleton<ICronActionSupplier, CronActionSupplier>();
                    services.AddOptions<SchedulerOptions>()
                        .Bind(ctx.Configuration.GetSection("Scheduling"))
                        .ValidateDataAnnotations();

                    services.AddTransient<TallyService>();
                });

        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine($"!!! unhandled exception: ${e.Exception}");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine($"!!! unhandled exception: ${e.ExceptionObject}");
        }
    }
}
