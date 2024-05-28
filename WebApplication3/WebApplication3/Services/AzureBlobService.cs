using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MyProject.Services
{
    public interface IAzureBlobService
    {
        Task<List<ImageDto>> GetImagesAsync();
        Task<bool> DeleteImageAsync(string fileName);
        Task<string> UploadImageAsync(Stream imageStream, string timestamp);
    }

    public class AzureBlobService : IAzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public AzureBlobService(IConfiguration configuration)
        {
            var storageAccountName = configuration["Azure:StorageAccountName"];
            var sasToken = configuration["Azure:SasToken"];
            var uri = new Uri($"https://{storageAccountName}.blob.core.windows.net?{sasToken}");
            _blobServiceClient = new BlobServiceClient(uri);
        }

        public async Task<List<ImageDto>> GetImagesAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
            var images = new List<ImageDto>();

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                var uri = containerClient.GetBlobClient(blobItem.Name).Uri;
                images.Add(new ImageDto { Url = uri.ToString(), FileName = blobItem.Name });
            }

            return images;
        }

        public async Task<bool> DeleteImageAsync(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");
            var blobClient = containerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                await blobClient.DeleteAsync();
                return true;
            }

            return false;
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string timestamp)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("iot-10sec-video");

            using (var stream = new MemoryStream())
            {
                await imageStream.CopyToAsync(stream);
                stream.Position = 0;

                var fileName = $"{timestamp}.jpg";
                var blobClient = containerClient.GetBlobClient(fileName);

                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "image/jpeg" });

                return fileName;
            }
        }
    }

    public class ImageDto
    {
        public string Url { get; set; }
        public string FileName { get; set; }
    }
}
