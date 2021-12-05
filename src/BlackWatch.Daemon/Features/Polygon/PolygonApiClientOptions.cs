using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Daemon.Features.Polygon
{
    public class PolygonApiClientOptions
    {
        [Required]
        public string? BaseAddress { get; set; }

        [Required]
        public string? ApiKey { get; set; }
    }
}