using System;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Daemon.Cron;
using BlackWatch.Daemon.Features.CronActions;
using BlackWatch.Daemon.Features.Jobs;
using BlackWatch.Daemon.Features.Polygon;
using BlackWatch.Daemon.JobEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<IDataStore, RedisDataStore>();
                    services.AddOptions<RedisOptions>()
                        .Bind(ctx.Configuration.GetSection("Redis"))
                        .ValidateDataAnnotations();

                    services.AddHttpClient<IPolygonApiClient, PolygonApiClient>(http =>
                    {
                        http.BaseAddress = new Uri(ctx.Configuration["Polygon:BaseAddress"]);
                    });

                    services.AddHostedService<JobExecutor>();
                    services.AddSingleton<IJobFactory, JobFactory>();
                    services.AddOptions<JobExecutorOptions>()
                        .Bind(ctx.Configuration.GetSection("JobExecution"))
                        .ValidateDataAnnotations();

                    services.AddHostedService<CronActionRunner>();
                    services.AddSingleton<ICronActionSupplier, CronActionSupplier>();
                    services.AddOptions<SchedulerOptions>()
                        .Bind(ctx.Configuration.GetSection("Scheduling"))
                        .ValidateDataAnnotations();
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
