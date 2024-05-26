using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

[Route("api/[controller]")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<ImagesController> _logger;

    public ImagesController(BlobServiceClient blobServiceClient, ILogger<ImagesController> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a list of images from Azure Blob Storage.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetImages()
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
        var imageUrls = new List<string>();

        try
        {
            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var uri = containerClient.GetBlobClient(blobItem.Name).Uri;
                imageUrls.Add(uri.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving images");
            return StatusCode((int)HttpStatusCode.InternalServerError, new ResponseDto<string>
            {
                Success = false,
                Message = "An error occurred while retrieving images."
            });
        }

        return Ok(new ResponseDto<List<string>>
        {
            Success = true,
            Data = imageUrls,
            Message = "Images retrieved successfully."
        });
    }

    /// <summary>
    /// Deletes an image from Azure Blob Storage.
    /// </summary>
    /// <param name="fileName">The name of the file to delete.</param>
    [HttpDelete("{fileName}")]
    public async Task<IActionResult> DeleteImage(string fileName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
        var blobClient = containerClient.GetBlobClient(fileName);

        try
        {
            if (await blobClient.ExistsAsync())
            {
                await blobClient.DeleteAsync();
                return Ok(new ResponseDto<string>
                {
                    Success = true,
                    Message = "Image deleted successfully."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image");
            return StatusCode((int)HttpStatusCode.InternalServerError, new ResponseDto<string>
            {
                Success = false,
                Message = "An error occurred while deleting the image."
            });
        }

        return NotFound(new ResponseDto<string>
        {
            Success = false,
            Message = "Image not found."
        });
    }
}
