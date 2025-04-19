var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "KafkaStarter API", Version = "v1" });
});

// Register Kafka producer service as a singleton
builder.Services.AddSingleton<KafkaStarter.Api.Services.IKafkaProducerService, KafkaStarter.Api.Services.KafkaProducerService>();

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

// Explicitly set URL to bind to port 5000
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KafkaStarter API v1");
    c.RoutePrefix = ""; // Set Swagger UI at root
});

// Comment out HTTPS redirection
// app.UseHttpsRedirection();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("Application starting... listening on http://localhost:5000");

app.Run();