using System;

namespace Meziantou.Backup
{
    [Flags]
    public enum FileInfoEqualityMethods
    {
        None = 0x0,
        Length = 0x1,
        LastWriteTime = 0x2,
        Content = 0x4,
        ContentMd5 = 0x8,
        ContentSha1 = 0x10,
        ContentSha256 = 0x20,
        ContentSha512 = 0x40,

        Default = LastWriteTime | Length,
    }
}