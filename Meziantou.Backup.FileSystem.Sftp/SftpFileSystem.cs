using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace Meziantou.Backup.FileSystem.Sftp
{
    public class SftpFileSystem : IFileSystem, IDisposable
    {
        private const char PathSeparator = '/';
        private const char AltPathSeparator = '\\';

        private SftpClient _client;

        public string Host { get; set; }
        public int Port { get; set; } = 22;
        public string Username { get; set; }
        public string Password { get; set; }
        public string PrivateKeyFile { get; set; }

        internal SftpClient Client
        {
            get
            {
                EnsureClient();
                return _client;
            }
        }

        protected virtual void EnsureClient()
        {
            if (_client != null)
                return;

            _client = CreateClient();
        }

        public virtual Task<IDirectoryInfo> GetOrCreateDirectoryItemAsync(string path, CancellationToken ct)
        {
            var fileInfo = CreateDirectoryRecursively(path);
            return Task.FromResult<IDirectoryInfo>(fileInfo);
        }

        protected virtual SftpClient CreateClient()
        {
            SftpClient client = null;
            if (!string.IsNullOrEmpty(PrivateKeyFile))
            {
                using (var pkf = new PrivateKeyFile(PrivateKeyFile, Password))
                {
                    try
                    {
                        client = new SftpClient(Host, Port, Username, pkf);
                        client.Connect();
                    }
                    catch
                    {
                        client?.Dispose();
                        throw;
                    }
                }
            }
            else
            {
                try
                {
                    client = new SftpClient(Host, Port, Username, Password);
                    client.Connect();
                }
                catch
                {
                    client?.Dispose();
                    throw;
                }
            }

            return client;
        }

        internal virtual Task<IReadOnlyCollection<SftpFileInfo>> ListDirectoryAsync(string path)
        {
            path = NormalizePath(path);

            var client = Client;
            var tcs = new TaskCompletionSource<IReadOnlyCollection<SftpFileInfo>>();
            client.BeginListDirectory(path, asyncResult =>
            {
                try
                {
                    var content = client.EndListDirectory(asyncResult)
                        .Where(sftpFile => sftpFile.Name != "." && sftpFile.Name != "..")
                        .Select(sftpFile => new SftpFileInfo(this, sftpFile))
                        .ToList();
                    tcs.TrySetResult(content);
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        protected virtual string NormalizePath(string path)
        {
            if (path == null)
                return null;

            return path.Replace(AltPathSeparator, PathSeparator);
        }

        protected virtual SftpFileInfo CreateDirectoryRecursively(string path)
        {
            path = NormalizePath(path);
            string current = "";

            if (path[0] == PathSeparator)
            {
                path = path.Substring(1);
            }

            while (!string.IsNullOrEmpty(path))
            {
                int p = path.IndexOf(PathSeparator);
                current += PathSeparator;
                if (p >= 0)
                {
                    current += path.Substring(0, p);
                    path = path.Substring(p + 1);
                }
                else
                {
                    current += path;
                    path = "";
                }

                try
                {
                    SftpFileAttributes attrs = Client.GetAttributes(current);
                    if (!attrs.IsDirectory)
                        return null;
                }
                catch (SftpPathNotFoundException)
                {
                    Client.CreateDirectory(current);
                }
            }

            var file = Client.Get(current);
            return new SftpFileInfo(this, file);
        }

        internal virtual SftpFileInfo CreateDirectory(string parent, string name)
        {
            string fullPath = NormalizePath(parent) + PathSeparator + name;

            Client.CreateDirectory(fullPath);
            var file = Client.Get(fullPath);
            return new SftpFileInfo(this, file);
        }

        internal virtual Task<SftpFileInfo> UploadFileAsync(string parent, string fileName, Stream stream)
        {
            string fullPath = NormalizePath(parent) + PathSeparator + fileName;

            var client = Client;
            var tcs = new TaskCompletionSource<SftpFileInfo>();
            client.BeginUploadFile(stream, fullPath, true, asyncResult =>
            {
                try
                {
                    client.EndUploadFile(asyncResult);
                    var fileInfo = new SftpFileInfo(this, client.Get(fullPath));
                    tcs.TrySetResult(fileInfo);
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        internal virtual Task<Stream> DownloadFileAsync(string fullPath)
        {
            var client = Client;
            var sftpFileStream = client.OpenRead(NormalizePath(fullPath));
            return Task.FromResult<Stream>(sftpFileStream);
        }

        public virtual void Initialize(IDictionary<string, object> data)
        {
            if (data == null)
                return;

            if (data.ContainsKey(nameof(Host)))
            {
                Host = data[nameof(Host)] as string;
            }

            if (data.ContainsKey(nameof(Port)))
            {
                var value = data[nameof(Port)];
                if (value is int)
                {
                    Port = (int)value;
                }
                else
                {
                    if (value is string str)
                    {
                        if (int.TryParse(str, out int port))
                        {
                            Port = port;
                        }
                    }
                }
            }

            if (data.ContainsKey(nameof(Username)))
            {
                Username = data[nameof(Username)] as string;
            }

            if (data.ContainsKey(nameof(Password)))
            {
                Password = data[nameof(Password)] as string;
            }

            if (data.ContainsKey(nameof(PrivateKeyFile)))
            {
                PrivateKeyFile = data[nameof(PrivateKeyFile)] as string;
            }
        }

        public virtual void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}