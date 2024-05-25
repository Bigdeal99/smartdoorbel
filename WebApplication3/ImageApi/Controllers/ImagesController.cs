using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using System.Collections.Generic;
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

    [HttpGet]
    public async Task<IActionResult> GetImages()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
        var imageUrls = new List<string>();

        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            var uri = containerClient.GetBlobClient(blobItem.Name).Uri;
            imageUrls.Add(uri.ToString());
        }

        return Ok(imageUrls);
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
}