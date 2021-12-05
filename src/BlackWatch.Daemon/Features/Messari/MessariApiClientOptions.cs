using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Daemon.Features.Messari
{
    public class MessariApiClientOptions
    {
        [Range(1, 500)]
        public int QuoteLimit { get; set; } = 200;
    }
}
