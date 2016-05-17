using System;
using System.Collections.Generic;
using Meziantou.Backup.FileSystem.Abstractions;

namespace Meziantou.Backup
{
    public class ProviderConfiguration
    {
        public IFileSystem Provider { get; set; }
        public string ProviderName { get; set; }
        public IDictionary<string, object> Configuration { get; set; }
        public string Path { get; set; }

        public IFileSystem CreateProvider()
        {
            if (Provider != null)
                return Provider;

            var type = GetType(ProviderName);
            var provider = (IFileSystem)CreateInstance(type);
            provider.Initialize(Configuration);
            return provider;
        }

        protected virtual object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        protected virtual Type GetType(string name)
        {
            if (string.Equals("FileSystem", name, StringComparison.InvariantCultureIgnoreCase))
                return Type.GetType("Meziantou.Backup.FileSystem.Physical.PhysicalFileSystem, Meziantou.Backup.FileSystem.Physical");

            if (string.Equals("OneDrive", name, StringComparison.InvariantCultureIgnoreCase))
                return Type.GetType("Meziantou.Backup.FileSystem.OneDrive.OneDriveFileSystem, Meziantou.Backup.FileSystem.OneDrive");

            return Type.GetType(name, true);
        }
    }
}