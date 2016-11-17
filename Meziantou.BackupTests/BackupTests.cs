using System;
using System.Linq;
using System.Threading;
using Meziantou.Backup;
using Meziantou.Backup.FileSystem.Abstractions;
using Meziantou.Backup.FileSystem.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Meziantou.BackupTests
{
    [TestClass]
    public class BackupTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Arrange
            var sourceProvider = new InMemoryFileSystem();
            sourceProvider.AddItem("item.png");

            var targetProvider = new InMemoryFileSystem();

            var backup = new Backup.Backup();

            // Act
            backup.RunAsync(sourceProvider, targetProvider, CancellationToken.None).Wait();

            // Assert
            Assert.IsTrue(targetProvider.HasItem("item.png"));
        }

        [TestMethod]
        public void TestMethod2()
        {
            // Arrange
            var sourceProvider = new InMemoryFileSystem();

            var targetProvider = new InMemoryFileSystem();
            targetProvider.AddItem("item.png");

            var backup = new Backup.Backup();
            backup.CanDeleteFiles = true;

            // Act
            backup.RunAsync(sourceProvider, targetProvider, CancellationToken.None).Wait();

            // Assert
            Assert.IsFalse(targetProvider.HasItem("item.png"));
        }

        [TestMethod]
        public void TestMethod3()
        {
            // Arrange
            var sourceProvider = new InMemoryFileSystem();
            sourceProvider.AddItem("Sample/item.png");
            sourceProvider.AddItem("Sample/item2.png");
            sourceProvider.AddItem("Sample/item3.png");
            sourceProvider.AddItem("Sample/sub1/item1"); // item
            sourceProvider.AddItem("Sample/sub1/item1.txt");
            sourceProvider.AddItem("Sample/sub2/");

            var targetProvider = new InMemoryFileSystem();
            targetProvider.AddItem("Sample/item.png");
            targetProvider.AddItem("Sample/item2.png");
            targetProvider.AddItem("Sample/item4.png");
            targetProvider.AddItem("Sample/sub1/item1");

            var backup = new Backup.Backup();

            // Act
            backup.RunAsync(sourceProvider, targetProvider, CancellationToken.None).Wait();

            // Assert
            Assert.IsTrue(targetProvider.HasItem("Sample/item.png"));
            Assert.IsTrue(targetProvider.HasItem("Sample/item2.png"));
            Assert.IsTrue(targetProvider.HasItem("Sample/item3.png"));
            Assert.IsTrue(targetProvider.HasItem("Sample/item4.png"));
            Assert.IsTrue(targetProvider.HasItem("Sample/sub1/item1"));
            Assert.IsTrue(targetProvider.HasItem("Sample/sub1/item1.txt"));
            Assert.IsTrue(targetProvider.HasItem("Sample/sub2/"));
        }

        [TestMethod]
        public void TestMethod4()
        {
            // Arrange
            var sourceProvider = new InMemoryFileSystem();
            sourceProvider.AddItem("item.png", new byte[] { 1, 2 });

            var targetProvider = new InMemoryFileSystem();
            targetProvider.AddItem("item.png", new byte[] { 1, 2, 3 });

            var backup = new Backup.Backup();

            // Act
            backup.RunAsync(sourceProvider, targetProvider, CancellationToken.None).Wait();

            // Assert
            Assert.AreEqual(2, targetProvider.GetFile("item.png").Length); // Default comparison use length
        }

        [TestMethod]
        public void TestMethod5()
        {
            // Arrange
            var sourceProvider = new InMemoryFileSystem();
            sourceProvider.AddItem("item.png", new byte[] { 1, 2 });

            var targetProvider = new InMemoryFileSystem();
            targetProvider.AddItem("item.png", new byte[] { 1, 2, 3 });

            var backup = new Backup.Backup();
            backup.EqualityMethods = FileInfoEqualityMethods.Content;

            // Act
            backup.RunAsync(sourceProvider, targetProvider, CancellationToken.None).Wait();

            // Assert
            Assert.AreEqual(2, targetProvider.GetFile("item.png").Length);
        }

        [TestMethod]
        public void TestMethod6()
        {
            // Arrange

            // Same MD5: http://crypto.stackexchange.com/questions/15873/what-is-the-md5-collision-with-the-smallest-input-values

            var sourceFileContent = HexaToBytes("d131dd02c5e6eec4693d9a0698aff95c2fcab58712467eab4004583eb8fb7f8955ad340609f4b30283e488832571415a085125e8f7cdc99fd91dbdf280373c5bd8823e3156348f5bae6dacd436c919c6dd53e2b487da03fd02396306d248cda0e99f33420f577ee8ce54b67080a80d1ec69821bcb6a8839396f9652b6ff72a70");
            var targetFileContent = HexaToBytes("d131dd02c5e6eec4693d9a0698aff95c2fcab50712467eab4004583eb8fb7f8955ad340609f4b30283e4888325f1415a085125e8f7cdc99fd91dbd7280373c5bd8823e3156348f5bae6dacd436c919c6dd53e23487da03fd02396306d248cda0e99f33420f577ee8ce54b67080280d1ec69821bcb6a8839396f965ab6ff72a70");

            var sourceProvider = new InMemoryFileSystem();
            sourceProvider.AddItem("item.png", sourceFileContent);

            var targetProvider = new InMemoryFileSystem();

            targetProvider.AddItem("item.png", targetFileContent);

            var backup = new Backup.Backup();
            backup.EqualityMethods = FileInfoEqualityMethods.ContentMd5;

            // Act
            backup.RunAsync(sourceProvider, targetProvider, CancellationToken.None).Wait();

            // Assert
            CollectionAssert.AreEqual(targetFileContent, targetProvider.GetFile("item.png").Content);
        }

        [TestMethod]
        public void TestMethod7()
        {
            // Arrange

            // Same MD5: http://crypto.stackexchange.com/questions/15873/what-is-the-md5-collision-with-the-smallest-input-values

            var sourceFileContent = HexaToBytes("d131dd02c5e6eec4693d9a0698aff95c2fcab58712467eab4004583eb8fb7f8955ad340609f4b30283e488832571415a085125e8f7cdc99fd91dbdf280373c5bd8823e3156348f5bae6dacd436c919c6dd53e2b487da03fd02396306d248cda0e99f33420f577ee8ce54b67080a80d1ec69821bcb6a8839396f9652b6ff72a70");
            var targetFileContent = HexaToBytes("d131dd02c5e6eec4693d9a0698aff95c2fcab50712467eab4004583eb8fb7f8955ad340609f4b30283e4888325f1415a085125e8f7cdc99fd91dbd7280373c5bd8823e3156348f5bae6dacd436c919c6dd53e23487da03fd02396306d248cda0e99f33420f577ee8ce54b67080280d1ec69821bcb6a8839396f965ab6ff72a70");

            var sourceProvider = new InMemoryFileSystem();
            sourceProvider.AddItem("item.png", sourceFileContent);

            var targetProvider = new InMemoryFileSystem();

            targetProvider.AddItem("item.png", targetFileContent);

            var backup = new Backup.Backup();
            backup.EqualityMethods = FileInfoEqualityMethods.ContentMd5 | FileInfoEqualityMethods.ContentSha1;

            // Act
            backup.RunAsync(sourceProvider, targetProvider, CancellationToken.None).Wait();

            // Assert
            CollectionAssert.AreEqual(sourceFileContent, targetProvider.GetFile("item.png").Content);
        }

        [TestMethod]
        public void TestMethod8()
        {
            // Arrange
            var sourceProvider = new InMemoryFileSystem();
            sourceProvider.AddItem("Sample/item.png", new byte[] { 0 });
            sourceProvider.AddItem("Sample/item2.png");

            var targetProvider = new InMemoryFileSystem();
            targetProvider.AddItem("Sample/item.png", new byte[] { 1 }); // Different from source

            var backup = new Backup.Backup();
            backup.EqualityMethods = FileInfoEqualityMethods.Content;
            backup.KeepHistory = true;

            // Act
            backup.RunAsync(sourceProvider, targetProvider, CancellationToken.None).Wait();

            // Assert
            Assert.IsTrue(targetProvider.HasItem("Sample/item.png"));
            Assert.IsTrue(targetProvider.HasItem("Sample/item2.png"));
            var children = targetProvider.GetDirectory("/Sample/").Children;
            Assert.IsNotNull(children.First(f => f.IsFile() && f.Name.StartsWith("item.png.")));
        }

        [TestMethod]
        public void TestMethod9()
        {
            // Arrange
            var sourceProvider = new InMemoryFileSystem();
            sourceProvider.AddItem("Sample/item.png", new byte[] { 1 });
            sourceProvider.AddItem("Sample/item2.png");

            var targetProvider = new InMemoryFileSystem();
            targetProvider.AddItem("Sample/item.png", new byte[] { 0 });
            targetProvider.AddItem("Sample/item.png.20160101000000.backuphistory", new byte[] { 1 }); // Different from source

            var backup = new Backup.Backup();
            backup.EqualityMethods = FileInfoEqualityMethods.Content;
            backup.KeepHistory = true;

            // Act
            backup.RunAsync(sourceProvider, targetProvider, CancellationToken.None).Wait();

            // Assert
            Assert.IsTrue(targetProvider.HasItem("Sample/item.png"));
            Assert.IsTrue(targetProvider.HasItem("Sample/item.png.20160101000000.backuphistory"));
            Assert.IsTrue(targetProvider.HasItem("Sample/item2.png"));
            var children = targetProvider.GetDirectory("/Sample/").Children;
            Assert.AreEqual(3, children.Count);
        }
        
        [TestMethod]
        public void TestMethod10()
        {
            // Arrange
            var sourceProvider = new InMemoryFileSystem();
            sourceProvider.AddItem("Sample/item.png", new byte[] { 4 });

            var targetProvider = new InMemoryFileSystem();
            targetProvider.AddItem("Sample/item.png", new byte[] { 0 });
            targetProvider.AddItem("Sample/item.png.20160101000000.backuphistory", new byte[] { 1 });
            targetProvider.AddItem("Sample/item.png.20160102000000.backuphistory", new byte[] { 2 });
            targetProvider.AddItem("Sample/item.png.20160103000000.backuphistory", new byte[] { 3 });

            var backup = new Backup.Backup();
            backup.EqualityMethods = FileInfoEqualityMethods.Content;
            backup.KeepHistory = true;

            // Act
            backup.RunAsync(sourceProvider, targetProvider, CancellationToken.None).Wait();

            // Assert
            Assert.IsTrue(targetProvider.HasItem("Sample/item.png"));
            var children = targetProvider.GetDirectory("/Sample/").Children;
            Assert.AreEqual(5, children.Count);
        }

        private static byte[] HexaToBytes(string str)
        {
            return Enumerable.Range(0, str.Length / 2).Select(x => Convert.ToByte(str.Substring(x * 2, 2), 16)).ToArray();
        }
    }
}
