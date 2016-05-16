using System;

namespace Meziantou.OneDrive
{
    public class Photo
    {
        public DateTime TakenDateTime { get; set; }
        public string CameraMake { get; set; }
        public string CameraModel { get; set; }
        public double FNumber { get; set; }
        public double ExposureDenominator { get; set; }
        public double ExposureNumerator { get; set; }
        public double FocalLength { get; set; }
        public long Iso { get; set; }
    }
}