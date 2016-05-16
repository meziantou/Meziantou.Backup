namespace Meziantou.OneDrive
{
    public class RemoveItem
    {
        public string Id { get; set; }
        public ItemReference ParentReference { get; set; }
        public Folder Folder { get; set; }
        public File File { get; set; }
        public FileSystemInfo FileSystemInfo { get; set; }
        public long Size { get; set; }
        public string Name { get; set; }
    }
}