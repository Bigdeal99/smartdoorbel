using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:3000") // Adjust to match your frontend URL
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5164);
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
    endpoints.MapGet("/api/images", async context =>
    {
        var blobServiceClient = context.RequestServices.GetRequiredService<BlobServiceClient>();
        var containerClient = blobServiceClient.GetBlobContainerClient("iot-10sec-video");
        var imageUrls = new List<string>();

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            var uri = containerClient.GetBlobClient(blobItem.Name).Uri;
            imageUrls.Add(uri.ToString());
        }

        await context.Response.WriteAsJsonAsync(imageUrls);
    });

    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("API is running.");
    });
});

app.Run();