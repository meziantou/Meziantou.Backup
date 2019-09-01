using System;

namespace Meziantou.Backup.Console
{
    [Flags]
    internal enum ConsoleLoggerLevel
    {
        None = 0x0,
        Error = 0x1,
        FileCopyProgress = 0x2,
        FileCreating = 0x4,
        FileUpdating = 0x8,
        FileDeleting = 0x10,
        DirectoryCreating = 0x20,
        DirectoryDeleting = 0x40,
        DirectorySynchronizing = 0x40,
        FullPath = 0x80,

        Default = Error | FileCreating | FileUpdating | FileDeleting | DirectoryCreating | DirectoryDeleting,
    }
}