using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using Confluent.Kafka;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Text.Json;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class MatchController : ControllerBase
{
     private readonly IConnectionMultiplexer _redis; 

    public MatchController(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }
    
    private static readonly ConcurrentDictionary<string, MatchResult> _matches = new();

    [HttpPost("search")]
    public async Task<IActionResult> SearchMatch([FromQuery] string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return BadRequest();

        var config = new ProducerConfig
        {
            BootstrapServers = "localhost:9092"
        };

        using var producer = new ProducerBuilder<Null, string>(config).Build();

        
        await producer.ProduceAsync("match-requests", new Message<Null, string> { Value = userId });
        producer.Flush(TimeSpan.FromSeconds(5)); 
        return NoContent(); // 204
    }

    [HttpGet("match-info")]
public IActionResult GetMatchInfo([FromQuery] string userId)
{
    if (string.IsNullOrEmpty(userId))
        return BadRequest();

   
    var db = _redis.GetDatabase();
    var matchJson = db.StringGet(userId);

    if (matchJson.IsNullOrEmpty)
        return NotFound();

    var match = JsonSerializer.Deserialize<MatchResult>(matchJson);
    return Ok(match);
}

}

public record MatchResult(string MatchId, string[] UserIds);
