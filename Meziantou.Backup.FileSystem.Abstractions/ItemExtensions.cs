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

        public static async Task<IFileInfo> CopyFileAsync(this IDirectoryInfo directory, IFileInfo file, CancellationToken ct)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            if (file == null) throw new ArgumentNullException(nameof(file));

            using (var inputStream = await file.OpenReadAsync(ct).ConfigureAwait(false))
            {
                return await directory.CreateFileAsync(file.Name, inputStream, file.Length, ct).ConfigureAwait(false);
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
    }
}