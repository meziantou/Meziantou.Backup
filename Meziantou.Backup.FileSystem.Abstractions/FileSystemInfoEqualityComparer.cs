using System;
using System.Collections.Generic;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public sealed class FileSystemInfoEqualityComparer : IEqualityComparer<IFileSystemInfo>
    {
        public StringComparer StringComparer { get; set; } = StringComparer.OrdinalIgnoreCase;

        public bool Equals(IFileSystemInfo x, IFileSystemInfo y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            if (x.IsDirectory != y.IsDirectory)
                return false;

            if (!StringComparer.Equals(x.Name, y.Name))
                return false;

            return true;
        }

        public int GetHashCode(IFileSystemInfo obj)
        {
            if (obj?.Name == null)
                return 0;

            return StringComparer.GetHashCode(obj.Name);
        }
    }
}