using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSingleton(new WebSocketServerManager("ws://0.0.0.0:8181"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Start WebSocket Server
var webSocketServerManager = app.Services.GetRequiredService<WebSocketServerManager>();
webSocketServerManager.Start();

app.Run(async (context) =>
{
    await context.Response.WriteAsync("WebSocket server is running.");
});

app.Run();