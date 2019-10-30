using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CoreRPC.Transport.Http;
using Xunit;
using Xunit.Abstractions;

namespace Rapi.Tests
{
    public class RapiSftpTests : RapiSftpBase, IClassFixture<RapiTestHost>
    {
        public RapiSftpTests(RapiTestHost host, ITestOutputHelper output) : base(host, output, false)
        {
        }
    }
    
    public class RapiSftpBackgroundTests : RapiSftpBase, IClassFixture<RapiTestHost>
    {
        public RapiSftpBackgroundTests(RapiTestHost host, ITestOutputHelper output) : base(host, output, true)
        {
        }
    }

    
    public abstract class RapiSftpBase
    {
        private readonly ITestOutputHelper _output;
        private readonly bool _useBackgroundApi;
        private readonly RapiTestHost _host;

        public RapiSftpBase(RapiTestHost host, ITestOutputHelper output, bool useBackgroundApi)
        {
            _output = output;
            _useBackgroundApi = useBackgroundApi;
            _host = host;
        }

        async Task DoOperation(IRapiSftpRpc rpc, string from, string to, bool upload)
        {
            if (_useBackgroundApi)
            {
                var id = Guid.NewGuid().ToString();
                await (upload
                    ? rpc.StartUpload(id, from, to, _host.Configuration.Sftp)
                    : rpc.StartDownload(id, from, to, _host.Configuration.Sftp));
                while (true)
                {
                    var status = await rpc.TryGetStatus(id);
                    if (status.IsCompleted)
                    {
                        if (status.Exception != null)
                            throw new Exception(status.Exception);
                        return;
                    }

                    await Task.Delay(100);
                }
            }
            else
            {
                await (upload
                    ? rpc.Upload(from, to, _host.Configuration.Sftp)
                    : rpc.Download(from, to, _host.Configuration.Sftp));
            }
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
            await DoOperation(connection.Sftp, input, output, false);

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
            await DoOperation(connection.Sftp, input, output, true);

            var downloaded = await connection.FileSystem.ReadFileContents(output);
            var words = Encoding.UTF8.GetString(downloaded);
            Assert.Equal("super secret", words);
        }
        
        [Fact]
        public async Task ShouldDownloadDirectoriesViaSftp()
        {
            var (rapi, root) = await Connect();
            var input = rapi.Path.Combine(root, "should_download_dir_in");
            var output = rapi.Path.Combine(root, "should_download_dir_out");
            var (files, directories) = await BuildComplexFileTree(rapi, input, "super secret 42");

            _output.WriteLine("Downloading files...");
            await rapi.FileSystem.CreateDirectory(output);
            await DoOperation(rapi.Sftp, input, output, false);

            _output.WriteLine("Asserting all folders are in place...");
            foreach (var name in directories)
            {
                var directory = rapi.Path.Combine(output, name);
                var exists = await rapi.FileSystem.DirectoryExists(directory);
                Assert.True(exists);
            }
            
            _output.WriteLine("Asserting all files are in place...");
            foreach (var name in files)
            {
                var file = rapi.Path.Combine(output, name);
                var exists = await rapi.FileSystem.FileExists(file);
                Assert.True(exists);
                
                var bytes = await rapi.FileSystem.ReadFileContents(file);
                var words = Encoding.UTF8.GetString(bytes);
                Assert.Equal("super secret 42", words);
            }
            
            _output.WriteLine("Asserting file and folder count...");
            var fs = await rapi.FileSystem.GetFiles(output);
            var ds = await rapi.FileSystem.GetDirectories(output);
            Assert.Equal(2, fs.Count);
            Assert.Equal(2, ds.Count);
        }

        [Fact]
        public async Task ShouldUploadDirectoriesViaSftp()
        {
            var (rapi, root) = await Connect();
            var input = rapi.Path.Combine(root, "should_upload_dir_in");
            var output = rapi.Path.Combine(root, "should_upload_dir_out");
            var (files, directories) = await BuildComplexFileTree(rapi, input, "42 super secret");

            _output.WriteLine("Uploading files...");
            await rapi.FileSystem.CreateDirectory(output);
            await DoOperation(rapi.Sftp, input, output, true);

            _output.WriteLine("Asserting all folders are in place...");
            foreach (var name in directories)
            {
                var directory = rapi.Path.Combine(output, name);
                var exists = await rapi.FileSystem.DirectoryExists(directory);
                Assert.True(exists);
            }
            
            _output.WriteLine("Asserting all files are in place...");
            foreach (var name in files)
            {
                var file = rapi.Path.Combine(output, name);
                var exists = await rapi.FileSystem.FileExists(file);
                Assert.True(exists);
                
                var bytes = await rapi.FileSystem.ReadFileContents(file);
                var words = Encoding.UTF8.GetString(bytes);
                Assert.Equal("42 super secret", words);
            }
            
            _output.WriteLine("Asserting file and folder count...");
            var fs = await rapi.FileSystem.GetFiles(output);
            var ds = await rapi.FileSystem.GetDirectories(output);
            Assert.Equal(2, fs.Count);
            Assert.Equal(2, ds.Count);
        }

