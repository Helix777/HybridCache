using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "RedisCachingInstance";
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = GenerateForecast(summaries);
    return forecast;
})
.WithName("GetWeatherForecast");


app.MapGet("/weather-memory-cache/{city}", (string city, IMemoryCache memoryCache) =>
{
    string cacheKey = $"weather_{city.ToLower()}";

    // SprawdŸ, czy dane s¹ w pamiêci podrêcznej
    if (memoryCache.TryGetValue(cacheKey, out WeatherForecast[] cachedWeather))
    {
        return Task.FromResult(Results.Ok(new
        {
            Source = "Cache",
            Weather = cachedWeather
        }));
    }

    var forecast = GenerateForecast(summaries);

    var cacheEntryOptions = new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    memoryCache.Set(cacheKey, forecast, cacheEntryOptions);

    return Task.FromResult(Results.Ok(new
    {
        Source = "API",
        Weather = forecast
    }));
})
 .WithName("GetWeatherForecast-MemoryCache");

app.MapGet("/weather-redis-cache/{city}", async (string city, IDistributedCache distributedCache) =>
{
    string cacheKey = $"weather_{city.ToLower()}";

    // SprawdŸ, czy dane s¹ w pamiêci podrêcznej Redis
    var cachedWeatherString = await distributedCache.GetStringAsync(cacheKey);

    if (!string.IsNullOrEmpty(cachedWeatherString))
    {
        var cachedWeather = JsonSerializer.Deserialize<WeatherForecast[]>(cachedWeatherString);
        return Results.Ok(new
        {
            Source = "Redis Cache",
            Weather = cachedWeather
        });
    }

    var forecast = GenerateForecast(summaries);

    var forecastJson = JsonSerializer.Serialize(forecast);

    var options = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    await distributedCache.SetStringAsync(cacheKey, forecastJson, options);

    return Results.Ok(new
    {
        Source = "API",
        Weather = forecast
    });
}).WithName("GetWeatherForecast-RedisCache");


app.Run();

static WeatherForecast[] GenerateForecast(string[] summaries)
{
    return Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
}


internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
