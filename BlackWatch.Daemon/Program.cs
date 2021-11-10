using System;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Daemon.Features.Polygon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BlackWatch.Daemon
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    services.AddOptions<JobExecutionConfig>()
                        .Bind(ctx.Configuration.GetSection("JobExecution"))
                        .ValidateDataAnnotations();
                    services.AddOptions<RedisConfig>()
                        .Bind(ctx.Configuration.GetSection("Redis"))
                        .ValidateDataAnnotations();

                    services.AddHostedService<JobExecutionWorker>();
                    services.AddHostedService<JobSchedulingWorker>();
                    services.AddHttpClient<IPolygonApiClient, PolygonApiClient>(http =>
                    {
                        http.BaseAddress = new Uri(ctx.Configuration["Polygon:BaseAddress"]);
                    });
                    services.AddSingleton<IDataStore, RedisDataStore>();
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
