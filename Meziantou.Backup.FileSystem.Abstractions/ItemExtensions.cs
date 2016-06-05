using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public static class ItemExtensions
    {
        private static readonly FileSystemInfoEqualityComparer EqualityComparer = new FileSystemInfoEqualityComparer();

        public static T Get<T>(this IEnumerable<T> items, T item) where T : IFileSystemInfo
        {
            return Get(items, item, EqualityComparer);
        }

        public static T Get<T>(this IEnumerable<T> items, T item, IEqualityComparer<IFileSystemInfo> comparer) where T : IFileSystemInfo
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            return items.FirstOrDefault(_ => comparer.Equals(item, _));
        }

        public static async Task<IFileInfo> FindFileAsync(this IDirectoryInfo di, string name, CancellationToken ct)
        {
            if (di == null) throw new ArgumentNullException(nameof(di));
            if (name == null) throw new ArgumentNullException(nameof(name));

            var children = await di.GetItemsAsync(ct);
            foreach (var child in children.OfType<IFileInfo>())
            {
                if (!child.IsFile())
                    continue;

                if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        public static async Task<IDirectoryInfo> FindDirectoryAsync(this IDirectoryInfo di, string name, CancellationToken ct)
        {
            if (di == null) throw new ArgumentNullException(nameof(di));
            if (name == null) throw new ArgumentNullException(nameof(name));

            var children = await di.GetItemsAsync(ct);
            foreach (var child in children.OfType<IDirectoryInfo>())
            {
                if (!child.IsDirectory())
                    continue;

                if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        public static Task<IFileInfo> CopyFileAsync(this IDirectoryInfo directory, IFileInfo file, CancellationToken ct)
        {
            return CopyFileAsync(directory, file, file.Name, ct);
        }

        public static async Task<IFileInfo> CopyFileAsync(this IDirectoryInfo directory, IFileInfo file, string fileName, CancellationToken ct)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            if (file == null) throw new ArgumentNullException(nameof(file));

            using (var inputStream = await file.OpenReadAsync(ct).ConfigureAwait(false))
            {
                return await directory.CreateFileAsync(fileName, inputStream, file.Length, ct).ConfigureAwait(false);
            }
        }

        public static bool IsFile(this IFileSystemInfo fsi)
        {
            if (fsi == null) throw new ArgumentNullException(nameof(fsi));

            return !fsi.IsDirectory;
        }

        public static bool IsDirectory(this IFileSystemInfo fsi)
        {
            if (fsi == null) throw new ArgumentNullException(nameof(fsi));

            return fsi.IsDirectory;
        }

        public static async Task<IFileInfo> CreateFileAsync(this IDirectoryInfo parent, string name, byte[] content, CancellationToken ct)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (content == null) throw new ArgumentNullException(nameof(content));

            using (MemoryStream ms = new MemoryStream(content))
            {
                return await parent.CreateFileAsync(name, ms, ms.Length, ct).ConfigureAwait(false);
            }
        }

        public static async Task<byte[]> GetContentBytesAsync(this IFileInfo fi, CancellationToken ct)
        {
            if (fi == null) throw new ArgumentNullException(nameof(fi));

            using (var stream = await fi.OpenReadAsync(ct))
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms, 81920, ct);
                return ms.ToArray();
            }
        }
    }
}