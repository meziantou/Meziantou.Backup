namespace Meziantou.OneDrive
{
    public class ThumbnailSet
    {
        public string Id { get; set; }
        public Thumbnail Small { get; set; }
        public Thumbnail Medium { get; set; }
        public Thumbnail Large { get; set; }
        public Thumbnail Source { get; set; }
    }
}