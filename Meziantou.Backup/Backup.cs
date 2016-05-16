using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup
{
    public class Backup
    {
        public ProviderConfiguration SourceProviderConfiguration { get; set; }
        public ProviderConfiguration TargetProviderConfiguration { get; set; }
        public int RetryCount { get; set; } = 3;
        public bool CanDeleteFiles { get; set; } = false;
        public bool CanUpdateFiles { get; set; } = true;
        public bool CanCreateFiles { get; set; } = true;
        public bool CanDeleteDirectories { get; set; } = false;
        public bool CanCreateDirectories { get; set; } = true;

        public event EventHandler<BackupActionEventArgs> Action;
        public event EventHandler<BackupErrorEventArgs> Error;

        private async Task<IDirectoryInfo> GetOrCreateRootDirectoryItemAsync(IFileSystem fileSystem, string path, CancellationToken ct)
        {
            await fileSystem.AuthenticateAsync(ct);
            return await fileSystem.GetOrCreateDirectoryItemAsync(path, ct);
        }

        public async Task BackupAsync(CancellationToken ct)
        {
            if (SourceProviderConfiguration == null)
                throw new InvalidOperationException("SourceProvider is null");

            if (TargetProviderConfiguration == null)
                throw new InvalidOperationException("TargetProvider is null");

            ct.ThrowIfCancellationRequested();

            var sourceProvider = SourceProviderConfiguration.CreateProvider();
            var targetProvider = TargetProviderConfiguration.CreateProvider();

            var directories = await Task.WhenAll(
               GetOrCreateRootDirectoryItemAsync(sourceProvider, SourceProviderConfiguration.Path, ct),
               GetOrCreateRootDirectoryItemAsync(targetProvider, TargetProviderConfiguration.Path, ct)
            );

            await SynchronizeAsync(directories[0], directories[1], ct);
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

        protected virtual bool AreEqual(IFileInfo source, IFileInfo target)
        {
            if (source.LastWriteTimeUtc > target.LastWriteTimeUtc)
            {
                return false;
            }

            if (source.Length != target.Length)
            {
                return false;
            }

            return true;
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

            var targetDirectories = new List<IDirectoryInfo>(targetItems.OfType<IDirectoryInfo>());

            //
            // Compute differencies
            //
            var toBeCreatedItems = new List<IFileSystemInfo>();
            var toBeDeletedItems = new List<IFileSystemInfo>();
            var toBeUpdatedItems = new List<IFileSystemInfo>();

            foreach (var sourceItem in sourceItems)
            {
                var targetItem = targetItems.Get(sourceItem);
                if (targetItem == null)
                {
                    toBeCreatedItems.Add(sourceItem);
                    continue;
                }

                var sourceFileItem = sourceItem as IFileInfo;
                var targetFileItem = targetItem as IFileInfo;
                if (sourceFileItem != null && targetFileItem != null)
                {
                    if (!AreEqual(sourceFileItem, targetFileItem))
                    {
                        toBeUpdatedItems.Add(sourceItem);
                        continue;
                    }
                }
            }

            foreach (var targetItem in targetItems)
            {
                if (sourceItems.Get(targetItem) == null)
                {
                    toBeDeletedItems.Add(targetItem);
                }
            }

            ct.ThrowIfCancellationRequested();

            //
            // Handle items
            //
            foreach (var item in toBeCreatedItems)
            {
                var file = item as IFileInfo;
                if (file != null)
                {
                    if (CanCreateFiles)
                    {
                        if (!OnAction(new BackupActionEventArgs(BackupAction.Creating, item, target)))
                            return;

                        var fi = await RetryAsync(() => target.CopyFileAsync(file, ct));
                        OnAction(new BackupActionEventArgs(BackupAction.Created, item, fi));
                    }
                }
                else
                {
                    var directory = item as IDirectoryInfo;
                    if (directory != null)
                    {
                        if (CanCreateDirectories)
                        {
                            if (!OnAction(new BackupActionEventArgs(BackupAction.Creating, item, target)))
                                return;

                            var di = await RetryAsync(() => target.CreateDirectoryAsync(directory.Name, ct));
                            targetDirectories.Add(di);
                            OnAction(new BackupActionEventArgs(BackupAction.Created, item, di));
                        }
                    }
                }
            }

            foreach (var item in toBeUpdatedItems)
            {
                var file = item as IFileInfo;
                if (file != null)
                {
                    if (CanUpdateFiles)
                    {
                        if (!OnAction(new BackupActionEventArgs(BackupAction.Updating, item, target)))
                            continue;

                        var fi = await RetryAsync(() => target.CopyFileAsync(file, ct));
                        OnAction(new BackupActionEventArgs(BackupAction.Updated, item, fi));
                    }
                }
            }

            foreach (var item in toBeDeletedItems)
            {
                var file = item as IFileInfo;
                if (file != null)
                {
                    if (CanDeleteFiles)
                    {
                        if (!OnAction(new BackupActionEventArgs(BackupAction.Deleting, item, target)))
                            return;

                        await RetryAsync(() => target.DeleteAsync(ct));
                        OnAction(new BackupActionEventArgs(BackupAction.Deleted, item, target));
                    }
                }
                else
                {
                    var directory = item as IDirectoryInfo;
                    if (directory != null)
                    {
                        if (CanDeleteDirectories)
                        {
                            if (!OnAction(new BackupActionEventArgs(BackupAction.Deleting, item, target)))
                                return;

                            await RetryAsync(() => directory.DeleteAsync(ct));
                            OnAction(new BackupActionEventArgs(BackupAction.Deleted, item, target));
                        }
                    }
                }
            }

            //
            // Synchronize sub folders
            //
            foreach (var directory in sourceItems.OfType<IDirectoryInfo>())
            {
                var targetDirectory = targetDirectories.Get(directory);
                if (targetDirectory != null)
                {
                    await SynchronizeAsync(directory, targetDirectory, ct);
                }
            }

            OnAction(new BackupActionEventArgs(BackupAction.Synchronized, source, target));
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
    }
}