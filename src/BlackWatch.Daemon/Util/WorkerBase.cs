using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Util;

public abstract class WorkerBase : BackgroundService
{
    private readonly ILogger _logger;

    protected WorkerBase(ILogger logger)
    {
        _logger = logger;
    }

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("worker {WorkerType} started", GetType());

        try
        {
            await ExecuteOverrideAsync(stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "error caught at worker top level");
        }

        _logger.LogInformation("worker {WorkerType} finished", GetType());
    }

    protected abstract Task ExecuteOverrideAsync(CancellationToken stoppingToken);
}
