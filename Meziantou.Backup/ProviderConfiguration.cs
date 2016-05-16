using System;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup
{
    public class ProviderConfiguration
    {
        public string ProviderName { get; set; }
        public string Configuration { get; set; }
        public string Path { get; set; }

        public IFileSystem CreateProvider()
        {
            var type = Type.GetType(ProviderName);
            if (type == null)
                return null; // TODO throw exception

            var provider = (IFileSystem)Activator.CreateInstance(type);
            provider.Initialize(Configuration);
            return provider;
        }
    }
}