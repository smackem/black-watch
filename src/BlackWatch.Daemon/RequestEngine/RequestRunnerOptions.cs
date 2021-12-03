using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Daemon.RequestEngine
{
    public class RequestRunnerOptions
    {
        [Range(1, int.MaxValue)]
        public int MaxRequestsPerMinute { get; set; } = int.MaxValue;
    }
}
