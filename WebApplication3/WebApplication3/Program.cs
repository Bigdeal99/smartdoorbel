using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyProject.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<IAzureBlobService, AzureBlobService>();
builder.Services.AddSingleton<WebSocketServerManager>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("DefaultConnection");
    var wsLocation = "ws://0.0.0.0:8181";
    return new WebSocketServerManager(wsLocation, connectionString);
});

// Add controllers
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    var port = Environment.GetEnvironmentVariable("PORT");
    if (string.IsNullOrEmpty(port))
    {
        throw new InvalidOperationException("The PORT environment variable is not set.");
    }
    serverOptions.ListenAnyIP(int.Parse(port));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("API and WebSocket server are running.");
    });
});

// Start WebSocket Server
try
{
    var webSocketServerManager = app.Services.GetRequiredService<WebSocketServerManager>();
    webSocketServerManager.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Error starting WebSocketServerManager: {ex.Message}");
    throw;
}

app.Run();