        /**
         * This is a very special test.
         * We need to ensure mounts will work fine on windows.
         * We create the following file structure:
         *
         * should_upload_symlink_root_in/
         * |-- file
         * [should_upload_symlink_root_linked]/
         * should_upload_symlink_root_out/
         *
         * Then, upload [should_upload_symlink_root_linked]
         * directly into should_upload_symlink_root_out.
         */
        [Fact]
        public async Task ShouldSupportUploadingRootSymlinkDirectories()
        {
            var (rapi, root) = await Connect();
            var input = rapi.Path.Combine(root, "should_upload_symlink_root_in");

            await rapi.FileSystem.CreateDirectory(input);
            await rapi.FileSystem.WriteFileContents(rapi.Path.Combine(input, "file"), Encoding.UTF8.GetBytes("42"));

            var linked = rapi.Path.Combine(root, "should_upload_symlink_root_linked");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var command = $"/c mklink /D {linked} {input}";
                _output.WriteLine($"Executing Windows command line: {command}");
                Process.Start(new ProcessStartInfo("cmd.exe", command));
            }
            else
            {
                var command = $"-c \"ln {input} {linked} -s\"";
                _output.WriteLine($"Executing Unix Bash: {command}");
                Process.Start(new ProcessStartInfo("bash", command));
            }

            var output = rapi.Path.Combine(root, "should_upload_symlink_root_out");
            await rapi.FileSystem.CreateDirectory(output);
            await DoOperation(rapi.Sftp, linked, output, true);

            var outFile = rapi.Path.Combine(output, "file");
            Assert.True(await rapi.FileSystem.FileExists(outFile));
            Assert.Equal("42", Encoding.UTF8.GetString(await rapi.FileSystem.ReadFileContents(outFile)));
        }

        /**
         * Here we build the following file tree for download and upload test cases:
         *
         * should_download_dir_in/
         * |-- sample_1
         * |   sample_2
         * |   sample_dir/
         * |   |-- sample_nested_dir/
         * |   |   +-- sample_5
         * |   |   sample_3
         * |   +-- sample_4
         * +-- sample_empty_dir/
         */
        private async Task<(string[] Files, string[] Directories)> BuildComplexFileTree(
            RapiConnection rapi, string baseDir, string fileContents)
        {
            _output.WriteLine("Building file tree...");
            await rapi.FileSystem.CreateDirectory(baseDir);
            await rapi.FileSystem.CreateDirectory(rapi.Path.Combine(baseDir, "sample_dir"));
            await rapi.FileSystem.CreateDirectory(rapi.Path.Combine(baseDir, "sample_dir", "sample_nested_dir"));
            await rapi.FileSystem.CreateDirectory(rapi.Path.Combine(baseDir, "sample_empty_dir"));
            
            var sample = Encoding.UTF8.GetBytes(fileContents);
            var directories = new[]
            {
                rapi.Path.Combine("sample_dir"),
                rapi.Path.Combine("sample_dir", "sample_nested_dir"),
                rapi.Path.Combine("sample_empty_dir")
            };
            var files = new[]
            {
                rapi.Path.Combine("sample_1"),
                rapi.Path.Combine("sample_2"),
                rapi.Path.Combine("sample_dir", "sample_3"),
                rapi.Path.Combine("sample_dir", "sample_4"),
                rapi.Path.Combine("sample_dir", "sample_nested_dir", "sample_5")
            };
            
            _output.WriteLine("Creating directories...");
            foreach (var directory in directories)
            {
                var combined = rapi.Path.Combine(baseDir, directory);
                await rapi.FileSystem.CreateDirectory(combined);
            }
            
            _output.WriteLine("Writing file contents...");
            foreach (var file in files)
            {
                var combined = rapi.Path.Combine(baseDir, file);
                await rapi.FileSystem.WriteFileContents(combined, sample);
            }

            _output.WriteLine("Successfully built file tree.");
            return (files, directories);
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