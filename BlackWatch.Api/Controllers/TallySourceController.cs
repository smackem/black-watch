using System;
using System.Net;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BlackWatch.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TallySourceController : Controller
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger<TallySourceController> _logger;
        private const string UserId = "0";

        public TallySourceController(IDataStore dataStore, ILogger<TallySourceController> logger)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TallySource>> GetById(string id)
        {
            var tallySource = await _dataStore.GetTallySourceAsync(UserId, id);
            // ReSharper disable once InvertIf
            if (tallySource == null)
            {
                _logger.LogWarning("tally source not found: {TallySourceId}", id);
                return NotFound();
            }

            return Ok(tallySource);
        }

        [HttpGet("all")]
        public async Task<TallySource[]> Index()
        {
            return await _dataStore.GetTallySourcesAsync(UserId);
        }

        [HttpPost]
        [ProducesResponseType(typeof(TallySource), 201)]
        public async Task<IActionResult> Create([FromBody] CreateTallySourceCommand command)
        {
            var id = await _dataStore.GenerateIdAsync();
            var tallySource = new TallySource(id, command.Code, 1, DateTimeOffset.UtcNow, command.Interval);
            await _dataStore.PutTallySourceAsync(UserId, tallySource);
            return CreatedAtAction(nameof(GetById), new { id = tallySource.Id }, tallySource);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(TallySource), 200)]
        public Task<IActionResult> Update(string id, [FromBody] CreateTallySourceCommand command)
        {
            return Task.FromResult((IActionResult) NotFound());
        }

        public record CreateTallySourceCommand(string Code, EvaluationInterval Interval);
    }
}