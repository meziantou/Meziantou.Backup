namespace Meziantou.Backup.FileSystem.Abstractions
{
    public interface IHashProvider
    {
        byte[] GetHash(string algorithmName);
    }
}
