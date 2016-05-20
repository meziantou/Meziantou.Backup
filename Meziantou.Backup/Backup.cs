using System;
using System.Collections.Concurrent;
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
        public bool CanDeleteFiles { get; set; } = false;
        public bool CanUpdateFiles { get; set; } = true;
        public bool CanCreateFiles { get; set; } = true;
        public bool CanDeleteDirectories { get; set; } = false;
        public bool CanCreateDirectories { get; set; } = true;
        public int MaxDegreeOfParallelism { get; set; }
        public FileInfoEqualityMethods EqualityMethods { get; set; } = FileInfoEqualityMethods.Default;

        public event EventHandler<BackupActionEventArgs> Action;
        public event EventHandler<BackupErrorEventArgs> Error;

        private BlockingCollection<Func<Task>> _blockingCollection;
        private long _remaingDirectories = 0;

        private async Task<IDirectoryInfo> GetOrCreateRootDirectoryItemAsync(IFileSystem fileSystem, string path, CancellationToken ct)
        {
            var authenticable = fileSystem as IAuthenticable;
            if (authenticable != null)
            {
                await authenticable.LogInAsync(ct);
            }

            return await fileSystem.GetOrCreateDirectoryItemAsync(path, ct);
        }

        public async Task RunAsync(ProviderConfiguration source, ProviderConfiguration target, CancellationToken ct)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            ct.ThrowIfCancellationRequested();

            var sourceProvider = source.CreateProvider();
            var targetProvider = target.CreateProvider();

            var directories = await Task.WhenAll(
               GetOrCreateRootDirectoryItemAsync(sourceProvider, source.Path, ct),
               GetOrCreateRootDirectoryItemAsync(targetProvider, target.Path, ct)
            );

            Run(directories[0], directories[1], ct);
        }

        public void Run(IDirectoryInfo source, IDirectoryInfo target, CancellationToken ct)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            _blockingCollection = new BlockingCollection<Func<Task>>();

            IncrementCounter();
            EnqueueSynchronize(source, target, ct);

            var parallelOptions = new ParallelOptions();
            parallelOptions.CancellationToken = ct;
            if (MaxDegreeOfParallelism > 0)
            {
                parallelOptions.MaxDegreeOfParallelism = MaxDegreeOfParallelism;
            }

            // Use ConcurrentQueue to enable safe enqueueing from multiple threads.
            var exceptions = new ConcurrentQueue<Exception>();
            Parallel.ForEach(_blockingCollection.GetConsumingPartitioner(ct), parallelOptions, async item =>
            {
                if (item == null) throw new ArgumentNullException(nameof(item));

                try
                {
                    await item();
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            });

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }

        private async Task<T> RetryAsync<T>(Func<Task<T>> action)
        {
            int count = 0;
            while (true)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex)
                {
                    count++;
                    if (count > RetryCount)
                        throw;

                    if (!OnError(new BackupErrorEventArgs(ex, count)))
                        throw;
                }
            }
        }

        private async Task RetryAsync(Func<Task> action)
        {
            int count = 0;
            while (true)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex)
                {
                    count++;
                    if (count > RetryCount)
                        throw;

                    if (!OnError(new BackupErrorEventArgs(ex, count)))
                        throw;
                }
            }
        }

        protected virtual async Task<bool> AreEqualAsync(IFileInfo source, IFileInfo target, CancellationToken ct)
        {
            if (source == null && target == null)
                return true;

            if (source == null || target == null)
                return false;

            if (EqualityMethods.HasFlag(FileInfoEqualityMethods.Length))
            {
                if (source.Length != target.Length)
                    return false;
            }

            if (EqualityMethods.HasFlag(FileInfoEqualityMethods.LastWriteTime))
            {
                if (source.LastWriteTimeUtc > target.LastWriteTimeUtc)
                    return false;
            }

            // Use content or hash, not both (useless)
            if (EqualityMethods.HasFlag(FileInfoEqualityMethods.Content))
            {
                using (var xStream = await source.OpenReadAsync(ct))
                using (var yStream = await target.OpenReadAsync(ct))
                {
                    byte[] xBuffer = new byte[81920];
                    byte[] yBuffer = new byte[81920];
                    var xTask = xStream.ReadAsync(xBuffer, 0, xBuffer.Length, ct);
                    var yTask = yStream.ReadAsync(yBuffer, 0, yBuffer.Length, ct);
                    var xRead = await xTask;
                    var yRead = await yTask;

                    if (xRead != yRead)
                        return false;

                    for (int i = 0; i < xRead; i++)
                    {
                        if (xBuffer[i] != yBuffer[i])
                            return false;
                    }
                }
            }
            else if (EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentMd5) ||
                EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentSha1) ||
                EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentSha256) ||
                EqualityMethods.HasFlag(FileInfoEqualityMethods.ContentSha512))
            {
                using (var xStream = await source.OpenReadAsync(ct))
                using (var yStream = await target.OpenReadAsync(ct))
                {
                    var xHashTask = ComputeHashAsync(xStream, ct);
                    var yHashTask = ComputeHashAsync(yStream, ct);
                    var xHash = await xHashTask;
                    var yHash = await yHashTask;
                    if (xHash.Zip(yHash, (a, b) => AreSame(a.Item2, b.Item2)).Any(areSame => areSame == false))
                        return false;
                }
            }

            return true;
        }

        private static bool AreSame(byte[] a, byte[] b)
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
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
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

        private void EnqueueTask(Func<Task> action, CancellationToken ct)
        {
            _blockingCollection.Add(action, ct);
        }

        private void EnqueueSynchronize(IDirectoryInfo sourceItem, IDirectoryInfo targetItem, CancellationToken ct)
        {
            EnqueueTask(async () =>
            {
                await SynchronizeAsync(sourceItem, targetItem, ct);
                OnAction(new BackupActionEventArgs(BackupAction.Synchronized, sourceItem, targetItem));
            }, ct);
        }

        private void IncrementCounter()
        {
            Interlocked.Increment(ref _remaingDirectories);
        }

        private void DecrementCounter()
        {
            Interlocked.Decrement(ref _remaingDirectories);
        }

        private void EnqueueCreateItem(IFileSystemInfo sourceItem, IDirectoryInfo targetItem, CancellationToken ct)
        {
            var file = sourceItem as IFileInfo;
            if (file != null)
            {
                if (CanCreateFiles)
                {
                    if (!OnAction(new BackupActionEventArgs(BackupAction.Creating, sourceItem, targetItem)))
                        return;

                    EnqueueTask(async () =>
                    {
                        var fi = await RetryAsync(() => targetItem.CopyFileAsync(file, ct));
                        OnAction(new BackupActionEventArgs(BackupAction.Created, file, fi));
                    }, ct);
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

                        IncrementCounter();
                        EnqueueTask(async () =>
                        {
                            var di = await RetryAsync(() => targetItem.CreateDirectoryAsync(sourceDirectory.Name, ct));
                            OnAction(new BackupActionEventArgs(BackupAction.Created, sourceItem, di));

                            // Continue synchonization
                            EnqueueSynchronize(sourceDirectory, di, ct);
                        }, ct);
                    }
                }
            }
        }

        private void EnqueueUpdateItem(IFileInfo sourceItem, IDirectoryInfo targetItem, CancellationToken ct)
        {
            if (CanUpdateFiles)
            {
                if (!OnAction(new BackupActionEventArgs(BackupAction.Updating, sourceItem, targetItem)))
                    return;

                EnqueueTask(async () =>
                {
                    var fi = await RetryAsync(() => targetItem.CopyFileAsync(sourceItem, ct));
                    OnAction(new BackupActionEventArgs(BackupAction.Updated, sourceItem, fi));
                }, ct);
            }
        }

        private void EnqueueDeleteItem(IFileSystemInfo sourceItem, IFileSystemInfo targetItem, CancellationToken ct)
        {
            var file = targetItem as IFileInfo;
            if (file != null)
            {
                if (CanDeleteFiles)
                {
                    if (!OnAction(new BackupActionEventArgs(BackupAction.Deleting, sourceItem, targetItem)))
                        return;

                    EnqueueTask(async () =>
                    {
                        await RetryAsync(() => targetItem.DeleteAsync(ct));
                        OnAction(new BackupActionEventArgs(BackupAction.Deleted, sourceItem, targetItem));
                    }, ct);
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

                        EnqueueTask(async () =>
                        {
                            await RetryAsync(() => directory.DeleteAsync(ct));
                            OnAction(new BackupActionEventArgs(BackupAction.Deleted, sourceItem, targetItem));
                        }, ct);
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

            var sourceItems = await RetryAsync(() => source.GetItemsAsync(ct));
            var targetItems = await RetryAsync(() => target.GetItemsAsync(ct));

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
                    EnqueueCreateItem(sourceItem, target, ct);
                    continue;
                }

                var sourceFileItem = sourceItem as IFileInfo;
                var targetFileItem = targetItem as IFileInfo;
                if (sourceFileItem != null && targetFileItem != null)
                {
                    if (!await AreEqualAsync(sourceFileItem, targetFileItem, ct))
                    {
                        EnqueueUpdateItem(sourceFileItem, target, ct);
                        continue;
                    }
                }

                OnAction(new BackupActionEventArgs(BackupAction.Synchronized, sourceItem, target));
            }

            foreach (var targetItem in targetItems)
            {
                if (sourceItems.Get(targetItem) == null)
                {
                    EnqueueDeleteItem(source, targetItem, ct);
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
                    IncrementCounter();
                    EnqueueSynchronize(directory, targetDirectory, ct);
                }
            }

            DecrementCounter();
            if (_remaingDirectories == 0)
            {
                _blockingCollection.CompleteAdding();
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