using Microsoft.AspNetCore.Mvc;
using MyProject.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly IAzureBlobService _blobService;

        public ImagesController(IAzureBlobService blobService)
        {
            _blobService = blobService;
        }

        public class ImageDto
        {
            public string Url { get; set; }
            public string FileName { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetImages()
        {
            var images = await _blobService.GetImagesAsync();
            return Ok(images);
        }

        [HttpDelete("{fileName}")]
        public async Task<IActionResult> DeleteImage(string fileName)
        {
            var result = await _blobService.DeleteImageAsync(fileName);
            if (result)
            {
                return NoContent();
            }

            return NotFound();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromQuery] string timestamp)
        {
            var fileName = await _blobService.UploadImageAsync(Request.Body, timestamp);
            return Ok(new { fileName });
        }
    }
}