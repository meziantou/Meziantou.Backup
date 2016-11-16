using Microsoft.Extensions.CommandLineUtils;

namespace Meziantou.Backup.Console
{
    internal static class CommandLineApplicationExtensions
    {
        public static CommandOption HelpOption(this CommandLineApplication app)
        {
            return app.HelpOption("-?|-h|--help");
        }
    }
}