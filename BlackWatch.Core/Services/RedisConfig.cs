using System.ComponentModel.DataAnnotations;

namespace BlackWatch.Core.Services
{
    public class RedisConfig
    {
        [Required]
        public string ConnectionString { get; set; } = string.Empty;
    }
}
