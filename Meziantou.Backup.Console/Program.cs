using System;
using System.Diagnostics;

namespace Meziantou.Backup.Console
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                var console = new BackupConsole();
                console.Main(args);
                return 0;
            }
            else
            {
                try
                {
                    var console = new BackupConsole();
                    console.Main(args);
                    return 0;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex);
                    return 1;
                }
            }
        }
    }
}
