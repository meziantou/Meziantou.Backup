using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public static class ItemExtensions
    {
        private static readonly ItemEqualityComparer EqualityComparer = new ItemEqualityComparer();

        public static T Get<T>(this IEnumerable<T> items, T item) where T : IFileSystemInfo
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            return items.FirstOrDefault(_ => EqualityComparer.Equals(item, _));
        }

        public static async Task<IFileInfo> CopyFileAsync(this IDirectoryInfo directory, IFileInfo file, CancellationToken ct)
        {
            if (directory == null) throw new ArgumentNullException(nameof(directory));
            if (file == null) throw new ArgumentNullException(nameof(file));

            using (var inputStream = await file.OpenReadAsync(ct))
            {
                return await directory.CreateFileAsync(file.Name, inputStream, file.Length, ct);
            }
        }
    }
}