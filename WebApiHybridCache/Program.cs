using Microsoft.Extensions.Caching.Hybrid;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "WeatherForecastRedis";
});
#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions()
    {
        Expiration = TimeSpan.FromSeconds(60),
        LocalCacheExpiration = TimeSpan.FromSeconds(5)
    };
});
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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

app.MapGet("/weather-hybrid-cache/{city}", async (string city, HybridCache hybridCache, CancellationToken ct) =>
{
    string cacheKey = $"weather_{city.ToLower()}";

    var cachedValue = await hybridCache.GetOrCreateAsync(cacheKey, cancel => GenerateForecast(summaries), cancellationToken: ct);

    return Results.Ok(new
    {
       Weather = cachedValue
    });
})
.WithName("GetWeatherForecast-HybridCache");

app.Run();

async ValueTask<WeatherForecast[]> GenerateForecast(string[] summaries)
{
    await Task.Delay(3000);
    return Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateTime.Now.ToString("HH:mm:ss"),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )).ToArray(); ;
}


internal record WeatherForecast(string Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


