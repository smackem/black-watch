using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace BlackWatch.Daemon.Features.Polygon
{
    public class PolygonApiClient : IPolygonApiClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        
        public PolygonApiClient(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }
    }
}
