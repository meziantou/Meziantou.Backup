using System.Threading;
using System.Threading.Tasks;
using Meziantou.Backup.FileSystem.Abstractions;
using Meziantou.Backup.FileSystem.Aes;
using Meziantou.Backup.FileSystem.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Meziantou.Backup.FileSystem.AesTests
{
    [TestClass]
    public class AesFileSystemTests
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            // Arrange
            var inMemoryFileSystem = new InMemoryFileSystem();
            var inMemoryRoot = await inMemoryFileSystem.GetOrCreateDirectoryItemAsync("", CancellationToken.None);

            var aesProvider = new AesFileSystem(inMemoryFileSystem);
            aesProvider.Version = AesVersion.Aes128;
            aesProvider.Password = "sample";
            
            var aesRoot = await aesProvider.GetOrCreateDirectoryItemAsync("", CancellationToken.None);

            // Act
            var encryptedFile = await aesRoot.CreateFileAsync("encrypted.png", new byte[] { 1, 2 }, CancellationToken.None);
            await inMemoryRoot.CopyFileAsync(encryptedFile, "decrypted.png", CancellationToken.None);

            var rawFile = inMemoryFileSystem.GetFile("encrypted.png");
            var rawEncryptedFile = await inMemoryRoot.CopyFileAsync(rawFile, "encrypted.png", CancellationToken.None);
            var rawDecryptedFile = await inMemoryRoot.CopyFileAsync(encryptedFile, "decrypted.png", CancellationToken.None);

            // Assert
            var rawDecryptedFileContent = await rawDecryptedFile.GetContentBytesAsync(CancellationToken.None);
            var rawEncryptedFileContent = await rawEncryptedFile.GetContentBytesAsync(CancellationToken.None);

            Assert.AreEqual(33, rawEncryptedFileContent.Length);
            Assert.AreEqual(2, rawDecryptedFileContent.Length);
            CollectionAssert.AreEquivalent(new byte[] { 1, 2 }, rawDecryptedFileContent);
        }
    }
}
