using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlackWatch.WebApp.Features.Api
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient http, ILogger<ApiClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<TallySource>> GetTallySourcesAsync()
        {
            var tallySources = await _http.GetFromJsonAsync<TallySource[]>("tallysource");
            return tallySources!;
        }

        public async Task<TallySource> GetTallySourceAsync(string id)
        {
            var tallySource = await _http.GetFromJsonAsync<TallySource>($"tallysource/{id}");
            return tallySource!;
        }

        public async Task<TallySource> CreateTallySourceAsync(PutTallySourceCommand command)
        {
            var response = await _http.PostAsJsonAsync("tallysource", command);
            var tallySource = await response.Content.ReadFromJsonAsync<TallySource>();
            return tallySource!;
        }

        public async Task<TallySource> UpdateTallySourceAsync(string id, PutTallySourceCommand command)
        {
            var response = await _http.PutAsJsonAsync($"tallysource/{id}", command);
            var tallySource = await response.Content.ReadFromJsonAsync<TallySource>();
            return tallySource!;
        }

        public Task DeleteTallySourceAsync(string id)
        {
            return _http.DeleteAsync($"tallysource/{id}");
        }

        public Task<Tally> EvaluateTallySourceAsync(string id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Tally> EvaluateTallySourceAndStoreTallyAsync(string id)
        {
            var response = await _http.PostAsync($"tallysource/{id}/eval", new StringContent(string.Empty));
            var tally = await response.Content.ReadFromJsonAsync<Tally>();
            return tally!;
        }

        public Task<IReadOnlyCollection<Tally>> GetTallyAsync(string tallySourceId, int count)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IReadOnlyCollection<Tally>> GetTalliesAsync(int count)
        {
            var tallies = await _http.GetFromJsonAsync<Tally[]>($"tally?count={count}");
            return tallies!;
        }
    }
}