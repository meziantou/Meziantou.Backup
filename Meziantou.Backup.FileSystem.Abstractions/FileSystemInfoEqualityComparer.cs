using System;
using System.Collections.Generic;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public class FileSystemInfoEqualityComparer : IEqualityComparer<IFileSystemInfo>
    {
        public StringComparison StringComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

        public bool Equals(IFileSystemInfo x, IFileSystemInfo y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            if (x.IsDirectory != y.IsDirectory)
                return false;
            
            if (!string.Equals(x.Name, y.Name, StringComparison))
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