using System.Text;
using System.Threading.Tasks;
using CoreRPC.Transport.Http;
using Xunit;
using Xunit.Abstractions;

namespace Rapi.Tests
{
    public class RapiSftpTests : IClassFixture<RapiTestHost>
    {
        private readonly ITestOutputHelper _output;
        private readonly RapiTestHost _host;

        public RapiSftpTests(RapiTestHost host, ITestOutputHelper output)
        {
            _output = output;
            _host = host;
        }
        
        [Fact]
        public async Task ShouldDownloadFilesViaSftp()
        {
            var (connection, root) = await Connect();
            var input = connection.Path.Combine(root, "should_download_sftp_in");
            await connection.FileSystem.WriteFileContents(input, Encoding.UTF8.GetBytes("super secret"));
            _output.WriteLine("should_download_sftp_in contents successfully written.");

            var output = connection.Path.Combine(root, "should_download_sftp_out");
            _output.WriteLine($"Downloading file from {input} to {output}");
            await connection.Sftp.Download(input, output, _host.Configuration.Sftp);

            var downloaded = await connection.FileSystem.ReadFileContents(output);
            var words = Encoding.UTF8.GetString(downloaded);
            Assert.Equal("super secret", words);
        }
        
        [Fact]
        public async Task ShouldUploadFilesViaSftp()
        {
            var (connection, root) = await Connect();
            var input = connection.Path.Combine(root, "should_upload_sftp_in");
            await connection.FileSystem.WriteFileContents(input, Encoding.UTF8.GetBytes("super secret"));
            _output.WriteLine("should_upload_sftp_in contents successfully written.");

            var output = connection.Path.Combine(root, "should_upload_sftp_out");
            _output.WriteLine($"Uploading file from {input} to {output}");
            await connection.Sftp.Upload(input, output, _host.Configuration.Sftp);

            var downloaded = await connection.FileSystem.ReadFileContents(output);
            var words = Encoding.UTF8.GetString(downloaded);
            Assert.Equal("super secret", words);
        }
        
        [Fact]
        public async Task ShouldDownloadDirectoriesViaSftp()
        {
            var (rapi, root) = await Connect();
            var input = rapi.Path.Combine(root, "should_download_dir_in");
            var sample = Encoding.UTF8.GetBytes("42");
            
            /**
             * Here we build the following file tree:
             *
             * should_download_dir_in/
             * |-- sample_1
             * |   sample_2
             * |   sample_dir/
             * |   |-- sample_3
             * |   +-- sample_4
             * +-- sample_empty_dir/
             */
            
            _output.WriteLine("Building file tree...");
            await rapi.FileSystem.CreateDirectory(input);
            await rapi.FileSystem.CreateDirectory(rapi.Path.Combine(input, "sample_dir"));
            await rapi.FileSystem.CreateDirectory(rapi.Path.Combine(input, "sample_dir", "sample_nested_dir"));
            await rapi.FileSystem.CreateDirectory(rapi.Path.Combine(input, "sample_empty_dir"));
            foreach (var file in new[]
            {
                rapi.Path.Combine(input, "sample_1"),
                rapi.Path.Combine(input, "sample_2"),
                rapi.Path.Combine(input, "sample_dir", "sample_3"),
                rapi.Path.Combine(input, "sample_dir", "sample_4"),
                rapi.Path.Combine(input, "sample_dir", "sample_nested_dir", "sample_5")
            })
                await rapi.FileSystem.WriteFileContents(file, sample);

            _output.WriteLine("Downloading files...");
            var output = rapi.Path.Combine(root, "should_download_dir_out");
            await rapi.FileSystem.CreateDirectory(output);
            await rapi.Sftp.Download(input, output, _host.Configuration.Sftp);

            _output.WriteLine("Asserting all files, folders and contents are correct...");
            Assert.True(await rapi.FileSystem.DirectoryExists(rapi.Path.Combine(output, "sample_dir")));
            Assert.True(await rapi.FileSystem.DirectoryExists(rapi.Path.Combine(output, "sample_empty_dir")));
            foreach (var file in new[]
            {
                rapi.Path.Combine(output, "sample_1"),
                rapi.Path.Combine(output, "sample_2"),
                rapi.Path.Combine(output, "sample_dir", "sample_3"),
                rapi.Path.Combine(output, "sample_dir", "sample_4"),
                rapi.Path.Combine(output, "sample_dir", "sample_nested_dir", "sample_5")
            })
            {
                await rapi.FileSystem.FileExists(file);
                var bytes = await rapi.FileSystem.ReadFileContents(file);
                var words = Encoding.UTF8.GetString(bytes);
                Assert.Equal("42", words);
            }
            
            _output.WriteLine("Asserting file and folder count...");
            var files = await rapi.FileSystem.GetFiles(output);
            var directories = await rapi.FileSystem.GetDirectories(output);
            Assert.Equal(2, files.Count);
            Assert.Equal(2, directories.Count);
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