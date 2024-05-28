using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System.Text;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure Blob Storage client
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var storageAccountName = configuration["Azure:StorageAccountName"];
    var sasToken = configuration["Azure:SasToken"];
    var uri = new Uri($"https://{storageAccountName}.blob.core.windows.net?{sasToken}");
    return new BlobServiceClient(uri);
});

// Add services to the container
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:3000") // Adjust to match your frontend URL
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add IFTTT Notification Service
builder.Services.AddHttpClient<IFTTTNotificationService>(client =>
{
    client.BaseAddress = new Uri("https://maker.ifttt.com");
});
builder.Services.AddSingleton<IFTTTNotificationService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var privateKey = configuration["IFTTT:PrivateKey"];
    return new IFTTTNotificationService(httpClient, "maker.ifttt.com", privateKey);
});

// Add MQTT client
builder.Services.AddSingleton<IMqttClient>(sp =>
{
    var factory = new MqttFactory();
    var mqttClient = factory.CreateMqttClient();

    var options = new MqttClientOptionsBuilder()
        .WithClientId("DotNetClient")
        .WithTcpServer("mqtt.flespi.io", 1883)
        .WithCredentials("YOUR_CLIENTSIDE_FLESPI_TOKEN", null)
        .WithCleanSession()
        .Build();

    mqttClient.UseConnectedHandler(async e =>
    {
        Console.WriteLine("Connected to MQTT Broker");
        await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("outside/notifications").Build());
        await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("camera/control").Build());
    });

    mqttClient.UseApplicationMessageReceivedHandler(async e =>
    {
        var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
        Console.WriteLine($"Received message: {message}");
        var notificationService = sp.GetRequiredService<IFTTTNotificationService>();

        if (e.ApplicationMessage.Topic == "outside/notifications")
        {
            // Handle notification message
            await notificationService.SendNotificationAsync("button_pressed");
        }
    });

    mqttClient.ConnectAsync(options).Wait();

    return mqttClient;
});

// Add WebSocket server manager
builder.Services.AddSingleton<WebSocketServerManager>(new WebSocketServerManager("ws://0.0.0.0:8181"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Start WebSocket Server
var webSocketServerManager = app.Services.GetRequiredService<WebSocketServerManager>();
webSocketServerManager.Start();

app.UseRouting();
app.UseCors();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("API is running.");
    });
});

app.Run();
