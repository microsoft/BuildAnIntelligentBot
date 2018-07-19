using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.AspNetCore.Http;
using ChatBot.Models;
using System.Net.Http;

namespace ChatBot.Services
{
    public class BlobStorageService
    {
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", Justification = "Avoiding Improper Instantiation antipattern : https://docs.microsoft.com/en-us/azure/architecture/antipatterns/improper-instantiation/")]
        private static readonly HttpClient Client = new HttpClient();
        private static volatile CloudBlobClient _blobClient;
        private static CloudBlobContainer _container;
        private readonly string _storageConnectionString;

        public BlobStorageService(string storageConnectionString)
        {
            _storageConnectionString = storageConnectionString;
        }

        public async Task<byte[]> FetchAudio(string blobName)
        {
            var blockBlob = (await GetContainer()).GetBlockBlobReference(blobName + ".wav");

            // Read content
            using (var ms = new MemoryStream())
            {
                await blockBlob.DownloadToStreamAsync(ms);
                return ms.ToArray();
            }
        }

        public async Task<string> StoreAudio(string id, byte[] audioContent)
        {
            var blob = (await GetContainer()).GetBlockBlobReference($"{id}.wav");
            await blob.UploadFromByteArrayAsync(audioContent, 0, audioContent.Length);
            return blob.Uri.AbsoluteUri;
        }

        public async Task<string> IsAudioAvailable(string id)
        {
            var blob = (await GetContainer()).GetBlockBlobReference($"{id}.wav");
            return await blob.ExistsAsync() ? blob.Uri.AbsoluteUri : null;
        }

        private static async Task CreateContainer()
        {
            _container = _blobClient.GetContainerReference(BotConstants.TextToSpeechAzureContainer);
            await _container.CreateIfNotExistsAsync();
            await _container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
        }

        /// <summary>
        /// Gets and creates the container if it doesn't exist
        /// </summary>
        /// <returns>The containe</returns>
        private async Task<CloudBlobContainer> GetContainer()
        {
            if (_blobClient == null)
            {
                var cloudStorageAccount = CloudStorageAccount.Parse(_storageConnectionString);
                _blobClient = cloudStorageAccount.CreateCloudBlobClient();

                await CreateContainer();
            }

            return _container;
        }
    }
}