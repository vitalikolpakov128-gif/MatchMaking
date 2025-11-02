using Confluent.Kafka;
using StackExchange.Redis;
using System.Text.Json;
using System;
using System.Threading;

var consumerConfig = new ConsumerConfig
{
    BootstrapServers = "matchmaking-kafka:9092", 
    GroupId = "matchmaking-worker-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
};


var redisConfig = new StackExchange.Redis.ConfigurationOptions
{
    EndPoints = { "matchmaking-redis:6379" },
    AbortOnConnectFail = false
};
var redis = ConnectionMultiplexer.Connect(redisConfig);
var db = redis.GetDatabase();

using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
consumer.Subscribe("match-requests");

Console.WriteLine("Worker started, waiting for messages...");

while (true)
{
    try
    {
        var result = consumer.Consume(CancellationToken.None);
        string userId = result.Message.Value;

        Console.WriteLine($"Received match request for user: {userId}");

        var match = new MatchResult(
            Guid.NewGuid().ToString(),
            new string[] { userId, "user2", "user3" }
        );

        db.StringSet(userId, JsonSerializer.Serialize(match));

        Console.WriteLine($"Match created for user {userId} with MatchId {match.MatchId}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

public record MatchResult(string MatchId, string[] UserIds);
