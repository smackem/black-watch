using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Daemon.Features.MessariApi;

public class MessariApiClientOptions
{
    [Range(1, 500)]
    public int QuoteLimit { get; set; } = 200;
}
