using System;
using Microsoft.Extensions.Logging;

namespace BlackWatch.WebApp.Services;

public class UiService
{
    private readonly ILogger<UiService> _logger;

    public UiService(ILogger<UiService> logger)
    {
        _logger = logger;
    }
    
    public event EventHandler? Refresh;

    internal void RaiseRefresh()
    {
        _logger.LogInformation("REFRESH");
        Refresh?.Invoke(this, EventArgs.Empty);
    }
}
