using System;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "KafkaStarter API", Version = "v1" });
});

builder.Services.AddSingleton<KafkaStarter.Api.Services.IKafkaProducerService, KafkaStarter.Api.Services.KafkaProducerService>();
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<KafkaStarter.Api.Services.IOpenAIService, KafkaStarter.Api.Services.OpenAIService>();
builder.Services.AddSingleton<KafkaStarter.Api.Services.AudioProcessingService>();

// CORS configuration for local development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KafkaStarter API v1");
    c.RoutePrefix = "";
});

// Configure WebSocket for TTS
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/audio")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var audioProcessingService = app.Services.GetRequiredService<KafkaStarter.Api.Services.AudioProcessingService>();
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await audioProcessingService.HandleTTSWebSocket(webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("Application starting... listening on http://localhost:5000");

app.Run();