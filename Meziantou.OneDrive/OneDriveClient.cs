using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Meziantou.OneDrive
{
    public class OneDriveClient : IDisposable
    {
        // https://dev.onedrive.com/index.htm
        private const string ApplicationId = "000000004418B915";
        private const string ReturnUrl = "https://login.live.com/oauth20_desktop.srf";
        private const string ApiUrl = "https://api.onedrive.com/v1.0/";
        private static readonly char[] PathSeparators = { '/', '\\' };
        private string _accessCode;
        private HttpClient _client;
        private readonly string[] _scopes = { "wl.signin", "wl.offline_access", "onedrive.readwrite" };

        public IRefreshTokenHandler RefreshTokenHandler { get; set; }

        public bool IsAuthenticated => _accessCode != null;

        public void Dispose()
        {
            _client?.Dispose();
        }

        public Task LogOutAsync(CancellationToken ct)
        {
            return LogOutAsync(true, true, ct);
        }

        public async Task LogOutAsync(bool removeRefreshToken, bool removeCookies, CancellationToken ct)
        {
            _accessCode = null;
            if (removeRefreshToken && RefreshTokenHandler != null)
            {
                await RefreshTokenHandler.DeleteRefreshTokenAsync(ct);
            }

            if (removeCookies)
            {
                string logoutUrl = $"https://login.live.com/oauth20_logout.srf?client_id={Uri.EscapeDataString(ApplicationId)}&redirect_uri={Uri.EscapeDataString(ReturnUrl)}";
                await GetAsync(logoutUrl, OneDriveClientGetOptions.None, ct);
            }
        }

        public async Task<bool> AuthenticateAsync(CancellationToken ct)
        {
            // https://dev.onedrive.com/auth/msa_oauth.htm
            if (_accessCode != null)
                return IsAuthenticated;

            if (await RefreshAccessTokenAsync(ct))
                return IsAuthenticated;

            var scope = string.Join(" ", _scopes);
            string loginUrl = $"https://login.live.com/oauth20_authorize.srf?client_id={ Uri.EscapeDataString(ApplicationId) }&scope={ Uri.EscapeDataString(scope) }&response_type=code&redirect_uri={ Uri.EscapeDataString(ReturnUrl) }";

            var authenticationForm = new AuthenticationForm(loginUrl);
            Application.EnableVisualStyles();
            Application.Run(authenticationForm);

            if (authenticationForm.AuthorizationCode != null)
            {
                var parameters = new Dictionary<string, string>();
                parameters["client_id"] = ApplicationId;
                parameters["redirect_uri"] = ReturnUrl;
                //parameters["client_secret"] = "";
                parameters["code"] = authenticationForm.AuthorizationCode;
                parameters["grant_type"] = "authorization_code";
                var result = await PostAsync<OneDriveToken>("https://login.live.com/oauth20_token.srf", new FormUrlEncodedContent(parameters), ct);
                await HandleTokenResponseAsync(result, ct);
            }

            return IsAuthenticated;
        }

        private async Task HandleTokenResponseAsync(OneDriveToken result, CancellationToken ct)
        {
            if (result != null && RefreshTokenHandler != null)
            {
                var refreshTokenInfo = new RefreshTokenInfo(result.RefreshToken);
                await RefreshTokenHandler.SaveRefreshTokenAsync(refreshTokenInfo, ct);
            }

            _accessCode = result?.AccessToken;
            UpdateCrendentials(_client);
        }

        protected async Task<bool> RefreshAccessTokenAsync(CancellationToken ct)
        {
            if (RefreshTokenHandler == null)
                return false;

            var refreshToken = await RefreshTokenHandler.RetrieveRefreshTokenAsync(ct);
            if (refreshToken?.RefreshToken == null)
                return false;

            var parameters = new Dictionary<string, string>();
            parameters["client_id"] = ApplicationId;
            parameters["redirect_uri"] = ReturnUrl;
            //parameters["client_secret"] = "";
            parameters["refresh_token"] = refreshToken.RefreshToken;
            parameters["grant_type"] = "refresh_token";
            var result = await PostAsync<OneDriveToken>("https://login.live.com/oauth20_token.srf", new FormUrlEncodedContent(parameters), ct);
            await HandleTokenResponseAsync(result, ct);
            return IsAuthenticated;
        }

        protected virtual async Task<T> PostAsync<T>(string url, HttpContent content, CancellationToken ct)
        {
            EnsureHttpClient();
            using (var result = await _client.PostAsync(url, content, ct))
            {
                await EnsureResultAsync(result, ct);
                var json = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json, CreateJsonSerializerSettings());
            }
        }

        protected virtual async Task<T> PostJsonAsync<T>(string url, object data, CancellationToken ct)
        {
            EnsureHttpClient();
            var jsonSerializerSettings = CreateJsonSerializerSettings();
            var jsonData = JsonConvert.SerializeObject(data, jsonSerializerSettings);

            using (var content = new StringContent(jsonData, null, "application/json"))
            {
                return await PostAsync<T>(url, content, ct);
            }
        }

        protected virtual async Task<T> PutAsync<T>(string url, HttpContent content, CancellationToken ct)
        {
            EnsureHttpClient();
            using (var result = await _client.PutAsync(url, content, ct))
            {
                await EnsureResultAsync(result, ct);
                var json = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json, CreateJsonSerializerSettings());
            }
        }

        protected virtual async Task<T> PutJsonAsync<T>(string url, object data, CancellationToken ct)
        {
            EnsureHttpClient();
            var jsonSerializerSettings = CreateJsonSerializerSettings();
            var jsonData = JsonConvert.SerializeObject(data, jsonSerializerSettings);

            using (var content = new StringContent(jsonData, null, "application/json"))
            {
                return await PutAsync<T>(url, content, ct);
            }
        }

        protected virtual async Task GetAsync(string url, OneDriveClientGetOptions options, CancellationToken ct)
        {
            EnsureHttpClient();
            using (var result = await _client.GetAsync(url, ct))
            {
                await EnsureResultAsync(result, ct);
            }
        }

        protected virtual async Task<T> GetAsync<T>(string url, OneDriveClientGetOptions options, CancellationToken ct)
        {
            EnsureHttpClient();
            using (var result = await _client.GetAsync(url, ct))
            {
                if (options.HasFlag(OneDriveClientGetOptions.ReturnDefaultWhenNotFound) && result.StatusCode == HttpStatusCode.NotFound)
                    return default(T);

                await EnsureResultAsync(result, ct);
                var json = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json, CreateJsonSerializerSettings());
            }
        }

        protected virtual async Task<Stream> GetStreamAsync(string url, CancellationToken ct)
        {
            EnsureHttpClient();
            var result = await _client.GetAsync(url, ct);
            Stream stream = null;
            try
            {
                await EnsureResultAsync(result, ct);
                stream = await result.Content.ReadAsStreamAsync();
            }
            catch
            {
                stream?.Dispose();
                result.Dispose();
                throw;
            }

            var s = new StreamWithDisposeEvents(stream);
            s.Disposed += (sender, args) => result.Dispose();
            return s;
        }

        protected virtual async Task<T> PutStreamAsync<T>(string url, Stream content, CancellationToken ct)
        {
            EnsureHttpClient();
            using (var sc = new StreamContent(content))
            {
                using (var result = await _client.PutAsync(url, sc, ct))
                {
                    await EnsureResultAsync(result, ct);
                    var json = await result.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(json, CreateJsonSerializerSettings());
                }
            }
        }

        protected virtual async Task<T> DeleteAsync<T>(string url, CancellationToken ct)
        {
            EnsureHttpClient();
            using (var result = await _client.DeleteAsync(url, ct))
            {
                await EnsureResultAsync(result, ct);
                var json = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json, CreateJsonSerializerSettings());
            }
        }

        protected virtual async Task DeleteAsync(string url, CancellationToken ct)
        {
            EnsureHttpClient();
            using (var result = await _client.DeleteAsync(url, ct))
            {
                await EnsureResultAsync(result, ct);
            }
        }

        protected virtual JsonSerializerSettings CreateJsonSerializerSettings()
        {
            var jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonSerializerSettings.Converters.Add(new OneDriveItemConverter(this));
            return jsonSerializerSettings;
        }

        protected void EnsureHttpClient()
        {
            if (_client == null)
            {
                _client = CreateHttpClient();
            }
        }

        protected async Task EnsureResultAsync(HttpResponseMessage message, CancellationToken ct)
        {
            if (!message.IsSuccessStatusCode)
            {
                try
                {
                    string content = null;
                    if (message.Content != null)
                    {
                        content = await message.Content.ReadAsStringAsync();
                    }

                    throw new Exception($"Code: {message.StatusCode}; Reason: {message.ReasonPhrase}; Content: " + content);
                }
                finally
                {
                    message?.Content?.Dispose();
                }
            }
        }

        protected void UpdateCrendentials(HttpClient client)
        {
            if (client == null)
                return;

            if (_accessCode != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessCode);
            }
            else
            {
                client.DefaultRequestHeaders.Authorization = null;
            }
        }

        protected virtual HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(ApiUrl);
            UpdateCrendentials(httpClient);
            return httpClient;
        }

        public Task<Stream> DownloadItemAsync(string path, CancellationToken ct)
        {
            return GetStreamAsync($"drive/root:{path}:/Content", ct);
        }

        public Task<Stream> DownloadItemAsync(OneDriveItem item, CancellationToken ct)
        {
            return GetStreamAsync($"drive/items/{item.Id}/Content", ct);
        }

        public async Task<OneDriveItem> CreateFileAsync(OneDriveItem parent, string fileName, Stream content, long length, int chunckSize, Func<ChunkUploadErrorEventArgs, bool> chunkErrorHandler, CancellationToken ct)
        {
            if (length <= chunckSize)
            {
                return await PutStreamAsync<OneDriveItem>($"drive/items/{parent.Id}:/{fileName}:/content", content, ct);
            }

            // https://dev.onedrive.com/items/upload_large_files.htm
            var session = await PostAsync<ChunkedUploadSessionResult>($"drive/items/{parent.Id}:/{fileName}:/upload.createSession", null, ct);
            long rangeFrom = 0;
            var bytes = new byte[chunckSize];
            int read;
            while ((read = await content.ReadAsync(bytes, 0, chunckSize, ct)) > 0)
            {
                using (var byteArrayContent = new ByteArrayContent(bytes, 0, read))
                {
                    int attempt = 1;
                    while (true)
                    {
                        long rangeTo = rangeFrom + read - 1;
                        try
                        {
                            byteArrayContent.Headers.ContentRange = new ContentRangeHeaderValue(rangeFrom, rangeTo, length);
                            using (var result = await _client.PutAsync(session.UploadUrl, byteArrayContent, ct))
                            {
                                await EnsureResultAsync(result, ct);
                                if (result.StatusCode == HttpStatusCode.Created || result.StatusCode == HttpStatusCode.OK)
                                {
                                    var json = await result.Content.ReadAsStringAsync();
                                    return JsonConvert.DeserializeObject<OneDriveItem>(json, CreateJsonSerializerSettings());
                                }
                            }

                            break;
                        }
                        catch (Exception ex)
                        {
                            if (chunkErrorHandler != null)
                            {
                                var args = new ChunkUploadErrorEventArgs(this, parent, ex, bytes, rangeFrom, rangeTo, attempt);
                                if (chunkErrorHandler(args))
                                {
                                    attempt++;
                                    continue;
                                }
                            }

                            throw;
                        }
                    }
                }

                rangeFrom += read;
            }

            return null; // TODO throw error
        }

        public Task<OneDriveItem> GetItemAsync(string path, CancellationToken ct)
        {
            return GetAsync<OneDriveItem>($"drive/root:{path}:", OneDriveClientGetOptions.ReturnDefaultWhenNotFound, ct);
        }

        public Task<OneDriveItem> GetRootFolderAsync(CancellationToken ct)
        {
            return GetAsync<OneDriveItem>("drive/root", OneDriveClientGetOptions.None, ct);
        }

        public Task<OneDriveItem> CreateDirectoryAsync(OneDriveItem parent, string name, CancellationToken ct)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (name == null) throw new ArgumentNullException(nameof(name));

            // https://dev.onedrive.com/items/create.htm
            var data = new
            {
                Name = name,
                Folder = new { }
            };

            return PostJsonAsync<OneDriveItem>($"drive/items/{parent.Id}/children", data, ct);
        }

        public async Task<OneDriveItem> CreateDirectoryAsync(string path, CancellationToken ct)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var parts = new Stack<string>();

            path = path.TrimEnd(PathSeparators);
            OneDriveItem item = null;
            while (true)
            {
                if (string.IsNullOrEmpty(path))
                {
                    item = await GetRootFolderAsync(ct);
                }
                else
                {
                    item = await GetItemAsync(path, ct);
                }

                if (item != null)
                    break;

                var index = path.LastIndexOfAny(PathSeparators);
                if (index < 0)
                    return null;

                parts.Push(path.Substring(index + 1));
                path = path.Substring(0, index);
            }

            while (parts.Any())
            {
                var part = parts.Pop();
                item = await CreateDirectoryAsync(item, part, ct);
            }

            return item;
        }

        public Task DeleteItemAsync(OneDriveItem item, CancellationToken ct)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            return DeleteAsync("drive/items/" + item.Id, ct);
        }

        public async Task<IReadOnlyList<OneDriveItem>> GetChildrenAsync(OneDriveItem parent, CancellationToken ct)
        {
            string url = $"drive/items/{parent.Id}/children";
            var items = new List<OneDriveItem>();

            while (!string.IsNullOrEmpty(url))
            {
                var nextItems = await GetAsync<PagedResponse<OneDriveItem>>(url, OneDriveClientGetOptions.None, ct);
                items.AddRange(nextItems.Value);
                url = nextItems.NextLink;
            }

            return items;
        }

        public Task<ThumbnailSet> GetThumbnailsAsync(OneDriveItem item, CancellationToken ct)
        {
            return GetAsync<ThumbnailSet>($"drive/items/{item.Id}/thumbnails", OneDriveClientGetOptions.None, ct);
        }
    }
}