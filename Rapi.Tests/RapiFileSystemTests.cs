using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreRPC.Transport.Http;
using Xunit;

namespace Rapi.Tests
{
    public class RapiFileSystemTests : IClassFixture<RapiTestHost>
    {
        private readonly RapiTestHost _host;

        public RapiFileSystemTests(RapiTestHost host) => _host = host;

        [Fact]
        public async Task ShouldReadFilesInSolutionDirectory()
        {
            var assembly = typeof(RapiFileSystemTests).Assembly.Location;
            var directory = Path.GetDirectoryName(assembly);
            var actual = Directory.GetFiles(directory);
            
            var connection = await RapiConnection.Connect(new HttpClientTransport(_host.Address));
            var received = await connection.FileSystem.GetFiles(directory);
            
            Assert.True(actual.All(file => received.Contains(file)));
        }

        [Theory]
        [InlineData("should_operate_on_directories")]
        [InlineData("folder name with spaces")]
        [InlineData("поддержка_кириллицы")]
        public async Task ShouldCreateAndRemoveDirectories(string directoryName)
        {
            var (connection, root) = await Connect();
            var directory = connection.Path.Combine(root, directoryName);
            var before = await connection.FileSystem.DirectoryExists(directory);
            Assert.False(before);

            await connection.FileSystem.CreateDirectory(directory);
            var after = await connection.FileSystem.DirectoryExists(directory);
            Assert.True(after);

            var directories = await connection.FileSystem.GetDirectories(root);
            var listed = directories.Any(path => directory == path);
            Assert.True(listed);
        }

        [Theory]
        [InlineData("should_write_files.txt", "Hello, world!")]
        [InlineData("file name with spaces.txt", "Hello, world!")]
        [InlineData("поддержка_кириллицы.txt", "Привет, мир!")]
        public async Task ShouldWriteAndReadFiles(string fileName, string fileContents)
        {
            var (connection, root) = await Connect();
            var file = connection.Path.Combine(root, fileName);

            var before = await connection.FileSystem.FileExists(file);
            Assert.False(before);
            
            await connection.FileSystem.WriteFileContents(file, Encoding.UTF8.GetBytes(fileContents));
            var after = await connection.FileSystem.FileExists(file);
            Assert.True(after);
            
            var read = await connection.FileSystem.ReadFileContents(file);
            var actual = Encoding.UTF8.GetString(read);
            Assert.Equal(fileContents, actual);
        }

        [Fact]
        public async Task ShouldCleanDirectory()
        {
            var (connection, root) = await Connect();
            var directory = connection.Path.Combine(root, "to_clean");
            
            await connection.FileSystem.CreateDirectory(directory);
            var directoryExists = await connection.FileSystem.DirectoryExists(directory);
            Assert.True(directoryExists);

            var file = connection.Path.Combine(directory, "example");
            await connection.FileSystem.WriteFileContents(file, Encoding.UTF8.GetBytes("42"));
            var fileExists = await connection.FileSystem.FileExists(file);
            Assert.True(fileExists);

            var before = await connection.FileSystem.GetFiles(directory);
            Assert.NotEmpty(before);
            
            await connection.FileSystem.CleanDirectory(directory);
            var after = await connection.FileSystem.GetFiles(directory);
            Assert.Empty(after);
        }
        
        [Fact]
        public async Task ShouldUnzipSimpleZipArchives()
        {
            var (connection, root) = await Connect();
            var toDirectory = connection.Path.Combine(root, "uploading_into");
            
            await connection.FileSystem.CreateDirectory(toDirectory);
            var exists = await connection.FileSystem.DirectoryExists(toDirectory);
            Assert.True(exists);

            var location = typeof(RapiFileSystemTests).Assembly.Location;
            var uploadFrom = Path.GetDirectoryName(location);
            var archiveName = Path.Combine(uploadFrom, "chrome.zip");

            await connection.FileSystem.Unzip(archiveName, toDirectory);
            var uploadedDirectory = connection.Path.Combine(toDirectory, "chrome-linux");
            var directory = await connection.FileSystem.DirectoryExists(uploadedDirectory);
            Assert.True(directory);

            var uploadedFile = connection.Path.Combine(uploadedDirectory, "product_logo_48.png");
            var file = await connection.FileSystem.FileExists(uploadedFile);
            Assert.True(file);
        }

        [Fact]
        public async Task ShouldRemoveFiles()
        {
            var (connection, root) = await Connect();
            var file = connection.Path.Combine(root, "delete_file_sample");
            await connection.FileSystem.WriteFileContents(file, Encoding.UTF8.GetBytes("42"));
            var before = await connection.FileSystem.FileExists(file);
            Assert.True(before);

            await connection.FileSystem.DeleteFile(file);
            var after = await connection.FileSystem.FileExists(file);
            Assert.False(after);
        }

        private async Task<(RapiConnection Connection, string Root)> Connect()
        {
            var connection = await RapiConnection.Connect(new HttpClientTransport(_host.Address));
            var root = connection.Path.Combine(connection.FileSystemInfo.TempDirectory, _host.DirectoryName);
            
            if (!await connection.FileSystem.DirectoryExists(root))
                await connection.FileSystem.CreateDirectory(root);
            
            return (connection, root);
        }
    }
}