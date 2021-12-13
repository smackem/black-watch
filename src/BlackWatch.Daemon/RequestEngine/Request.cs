using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Daemon.RequestEngine;

/// <summary>
///     a request executed by the <see cref="RequestRunner" />
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
        return $"Request[{_moniker}]";
    }
}

/// <summary>
///     a request that does nothing, only logs a warning. can be used to signal some misunderstanding...
/// </summary>
public class NopRequest : Request
{
    private NopRequest() : base("nop") {}

    public static Request Instance { get; } = new NopRequest();

    public override Task<RequestResult> ExecuteAsync(RequestContext ctx)
    {
        ctx.Logger.LogWarning("executing nop request");
        return Task.FromResult(RequestResult.Ok);
    }
}
