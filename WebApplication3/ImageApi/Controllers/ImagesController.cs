using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImageApi.Dtos;

namespace ImageApi.Controllers
{
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
            var images = new List<ImageDto>();

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var uri = containerClient.GetBlobClient(blobItem.Name).Uri;
                images.Add(new ImageDto { Url = uri.ToString(), FileName = blobItem.Name });
            }

            var response = new ResponseDto<List<ImageDto>>
            {
                Success = true,
                Message = "Images retrieved successfully.",
                Data = images
            };

            return Ok(response);
        }

        [HttpDelete("{fileName}")]
        public async Task<IActionResult> DeleteImage(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
            var blobClient = containerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                await blobClient.DeleteAsync();
                var response = new ResponseDto<string>
                {
                    Success = true,
                    Message = "Image deleted successfully.",
                    Data = fileName
                };

                return Ok(response);
            }

            var errorResponse = new ResponseDto<string>
            {
                Success = false,
                Message = "Image not found.",
                Data = fileName
            };

            return NotFound(errorResponse);
        }
    }
}
