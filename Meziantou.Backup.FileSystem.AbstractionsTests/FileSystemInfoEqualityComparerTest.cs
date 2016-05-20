using System;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Meziantou.Backup.FileSystem.AbstractionsTests
{
    [TestClass]
    public class FileSystemInfoEqualityComparerTest
    {
        private class TestFileSystemInfo : IFileSystemInfo
        {
            public bool IsDirectory { get; set; }
            public string Name { get; set; }
            public bool Exists { get; set; }
            public DateTime CreationTimeUtc { get; set; }
            public DateTime LastWriteTimeUtc { get; set; }

            public Task DeleteAsync(CancellationToken ct)
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void Equals01()
        {
            // Arrange
            FileSystemInfoEqualityComparer comparer = new FileSystemInfoEqualityComparer();

            var item1 = new TestFileSystemInfo();
            item1.Name = "Sample";
            item1.Exists = true;
            item1.IsDirectory = true;
            item1.CreationTimeUtc = new DateTime(2016, 05, 19, 10, 0, 0, DateTimeKind.Utc);
            item1.LastWriteTimeUtc = new DateTime(2016, 05, 19, 10, 50, 0, DateTimeKind.Utc);

            var item2 = new TestFileSystemInfo();
            item2.Name = "sample";
            item2.Exists = true;
            item2.IsDirectory = true;
            item2.CreationTimeUtc = new DateTime(2016, 05, 19, 10, 0, 0, DateTimeKind.Utc);
            item2.LastWriteTimeUtc = new DateTime(2016, 05, 19, 10, 50, 0, DateTimeKind.Utc);

            // Act
            var result = comparer.Equals(item1, item2);

            // Assert
            Assert.AreEqual(true, result);
        }
        
        [TestMethod]
        public void Equals02()
        {
            // Arrange
            FileSystemInfoEqualityComparer comparer = new FileSystemInfoEqualityComparer();

            var item1 = new TestFileSystemInfo();
            item1.Name = "Sample";
            item1.Exists = true;
            item1.IsDirectory = true;
            item1.CreationTimeUtc = new DateTime(2016, 05, 19, 10, 0, 0, DateTimeKind.Utc);
            item1.LastWriteTimeUtc = new DateTime(2016, 05, 19, 10, 50, 0, DateTimeKind.Utc);

            var item2 = new TestFileSystemInfo();
            item2.Name = "sample2";
            item2.Exists = true;
            item2.IsDirectory = true;
            item2.CreationTimeUtc = new DateTime(2016, 05, 19, 10, 0, 0, DateTimeKind.Utc);
            item2.LastWriteTimeUtc = new DateTime(2016, 05, 19, 10, 50, 0, DateTimeKind.Utc);

            // Act
            var result = comparer.Equals(item1, item2);

            // Assert
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void Equals03()
        {
            // Arrange
            FileSystemInfoEqualityComparer comparer = new FileSystemInfoEqualityComparer();

            var item1 = new TestFileSystemInfo();
            item1.Name = "Sample";
            item1.Exists = true;
            item1.IsDirectory = false;
            item1.CreationTimeUtc = new DateTime(2016, 05, 19, 10, 0, 0, DateTimeKind.Utc);
            item1.LastWriteTimeUtc = new DateTime(2016, 05, 19, 10, 50, 0, DateTimeKind.Utc);

            var item2 = new TestFileSystemInfo();
            item2.Name = "sample";
            item2.Exists = true;
            item2.IsDirectory = true;
            item2.CreationTimeUtc = new DateTime(2016, 05, 19, 10, 0, 0, DateTimeKind.Utc);
            item2.LastWriteTimeUtc = new DateTime(2016, 05, 19, 10, 50, 0, DateTimeKind.Utc);

            // Act
            var result = comparer.Equals(item1, item2);

            // Assert
            Assert.AreEqual(false, result);
        }
    }
}
