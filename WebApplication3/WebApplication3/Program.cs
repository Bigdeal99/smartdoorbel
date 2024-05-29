using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyProject.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<IAzureBlobService, AzureBlobService>();
var port = Environment.GetEnvironmentVariable("PORT") ?? "8181";
//builder.WebHost.UseUrls("http://*:9999");
builder.Services.AddSingleton<WebSocketServerManager>(sp =>
    new WebSocketServerManager("ws://0.0.0.0:"+port, builder.Configuration.GetConnectionString("DefaultConnection")));

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
    serverOptions.ListenAnyIP(int.Parse(port ?? "5164"));
    serverOptions.ListenAnyIP(7026, listenOptions => listenOptions.UseHttps());
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
var webSocketServerManager = app.Services.GetRequiredService<WebSocketServerManager>();
webSocketServerManager.Start();

app.Run();