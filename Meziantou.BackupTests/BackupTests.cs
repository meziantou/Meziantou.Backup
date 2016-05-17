using System;
using System.Threading;
using Meziantou.Backup;
using Meziantou.BackupTests.InMemoryFileSystem;
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
            var sourceProvider = new FileSystem();
            sourceProvider.AddItem("item.png");

            var targetProvider = new FileSystem();

            var sourceProviderConfiguration = new ProviderConfiguration { Provider = sourceProvider };
            var targetProviderConfiguration = new ProviderConfiguration { Provider = targetProvider };

            var backup = new Backup.Backup();

            // Act
            backup.RunAsync(sourceProviderConfiguration, targetProviderConfiguration, CancellationToken.None).Wait();

            // Assert
            Assert.IsTrue(targetProvider.HasItem("item.png"));
        }

        [TestMethod]
        public void TestMethod2()
        {
            // Arrange
            var sourceProvider = new FileSystem();

            var targetProvider = new FileSystem();
            targetProvider.AddItem("item.png");

            var sourceProviderConfiguration = new ProviderConfiguration { Provider = sourceProvider };
            var targetProviderConfiguration = new ProviderConfiguration { Provider = targetProvider };

            var backup = new Backup.Backup();
            backup.CanDeleteFiles = true;

            // Act
            backup.RunAsync(sourceProviderConfiguration, targetProviderConfiguration, CancellationToken.None).Wait();

            // Assert
            Assert.IsFalse(targetProvider.HasItem("item.png"));
        }

        [TestMethod]
        public void TestMethod3()
        {
            // Arrange
            var sourceProvider = new FileSystem();
            sourceProvider.AddItem("Sample/item.png");
            sourceProvider.AddItem("Sample/item2.png");
            sourceProvider.AddItem("Sample/item3.png");
            sourceProvider.AddItem("Sample/sub1/item1"); // item
            sourceProvider.AddItem("Sample/sub1/item1.txt");
            sourceProvider.AddItem("Sample/sub2/");

            var targetProvider = new FileSystem();
            targetProvider.AddItem("Sample/item.png");
            targetProvider.AddItem("Sample/item2.png");
            targetProvider.AddItem("Sample/item4.png");
            targetProvider.AddItem("Sample/sub1/item1");

            var sourceProviderConfiguration = new ProviderConfiguration { Provider = sourceProvider };
            var targetProviderConfiguration = new ProviderConfiguration { Provider = targetProvider };

            var backup = new Backup.Backup();

            // Act
            backup.RunAsync(sourceProviderConfiguration, targetProviderConfiguration, CancellationToken.None).Wait();

            // Assert
            Assert.IsTrue(targetProvider.HasItem("Sample/item.png"));
            Assert.IsTrue(targetProvider.HasItem("Sample/item2.png"));
            Assert.IsTrue(targetProvider.HasItem("Sample/item3.png"));
            Assert.IsTrue(targetProvider.HasItem("Sample/item4.png"));
            Assert.IsTrue(targetProvider.HasItem("Sample/sub1/item1"));
            Assert.IsTrue(targetProvider.HasItem("Sample/sub1/item1.txt"));
            Assert.IsTrue(targetProvider.HasItem("Sample/sub2/"));
        }
    }
}
