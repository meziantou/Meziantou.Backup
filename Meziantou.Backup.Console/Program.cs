using System;
using System.Threading;
using Meziantou.Backup.FileSystem.Abstractions;
using Meziantou.Backup.FileSystem.OneDrive;
using Meziantou.Backup.FileSystem.Physical;

namespace Meziantou.Backup.Console
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Backup backup = new Backup();
            backup.Action += Backup_Action;
            backup.SourceProviderConfiguration = new ProviderConfiguration
            {
                ProviderName = typeof(PhysicalFileSystem).AssemblyQualifiedName,
                Path = @"C:\Users\meziantou\Desktop\Publish\"
            };

            //backup.TargetProviderConfiguration = new ProviderConfiguration()
            //{
            //    ProviderName = typeof(PhysicalDrive).FullName,
            //    Path = @"C:\Users\meziantou\Desktop\Publish - backup\"
            //};

            backup.TargetProviderConfiguration = new ProviderConfiguration()
            {
                ProviderName = typeof(OneDriveFileSystem).AssemblyQualifiedName,
                Path = @"/Backup - Sample/Test1/sub/"
            };

            var backupAsync = backup.BackupAsync(CancellationToken.None);
            var awaiter = backupAsync.GetAwaiter();
            awaiter.GetResult();
        }

        private static void Backup_Action(object sender, BackupActionEventArgs e)
        {
            System.Console.WriteLine($"{e.Action}: <{GetDisplayName(e.SourceItem)}> -> <{GetDisplayName(e.TargetItem)}>");
        }

        private static string GetDisplayName(IFileSystemInfo item)
        {
            var fullName = item as IFullName;
            if (fullName != null)
                return fullName.FullName;

            return item?.Name;
        }
    }
}
