using System;
using System.Globalization;

namespace Meziantou.Backup.FileSystem.Abstractions
{
    public class HistoryFile : IComparable<HistoryFile>, IComparable
    {
        private static StringComparison _fileNameComparison = StringComparison.OrdinalIgnoreCase;
        private string _fileName;
        public const string FileExtension = ".backuphistory";
        private const string DateTimeFormat = "yyyyMMddhhmmss";

        public IFileInfo File { get; }
        public DateTime DateTime { get; set; }

        public string FileName
        {
            get
            {
                if (_fileName == null)
                    return File.Name;

                return _fileName;
            }
            private set { _fileName = value; }
        }

        public HistoryFile(IFileInfo file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            File = file;
        }

        public string ComputeFileName()
        {
            return ComputeFileName(FileName, DateTime);
        }

        public static string ComputeFileName(string fileName, DateTime dateTime)
        {
            return fileName + "." + dateTime.ToString(DateTimeFormat) + FileExtension;
        }

        public static HistoryFile Parse(IFileInfo file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            string fileName = file.Name;
            if (!fileName.EndsWith(FileExtension, _fileNameComparison))
                return null;

            fileName = fileName.Substring(0, fileName.Length - FileExtension.Length);
            var index = fileName.LastIndexOf('.');
            if (index < 0)
                return null;

            string date = fileName.Substring(index + 1);
            fileName = fileName.Substring(0, index);

            if (DateTime.TryParseExact(date, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                return new HistoryFile(file) { DateTime = dateTime, FileName = fileName };

            return null;
        }

        public bool IsSame(IFileInfo fileInfo)
        {
            return fileInfo != null && string.Equals(fileInfo.Name, FileName, _fileNameComparison);
        }

        public int CompareTo(HistoryFile other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var dateTimeComparison = DateTime.CompareTo(other.DateTime);
            if (dateTimeComparison != 0) return dateTimeComparison;
            return string.Compare(FileName, other.FileName, _fileNameComparison);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            if (!(obj is HistoryFile)) throw new ArgumentException($"Object must be of type {nameof(HistoryFile)}");
            return CompareTo((HistoryFile)obj);
        }
    }
}
