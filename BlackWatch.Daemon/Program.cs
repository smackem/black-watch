using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackWatch.Core;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Daemon.Features.Polygon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<IDataStore>(sp =>
                        new RedisDataStore(
                            ctx.Configuration["Redis:ConnectionString"],
                            sp.GetService<ILogger<RedisDataStore>>()!));
                    services.AddHttpClient<IPolygonApiClient, PolygonApiClient>(http =>
                    {
                        http.BaseAddress = new Uri("https://api.polygon.io/v2/");
                    });
                });
    }
}
