using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BlackWatch.Core.Contracts;
using BlackWatch.Core.Services;
using BlackWatch.Core.Util;
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
        private readonly TallyService _tallyService;

        private const string UserId = "0";
        private const string ResponseMimeType = "application/json";

        public TallySourceController(IDataStore dataStore, ILogger<TallySourceController> logger, TallyService tallyService)
        {
            _dataStore = dataStore;
            _logger = logger;
            _tallyService = tallyService;
        }

        public record PutTallySourceCommand(string Code, EvaluationInterval Interval);

        [HttpGet("{id}")]
        [Produces(ResponseMimeType)]
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

        [HttpGet("{id}/eval")]
        [ProducesResponseType(typeof(Tally), (int) HttpStatusCode.OK)]
        [Produces(ResponseMimeType)]
        public async Task<ActionResult<Tally>> Evaluate(string id)
        {
            var tally = await InternalEvaluateAsync(id);

            return tally != null
                ? Ok(tally)
                : NotFound();
        }

        [HttpPost("{id}/eval")]
        [ProducesResponseType(typeof(Tally), (int) HttpStatusCode.Created)]
        [Produces(ResponseMimeType)]
        public async Task<ActionResult<Tally>> EvaluateAndStoreTally(string id)
        {
            var tally = await InternalEvaluateAsync(id);
            if (tally == null)
            {
                return NotFound();
            }

            await _dataStore.PutTallyAsync(tally);
            _logger.LogInformation("tally stored: {Tally}", tally);
            return CreatedAtAction(nameof(GetTally), new { id, count = 1 }, tally);
        }

        [HttpGet("{id}/tally")]
        [ProducesResponseType(typeof(Tally), (int)HttpStatusCode.OK)]
        [Produces(ResponseMimeType)]
        public async Task<ActionResult<Tally[]>> GetTally(string id, [FromQuery] int count = 1)
        {
            var tallySource = await _dataStore.GetTallySourceAsync(UserId, id);
            // ReSharper disable once InvertIf
            if (tallySource == null)
            {
                _logger.LogWarning("tally source not found: {TallySourceId}", id);
                return NotFound();
            }

            var tallies = await _dataStore.GetTalliesAsync(id, count);
            return Ok(tallies);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyCollection<TallySource>), (int) HttpStatusCode.OK)]
        [Produces(ResponseMimeType)]
        public async Task<IReadOnlyCollection<TallySource>> Index()
        {
            return await _dataStore.GetTallySourcesAsync().ToList().ConfigureAwait(false);
        }

        [HttpPost]
        [ProducesResponseType(typeof(TallySource), (int) HttpStatusCode.Created)]
        [Produces(ResponseMimeType)]
        public async Task<IActionResult> Create([FromBody] PutTallySourceCommand command)
        {
            var id = await _dataStore.GenerateIdAsync();
            var (code, interval) = command;
            var tallySource = new TallySource(id, code, 1, DateTimeOffset.UtcNow, interval);
            await _dataStore.PutTallySourceAsync(UserId, tallySource);
            _logger.LogInformation("tally source created: user-{UserId}:{TallySourceId}", UserId, id);
            return CreatedAtAction(nameof(GetById), new { id = tallySource.Id }, tallySource);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(TallySource), (int) HttpStatusCode.OK)]
        [Produces(ResponseMimeType)]
        public async Task<IActionResult> Update(string id, [FromBody] PutTallySourceCommand command)
        {
            var tallySource = await _dataStore.GetTallySourceAsync(UserId, id);
            if (tallySource == null)
            {
                _logger.LogWarning("tally source not found: user-{UserId}:{TallySourceId}", UserId, id);
                return NotFound();
            }

            var (code, interval) = command;
            var modifiedTallySource = tallySource.Update(code, interval);
            await _dataStore.PutTallySourceAsync(UserId, modifiedTallySource);
            _logger.LogInformation("tally source updated: user-{UserId}:{TallySourceId}", UserId, id);
            return Ok(modifiedTallySource);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            // ReSharper disable once InvertIf
            if (await _dataStore.DeleteTallySourceAsync(UserId, id) == false)
            {
                _logger.LogWarning("tally source not found: user-{UserId}:{TallySourceId}", UserId, id);
                return NotFound();
            }

            _logger.LogInformation("tally source removed: user-{UserId}:{TallySourceId}", UserId, id);
            return NoContent();
        }

        private async Task<Tally?> InternalEvaluateAsync(string id)
        {
            var tallySource = await _dataStore.GetTallySourceAsync(UserId, id);
            // ReSharper disable once InvertIf
            if (tallySource == null)
            {
                _logger.LogWarning("tally source not found: {TallySourceId}", id);
                return null;
            }

            return await _tallyService.EvaluateAsync(tallySource);
        }
    }
}