using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;

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
        builder.WithOrigins("http://172.20.10.2:3000")
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
    endpoints.MapControllers();

    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("API is running.");
    });
});

app.Run();