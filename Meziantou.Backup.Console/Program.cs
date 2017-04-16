using System;
using System.Diagnostics;

namespace Meziantou.Backup.Console
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                var console = new BackupConsole();
                console.Main(args);
            }
            else
            {
                try
                {
                    var console = new BackupConsole();
                    console.Main(args);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex);
                }
            }
        }
    }
}
