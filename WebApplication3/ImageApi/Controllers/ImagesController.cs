using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using MQTTnet;
using MQTTnet.Client;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;


[Route("api/[controller]")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IMqttClient _mqttClient;

    public ImagesController(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
        
        // Configure MQTT client options
        var options = new MqttClientOptionsBuilder()
            .WithClientId("API_Client")
            .WithTcpServer("mqtt.flespi.io", 1883)
            .WithCredentials("zomufnJ4kljspMzkeTjAf38E9gfaMAp7Qvd1u3QboArEtJnUTrfkOYke86fYSeu8", "")
            .WithCleanSession()
            .Build();

        // Create and connect MQTT client
        _mqttClient = new MqttFactory().CreateMqttClient();
        _mqttClient.ConnectAsync(options).Wait();
    }

    public class ImageDto
    {
        public string Url { get; set; }
        public string FileName { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> GetImages()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
        var images = new List<ImageDto>();

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            var uri = containerClient.GetBlobClient(blobItem.Name).Uri;
            images.Add(new ImageDto { Url = uri.ToString(), FileName = blobItem.Name });
        }

        return Ok(images);
    }

    [HttpDelete("{fileName}")]
    public async Task<IActionResult> DeleteImage(string fileName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
        var blobClient = containerClient.GetBlobClient(fileName);

        if (await blobClient.ExistsAsync())
        {
            await blobClient.DeleteAsync();
            return NoContent();
        }

        return NotFound();
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage([FromBody] byte[] imageData)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
        var blobClient = containerClient.GetBlobClient(fileName);

        await blobClient.UploadAsync(new BinaryData(imageData));

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("iot/notification")
            .WithPayload("Image uploaded")
            .Build();

        await _mqttClient.PublishAsync(message);

        await SendIFTTTNotification();

        return Ok(new { Url = blobClient.Uri.ToString(), FileName = fileName });
    }

    private async Task SendIFTTTNotification()
    {
        using (var client = new HttpClient())
        {
            var requestUri = "https://maker.ifttt.com/trigger/button_pressed/with/key/nYTXhTPJg0LkIgtxP45lX8OmITfAhV_3zEN7LGMCTEz";
            var response = await client.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error sending IFTTT notification: {response.StatusCode}");
            }
        }
    }
}
