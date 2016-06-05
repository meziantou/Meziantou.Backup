using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup
{
    public class Backup
    {
        public int RetryCount { get; set; } = 3;
        public bool ContinueOnError { get; set; } = true;
        public bool CanDeleteFiles { get; set; } = false;
        public bool CanUpdateFiles { get; set; } = true;
        public bool CanCreateFiles { get; set; } = true;
        public bool CanDeleteDirectories { get; set; } = false;
        public bool CanCreateDirectories { get; set; } = true;
        public FileInfoEqualityMethods EqualityMethods { get; set; } = FileInfoEqualityMethods.Default;

        public event EventHandler<BackupActionEventArgs> Action;
        public event EventHandler<BackupErrorEventArgs> Error;
        public event EventHandler<FileCopyingEventArgs> Copying;

        public async Task RunAsync(IFileSystem source, IFileSystem target, CancellationToken ct)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var sourceDirectory = await source.GetOrCreateDirectoryItemAsync("/", ct);
            var targetDirectory = await target.GetOrCreateDirectoryItemAsync("/", ct);

            await SynchronizeAsync(sourceDirectory, targetDirectory, ct);
        }

        public Task RunAsync(IDirectoryInfo source, IDirectoryInfo target, CancellationToken ct)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            return SynchronizeAsync(source, target, ct);
        }

        private async Task<T> RetryAsync<T>(Func<Task<T>> action, CancellationToken ct)
        {
            int count = 0;
            while (true)
            {
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    count++;
                    if (count > RetryCount || !OnError(new BackupErrorEventArgs(ex, count)))
                    {
                        if (ContinueOnError)
                        {
                            return default(T);
                        }

                        throw;
                    }
                }

                await Task.Delay(1000, ct);
            }
        }

        private async Task RetryAsync(Func<Task> action, CancellationToken ct)
        {
            int count = 0;
            while (true)
            {
                try
                {
                    await action().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    count++;
                    if (count > RetryCount || !OnError(new BackupErrorEventArgs(ex, count)))
                    {
                        if (ContinueOnError)
                            return;

                        throw;
                    }
                }

                await Task.Delay(1000, ct);
            }
        }

        protected virtual async Task<FileInfoEqualityMethods> AreEqualAsync(IFileInfo source, IFileInfo target, CancellationToken ct)
        {
            if (source == null && target == null)
                return FileInfoEqualityMethods.None;

            if (source == null || target == null)
                return FileInfoEqualityMethods.None;

            if (EqualityMethods.HasFlag(FileInfoEqualityMethods.Length))
            {
                if (source.Length < 0 || target.Length < 0 || source.Length != target.Length)
                    return FileInfoEqualityMethods.Length;
            }

            if (EqualityMethods.HasFlag(FileInfoEqualityMethods.LastWriteTime))
            {
                if (source.LastWriteTimeUtc > target.LastWriteTimeUtc)
                    return FileInfoEqualityMethods.LastWriteTime;
            }

            // Use content or hash, not both (useless)
            if (EqualityMethods.HasFlag(FileInfoEqualityMethods.Content))
            {
                using (var xStream = await source.OpenReadAsync(ct).ConfigureAwait(false))
                using (var yStream = await target.OpenReadAsync(ct).ConfigureAwait(false))
                {
                    byte[] xBuffer = new byte[81920];
                    byte[] yBuffer = new byte[81920];
                    var xTask = xStream.ReadAsync(xBuffer, 0, xBuffer.Length, ct);
                    var yTask = yStream.ReadAsync(yBuffer, 0, yBuffer.Length, ct);
                    var xRead = await xTask.ConfigureAwait(false);
                    var yRead = await yTask.ConfigureAwait(false);

                    if (xRead != yRead)
                        return FileInfoEqualityMethods.Content;

                    for (int i = 0; i < xRead; i++)
                    {
                        if (xBuffer[i] != yBuffer[i])
                            return FileInfoEqualityMethods.Content;
                    }
                }
            }
            else if (EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentMd5) ||
                EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentSha1) ||
                EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentSha256) ||
                EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentSha512))
            {
                using (var xStream = await source.OpenReadAsync(ct).ConfigureAwait(false))
                using (var yStream = await target.OpenReadAsync(ct).ConfigureAwait(false))
                {
                    var xHashTask = ComputeHashAsync(xStream, ct);
                    var yHashTask = ComputeHashAsync(yStream, ct);
                    var xHash = await xHashTask.ConfigureAwait(false);
                    var yHash = await yHashTask.ConfigureAwait(false);
                    var diff = xHash.Zip(yHash, (a, b) => AreEqual(a.Item2, b.Item2) ? FileInfoEqualityMethods.None : a.Item1).FirstOrDefault(_ => _ != FileInfoEqualityMethods.None);
                    if (diff != FileInfoEqualityMethods.None)
                        return diff;
                }
            }

            return FileInfoEqualityMethods.None;
        }

        private static bool AreEqual(byte[] a, byte[] b)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        protected virtual async Task<IList<Tuple<FileInfoEqualityMethods, byte[]>>> ComputeHashAsync(Stream stream, CancellationToken ct)
        {
            var algorithms = new List<HashResult>();
            try
            {
                // Init algorithm
                if (EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentMd5))
                    algorithms.Add(new HashResult(FileInfoEqualityMethods.ContentMd5));

                if (EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentSha1))
                    algorithms.Add(new HashResult(FileInfoEqualityMethods.ContentSha1));

                if (EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentSha256))
                    algorithms.Add(new HashResult(FileInfoEqualityMethods.ContentSha256));

                if (EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentSha512))
                    algorithms.Add(new HashResult(FileInfoEqualityMethods.ContentSha512));

                byte[] buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                {
                    foreach (var hashAlgorithm in algorithms)
                    {
                        hashAlgorithm.TransformBlock(buffer, read);
                    }
                }

                return algorithms.Select(result => Tuple.Create(result.Method, result.TransformFinalBlock())).ToList();
            }
            finally
            {
                foreach (var hashAlgorithm in algorithms)
                {
                    hashAlgorithm.Dispose();
                }
            }
        }

        private async Task<IFileInfo> CopyFileAsync(IDirectoryInfo directory, IFileInfo file, CancellationToken ct)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            if (file == null) throw new ArgumentNullException(nameof(file));

            using (var inputStream = await file.OpenReadAsync(ct).ConfigureAwait(false))
            {
                using (var progressStream = new ProgressStream(inputStream, true))
                {
                    long currentPosition = 0;
                    progressStream.StreamRead += (sender, args) =>
                    {
                        currentPosition += args.Count;
                        OnCopying(new FileCopyingEventArgs(file, directory, currentPosition, file.Length));
                    };

                    return await directory.CreateFileAsync(file.Name, progressStream, file.Length, ct).ConfigureAwait(false);
                }
            }
        }

        private async Task CreateItemAsync(IFileSystemInfo sourceItem, IDirectoryInfo targetItem, CancellationToken ct)
        {
            var file = sourceItem as IFileInfo;
            if (file != null)
            {
                if (CanCreateFiles)
                {
                    if (!OnAction(new BackupActionEventArgs(BackupAction.Creating, sourceItem, targetItem)))
                        return;

                    var fi = await RetryAsync(() => CopyFileAsync(targetItem, file, ct), ct).ConfigureAwait(false);
                    OnAction(new BackupActionEventArgs(BackupAction.Created, file, fi));
                }
            }
            else
            {
                var sourceDirectory = sourceItem as IDirectoryInfo;
                if (sourceDirectory != null)
                {
                    if (CanCreateDirectories)
                    {
                        if (!OnAction(new BackupActionEventArgs(BackupAction.Creating, sourceDirectory, targetItem)))
                            return;

                        var di = await RetryAsync(() => targetItem.CreateDirectoryAsync(sourceDirectory.Name, ct), ct).ConfigureAwait(false);
                        OnAction(new BackupActionEventArgs(BackupAction.Created, sourceItem, di));

                        if (di != null)
                        {
                            // Continue synchonization of the new directory
                            await SynchronizeAsync(sourceDirectory, di, ct).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async Task UpdateItemAsync(IFileInfo sourceItem, IDirectoryInfo targetItem, FileInfoEqualityMethods diff, CancellationToken ct)
        {
            if (CanUpdateFiles)
            {
                if (!OnAction(new BackupActionEventArgs(BackupAction.Updating, sourceItem, targetItem, diff)))
                    return;

                var fi = await RetryAsync(() => CopyFileAsync(targetItem, sourceItem, ct), ct).ConfigureAwait(false);
                OnAction(new BackupActionEventArgs(BackupAction.Updated, sourceItem, fi, diff));
            }
        }

        private async Task DeleteItemAsync(IFileSystemInfo sourceItem, IFileSystemInfo targetItem, CancellationToken ct)
        {
            var file = targetItem as IFileInfo;
            if (file != null)
            {
                if (CanDeleteFiles)
                {
                    if (!OnAction(new BackupActionEventArgs(BackupAction.Deleting, sourceItem, targetItem)))
                        return;

                    await RetryAsync(() => targetItem.DeleteAsync(ct), ct).ConfigureAwait(false);
                    OnAction(new BackupActionEventArgs(BackupAction.Deleted, sourceItem, targetItem));
                }
            }
            else
            {
                var directory = targetItem as IDirectoryInfo;
                if (directory != null)
                {
                    if (CanDeleteDirectories)
                    {
                        if (!OnAction(new BackupActionEventArgs(BackupAction.Deleting, sourceItem, targetItem)))
                            return;

                        await RetryAsync(() => directory.DeleteAsync(ct), ct).ConfigureAwait(false);
                        OnAction(new BackupActionEventArgs(BackupAction.Deleted, sourceItem, targetItem));
                    }
                }
            }
        }

        protected virtual async Task SynchronizeAsync(IDirectoryInfo source, IDirectoryInfo target, CancellationToken ct)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            ct.ThrowIfCancellationRequested();

            if (!OnAction(new BackupActionEventArgs(BackupAction.Synchronizing, source, target)))
                return;
            
            var sourceItemsResult = await RetryAsync(() => source.GetItemsAsync(ct), ct).ConfigureAwait(false);
            if (sourceItemsResult == null)
                return;

            var targetItemResult = await RetryAsync(() => target.GetItemsAsync(ct), ct).ConfigureAwait(false);
            if (targetItemResult == null)
                return;

            // Clone collections because CreateItem or DeleteItem may change them
            var sourceItems = sourceItemsResult.ToList();
            var targetItems = targetItemResult.ToList();
            
            //
            // Compute differencies
            //
            foreach (var sourceItem in sourceItems)
            {
                if (!OnAction(new BackupActionEventArgs(BackupAction.Synchronizing, sourceItem, target)))
                    continue;

                var targetItem = targetItems.Get(sourceItem);
                if (targetItem == null)
                {
                    await CreateItemAsync(sourceItem, target, ct).ConfigureAwait(false);
                    continue;
                }

                var sourceFileItem = sourceItem as IFileInfo;
                var targetFileItem = targetItem as IFileInfo;
                if (sourceFileItem != null && targetFileItem != null)
                {
                    var diff = await AreEqualAsync(sourceFileItem, targetFileItem, ct).ConfigureAwait(false);
                    if (diff != FileInfoEqualityMethods.None)
                    {
                        await UpdateItemAsync(sourceFileItem, target, diff, ct).ConfigureAwait(false);
                        continue;
                    }
                }

                OnAction(new BackupActionEventArgs(BackupAction.Synchronized, sourceItem, target));
            }

            foreach (var targetItem in targetItems)
            {
                if (sourceItems.Get(targetItem) == null)
                {
                    await DeleteItemAsync(source, targetItem, ct).ConfigureAwait(false);
                }
            }

            ct.ThrowIfCancellationRequested();

            //
            // Synchronize sub folders
            //
            foreach (var directory in sourceItems.OfType<IDirectoryInfo>())
            {
                var targetDirectory = targetItems.Get(directory) as IDirectoryInfo;
                if (targetDirectory != null)
                {
                    await SynchronizeAsync(directory, targetDirectory, ct).ConfigureAwait(false);
                }
            }
        }

        protected virtual bool OnAction(BackupActionEventArgs e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));

            Action?.Invoke(this, e);
            return !e.Cancel;
        }

        protected virtual bool OnError(BackupErrorEventArgs e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));

            Error?.Invoke(this, e);
            return !e.Cancel;
        }

        protected virtual void OnCopying(FileCopyingEventArgs e)
        {
            Copying?.Invoke(this, e);
        }

        private class HashResult : IDisposable
        {
            private readonly HashAlgorithm _algorithm;

            public FileInfoEqualityMethods Method { get; }

            public HashResult(FileInfoEqualityMethods method)
            {
                Method = method;
                if (method.HasFlag(FileInfoEqualityMethods.ContentMd5))
                {
                    _algorithm = MD5.Create();
                }
                else if (method.HasFlag(FileInfoEqualityMethods.ContentSha1))
                {
                    _algorithm = SHA1.Create();
                }
                else if (method.HasFlag(FileInfoEqualityMethods.ContentSha256))
                {
                    _algorithm = SHA256.Create();
                }
                else if (method.HasFlag(FileInfoEqualityMethods.ContentSha512))
                {
                    _algorithm = SHA512.Create();
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(method));
                }
            }

            public void TransformBlock(byte[] bytes, int count)
            {
                _algorithm.TransformBlock(bytes, 0, count, null, 0);
            }

            public byte[] TransformFinalBlock()
            {
                _algorithm.TransformFinalBlock(new byte[0], 0, 0);
                return _algorithm.Hash;
            }

            public void Dispose()
            {
                _algorithm?.Dispose();
            }
        }
    }
}