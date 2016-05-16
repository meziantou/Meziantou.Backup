using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Meziantou.OneDrive
{
    // https://dev.onedrive.com/resources/item.htm
    public class OneDriveItem
    {
        private readonly OneDriveClient _client;

        public OneDriveItem(OneDriveClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            _client = client;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string ETag { get; set; }
        public string CTag { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime LastModifiedDateTime { get; set; }
        public long Size { get; set; }
        public string WebUrl { get; set; }
        public string WebDavUrl { get; set; }
        public string Description { get; set; }
        public IdentitySet CreatedBy { get; set; }
        public IdentitySet LastModifiedBy { get; set; }
        public Audio Audio { get; set; }
        public FileSystemInfo FileSystemInfo { get; set; }
        public Folder Folder { get; set; }
        public File File { get; set; }
        public Image Image { get; set; }
        public Location Location { get; set; }
        public Package Package { get; set; }
        public ItemReference ItemReference { get; set; }
        public Photo Photo { get; set; }
        public RemoveItem RemoteItem { get; set; }
        public SearchResult SearchResult { get; set; }
        public Shared Shared { get; set; }
        public SharepointIds SharepointIds { get; set; }
        public SpecialFolder SpecialFolder { get; set; }
        public Video Video { get; set; }
        public ItemReference ParentReference { get; set; }

        [JsonProperty("@content.downloadUrl")]
        public string DownloadUrl { get; set; }

        public Task DeleteAsync(CancellationToken ct)
        {
            return _client.DeleteItemAsync(this, ct);
        }

        public Task<IReadOnlyList<OneDriveItem>> GetChildrenAsync(CancellationToken ct)
        {
            return _client.GetChildrenAsync(this, ct);
        }

        public Task<ThumbnailSet> GetThumbnailsAsync(CancellationToken ct)
        {
            return _client.GetThumbnailsAsync(this, ct);
        }

        public Task<OneDriveItem> CreateDirectoryAsync(string name, CancellationToken ct)
        {
            return _client.CreateDirectoryAsync(this, name, ct);
        }

        public Task<Stream> DownloadAsync(CancellationToken ct)
        {
            return _client.DownloadItemAsync(this, ct);
        }

        public Task<OneDriveItem> CreateFileAsync(string fileName, Stream content, long length, int chunckSize, Func<ChunkUploadErrorEventArgs, bool> chunkErrorHandler, CancellationToken ct)
        {
            return _client.CreateFileAsync(this, fileName, content, length, chunckSize, chunkErrorHandler, ct);
        }
    }
}