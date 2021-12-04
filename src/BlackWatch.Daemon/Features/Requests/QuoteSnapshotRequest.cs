using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Daemon.Features.Messari;
using BlackWatch.Daemon.RequestEngine;

namespace BlackWatch.Daemon.Features.Requests
{
    public class QuoteSnapshotRequest : Request
    {
        private readonly QuoteSnapshotRequestInfo _info;
        private readonly IMessariApiClient _messari;
        private readonly IDataStore _dataStore;

        public QuoteSnapshotRequest(QuoteSnapshotRequestInfo info, IDataStore dataStore, IMessariApiClient messari)
            : base("download quote snapshots")
        {
            _info = info;
            _messari = messari;
            _dataStore = dataStore;
        }

        public override async Task<RequestResult> ExecuteAsync(RequestContext ctx)
        {
            var assets = await _messari.GetAssetsAsync(200);
            // TODO: create quote objects and store them in db
            return RequestResult.Ok;
        }
    }
}
