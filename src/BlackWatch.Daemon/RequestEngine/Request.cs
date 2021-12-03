using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.RequestEngine
{
    /// <summary>
    /// a job executed by the <see cref="RequestRunner"/>
    /// </summary>
    public abstract class Request
    {
        private readonly string _moniker;

        protected Request(string moniker)
        {
            _moniker = moniker;
        }

        public abstract Task<RequestResult> ExecuteAsync(RequestContext ctx);

        public override string ToString()
        {
            return $"Job[{_moniker}]";
        }
    }

    /// <summary>
    /// a job that does nothing, only logs a warning. can be used to signal some misunderstanding...
    /// </summary>
    public class NopRequest : Request
    {
        private NopRequest() : base("nop") { }

        public static Request Instance { get; } = new NopRequest();

        public override Task<RequestResult> ExecuteAsync(RequestContext ctx)
        {
            ctx.Logger.LogWarning("executing nop job");
            return Task.FromResult(RequestResult.Ok);
        }
    }
}
