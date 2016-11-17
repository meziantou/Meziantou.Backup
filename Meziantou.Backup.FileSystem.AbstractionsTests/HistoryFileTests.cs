using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Meziantou.Backup.FileSystem.AbstractionsTests
{
    [TestClass]
    public class HistoryFileTests
    {
        private class FileInfo : IFileInfo
        {
            public FileInfo(string name)
            {
                Name = name;
            }

            public bool IsDirectory { get; }
            public string Name { get; }
            public DateTime CreationTimeUtc { get; }
            public DateTime LastWriteTimeUtc { get; }
            public Task DeleteAsync(CancellationToken ct)
            {
                throw new NotImplementedException();
            }

            public long Length { get; }
            public Task<Stream> OpenReadAsync(CancellationToken ct)
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void Parse01()
        {
            var historyFile = HistoryFile.Parse(new FileInfo("abc.txt"));
            Assert.AreEqual(null, historyFile);
        }

        [TestMethod]
        public void Parse02()
        {
            var historyFile = HistoryFile.Parse(new FileInfo("abc.backuphistory"));
            Assert.AreEqual(null, historyFile);
        }

        [TestMethod]
        public void Parse03()
        {
            var historyFile = HistoryFile.Parse(new FileInfo("abc.txt.20161117011122.backuphistory"));

            Assert.AreEqual("abc.txt", historyFile.FileName);
            Assert.AreEqual(new DateTime(2016, 11, 17, 1, 11, 22), historyFile.DateTime);
        }


        [TestMethod]
        public void ComputeFileName03()
        {
            var historyFile = new HistoryFile(new FileInfo("abc.txt"));
            historyFile.DateTime = new DateTime(2016, 11, 17, 1, 11, 22);

            string fileName = historyFile.ComputeFileName();

            Assert.AreEqual("abc.txt.20161117011122.backuphistory", fileName);
        }
    }
}
