using System;
using System.Collections.Generic;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public class ItemEqualityComparer : IEqualityComparer<IFileSystemInfo>
    {
        public bool Equals(IFileSystemInfo x, IFileSystemInfo y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            if (!string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            if (x is IFileInfo && !(y is IFileInfo))
                return false;

            if (x is IDirectoryInfo && !(y is IDirectoryInfo))
                return false;

            return true;
        }

        public int GetHashCode(IFileSystemInfo obj)
        {
            if (obj?.Name == null)
                return 0;

            return obj.Name.GetHashCode();
        }
    }
}