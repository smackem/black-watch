using System;
using BlackWatch.Core.Contracts;

namespace BlackWatch.Daemon.RequestEngine;

/// <summary>
///     creates <see cref="Request" /> instances from <see cref="RequestInfo" />s retrieved from other services
/// </summary>
public interface IRequestFactory
{
    Request BuildRequest(RequestInfo requestInfo, IServiceProvider sp);
}
