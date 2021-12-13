using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Cron;
using Cronos;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.Features.CronActions;

public class QuoteSnapshotRequestAction : CronAction
{
    private readonly IDataStore _dataStore;
    private readonly ILogger _logger;

    public QuoteSnapshotRequestAction(CronExpression cronExpr, IDataStore dataStore, ILogger logger)
        : base(cronExpr, "trigger download of quote snapshots")
    {
        _dataStore = dataStore;
        _logger = logger;
    }

    public override async Task<bool> ExecuteAsync()
    {
        var requestInfo = RequestInfo.DownloadQuoteSnapshots(ApiTags.Messari);
        _logger.LogInformation("queue request: download quote snapshots {RequestInfo}", requestInfo);
        await _dataStore.EnqueueRequestAsync(requestInfo);
        return true;
    }
}
