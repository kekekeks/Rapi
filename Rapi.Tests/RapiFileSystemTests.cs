using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rapi.Tests
{
    [TestFixture]
    public class RapiFileSystemTests
    {
        [Test]
        public async Task ShouldReadFilesInSolutionDirectory()
        {
            var assembly = typeof(RapiFileSystemTests).Assembly.Location;
            var directory = Path.GetDirectoryName(assembly)!;
            var actual = Directory.GetFiles(directory);

            var connection = await RapiConnection.Connect(RapiTestHost.Address);
            var received = await connection.FileSystem.GetFiles(directory);

            Assert.That(actual.All(file => received.Contains(file)), Is.True);
        }

        [TestCase("should_operate_on_directories")]
        [TestCase("folder name with spaces")]
        [TestCase("поддержка_кириллицы")]
        public async Task ShouldCreateAndRemoveDirectories(string directoryName)
        {
            var (connection, root) = await Connect();
            var directory = connection.Path.Combine(root, directoryName);
            var before = await connection.FileSystem.DirectoryExists(directory);
            Assert.That(before, Is.False);

            await connection.FileSystem.CreateDirectory(directory);
            var after = await connection.FileSystem.DirectoryExists(directory);
            Assert.That(after, Is.True);

            var directories = await connection.FileSystem.GetDirectories(root);
            var listed = directories.Any(path => directory == path);
            Assert.That(listed, Is.True);
        }

        [TestCase("should_write_files.txt", "Hello, world!")]
        [TestCase("file name with spaces.txt", "Hello, world!")]
        [TestCase("поддержка_кириллицы.txt", "Привет, мир!")]
        public async Task ShouldWriteAndReadFiles(string fileName, string fileContents)
        {
            var (connection, root) = await Connect();
            var file = connection.Path.Combine(root, fileName);

            var before = await connection.FileSystem.FileExists(file);
            Assert.That(before, Is.False);

            await connection.FileSystem.WriteFileContents(file, Encoding.UTF8.GetBytes(fileContents));
            var after = await connection.FileSystem.FileExists(file);
            Assert.That(after, Is.True);

            var read = await connection.FileSystem.ReadFileContents(file);
            var actual = Encoding.UTF8.GetString(read);
            Assert.That(actual, Is.EqualTo(fileContents));
        }

        [Test]
        public async Task ShouldCleanDirectory()
        {
            var (connection, root) = await Connect();
            var directory = connection.Path.Combine(root, "to_clean");

            await connection.FileSystem.CreateDirectory(directory);
            var directoryExists = await connection.FileSystem.DirectoryExists(directory);
            Assert.That(directoryExists, Is.True);

            var file = connection.Path.Combine(directory, "example");
            await connection.FileSystem.WriteFileContents(file, Encoding.UTF8.GetBytes("42"));
            var fileExists = await connection.FileSystem.FileExists(file);
            Assert.That(fileExists, Is.True);

            var before = await connection.FileSystem.GetFiles(directory);
            Assert.That(before, Is.Not.Empty);

            await connection.FileSystem.CleanDirectory(directory);
            var after = await connection.FileSystem.GetFiles(directory);
            Assert.That(after, Is.Empty);
        }

        [Test]
        public async Task ShouldUnzipSimpleZipArchives()
        {
            var (connection, root) = await Connect();
            var toDirectory = connection.Path.Combine(root, "uploading_into");

            await connection.FileSystem.CreateDirectory(toDirectory);
            var exists = await connection.FileSystem.DirectoryExists(toDirectory);
            Assert.That(exists, Is.True);

            var location = typeof(RapiFileSystemTests).Assembly.Location;
            var uploadFrom = Path.GetDirectoryName(location)!;
            var archiveName = Path.Combine(uploadFrom, "chrome.zip");

            await connection.FileSystem.Unzip(archiveName, toDirectory);
            var uploadedDirectory = connection.Path.Combine(toDirectory, "chrome-linux");
            var directory = await connection.FileSystem.DirectoryExists(uploadedDirectory);
            Assert.That(directory, Is.True);

            var uploadedFile = connection.Path.Combine(uploadedDirectory, "product_logo_48.png");
            var fileExists = await connection.FileSystem.FileExists(uploadedFile);
            Assert.That(fileExists, Is.True);
        }

        [Test]
        public async Task ShouldRemoveFiles()
        {
            var (connection, root) = await Connect();
            var file = connection.Path.Combine(root, "delete_file_sample");
            await connection.FileSystem.WriteFileContents(file, Encoding.UTF8.GetBytes("42"));
            var before = await connection.FileSystem.FileExists(file);
            Assert.That(before, Is.True);

            await connection.FileSystem.DeleteFile(file);
            var after = await connection.FileSystem.FileExists(file);
            Assert.That(after, Is.False);
        }

        private static async Task<(RapiConnection Connection, string Root)> Connect()
        {
            var connection = await RapiConnection.Connect(RapiTestHost.Address);
            var root = connection.Path.Combine(connection.FileSystemInfo.TempDirectory!, RapiTestHost.DirectoryName);

            if (!await connection.FileSystem.DirectoryExists(root))
                await connection.FileSystem.CreateDirectory(root);

            return (connection, root);
        }
    }
}
