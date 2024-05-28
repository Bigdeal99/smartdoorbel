using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly BlobServiceClient _blobServiceClient;

    public ImagesController(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
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
    public async Task<IActionResult> UploadImage([FromQuery] string timestamp)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");

        using (var stream = new MemoryStream())
        {
            await Request.Body.CopyToAsync(stream);
            stream.Position = 0;

            var fileName = $"{timestamp}.jpg";
            var blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "image/jpeg" });

            return Ok(new { fileName });
        }
    }
}
