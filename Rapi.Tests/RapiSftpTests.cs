using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rapi.Tests
{
    // This fixture tests the legacy API.
    [TestFixture]
    [Ignore("Need SUT for this")]
    public sealed class RapiSftpTests : RapiSftpTestsBase
    {
        public RapiSftpTests() : base(false) { }
    }

    // This fixture tests the operation API.
    [TestFixture]
    [Ignore("Need SUT for this")]
    public class RapiSftpBackgroundTests : RapiSftpTestsBase
    {
        public RapiSftpBackgroundTests() : base(true) { }
    }

    public abstract class RapiSftpTestsBase
    {
        private readonly bool _useBackgroundApi;

        protected RapiSftpTestsBase(bool useBackgroundApi)
        {
            _useBackgroundApi = useBackgroundApi;
        }

        [Test]
        public async Task ShouldDownloadFilesViaSftp()
        {
            var (connection, root) = await Connect();
            var input = connection.Path.Combine(root, "should_download_sftp_in");
            await connection.FileSystem.WriteFileContents(input, Encoding.UTF8.GetBytes("super secret"));
            TestContext.Out.WriteLine("should_download_sftp_in contents successfully written.");

            var output = connection.Path.Combine(root, "should_download_sftp_out");
            TestContext.Out.WriteLine($"Downloading file from {input} to {output}");
            await DoOperation(connection.Sftp, input, output, false);

            var downloaded = await connection.FileSystem.ReadFileContents(output);
            var words = Encoding.UTF8.GetString(downloaded);
            Assert.That(words, Is.EqualTo("super secret"));
        }

        [Test]
        public async Task ShouldUploadFilesViaSftp()
        {
            var (connection, root) = await Connect();
            var input = connection.Path.Combine(root, "should_upload_sftp_in");
            await connection.FileSystem.WriteFileContents(input, Encoding.UTF8.GetBytes("super secret"));
            TestContext.Out.WriteLine("should_upload_sftp_in contents successfully written.");

            var output = connection.Path.Combine(root, "should_upload_sftp_out");
            TestContext.Out.WriteLine($"Uploading file from {input} to {output}");
            await DoOperation(connection.Sftp, input, output, true);

            var downloaded = await connection.FileSystem.ReadFileContents(output);
            var words = Encoding.UTF8.GetString(downloaded);
            Assert.That(words, Is.EqualTo("super secret"));
        }

        [Test]
        public async Task ShouldDownloadDirectoriesViaSftp()
        {
            var (rapi, root) = await Connect();
            var input = rapi.Path.Combine(root, "should_download_dir_in");
            var output = rapi.Path.Combine(root, "should_download_dir_out");
            var (files, directories) = await BuildComplexFileTree(rapi, input, "super secret 42");

            TestContext.Out.WriteLine("Downloading files...");
            await rapi.FileSystem.CreateDirectory(output);
            await DoOperation(rapi.Sftp, input, output, false);

            TestContext.Out.WriteLine("Asserting all folders are in place...");
            foreach (var name in directories)
            {
                var directory = rapi.Path.Combine(output, name);
                var exists = await rapi.FileSystem.DirectoryExists(directory);
                Assert.That(exists, Is.True);
            }

            TestContext.Out.WriteLine("Asserting all files are in place...");
            foreach (var name in files)
            {
                var file = rapi.Path.Combine(output, name);
                var exists = await rapi.FileSystem.FileExists(file);
                Assert.That(exists, Is.True);

                var bytes = await rapi.FileSystem.ReadFileContents(file);
                var words = Encoding.UTF8.GetString(bytes);
                Assert.That(words, Is.EqualTo("super secret 42"));
            }

            TestContext.Out.WriteLine("Asserting file and folder count...");
            var fs = await rapi.FileSystem.GetFiles(output);
            var ds = await rapi.FileSystem.GetDirectories(output);
            Assert.That(fs.Count, Is.EqualTo(2));
            Assert.That(ds.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task ShouldUploadDirectoriesViaSftp()
        {
            var (rapi, root) = await Connect();
            var input = rapi.Path.Combine(root, "should_upload_dir_in");
            var output = rapi.Path.Combine(root, "should_upload_dir_out");
            var (files, directories) = await BuildComplexFileTree(rapi, input, "42 super secret");

            TestContext.Out.WriteLine("Uploading files...");
            await rapi.FileSystem.CreateDirectory(output);
            await DoOperation(rapi.Sftp, input, output, true);

            TestContext.Out.WriteLine("Asserting all folders are in place...");
            foreach (var name in directories)
            {
                var directory = rapi.Path.Combine(output, name);
                var exists = await rapi.FileSystem.DirectoryExists(directory);
                Assert.That(exists, Is.True);
            }

            TestContext.Out.WriteLine("Asserting all files are in place...");
            foreach (var name in files)
            {
                var file = rapi.Path.Combine(output, name);
                var exists = await rapi.FileSystem.FileExists(file);
                Assert.That(exists, Is.True);

                var bytes = await rapi.FileSystem.ReadFileContents(file);
                var words = Encoding.UTF8.GetString(bytes);
                Assert.That(words, Is.EqualTo("42 super secret"));
            }

            TestContext.Out.WriteLine("Asserting file and folder count...");
            var fs = await rapi.FileSystem.GetFiles(output);
            var ds = await rapi.FileSystem.GetDirectories(output);
            Assert.That(fs.Count, Is.EqualTo(2));
            Assert.That(ds.Count, Is.EqualTo(2));
        }

        /**
         * This is a very special test.
         * We need to ensure mounts will work fine on windows.
         */
        [Test]
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
                TestContext.Out.WriteLine($"Executing Windows command line: {command}");
                Process.Start(new ProcessStartInfo("cmd.exe", command));
            }
            else
            {
                var command = $"-c \"ln {input} {linked} -s\"";
                TestContext.Out.WriteLine($"Executing Unix Bash: {command}");
                Process.Start(new ProcessStartInfo("bash", command));
            }

            var output = rapi.Path.Combine(root, "should_upload_symlink_root_out");
            await rapi.FileSystem.CreateDirectory(output);
            await DoOperation(rapi.Sftp, linked, output, true);

            var outFile = rapi.Path.Combine(output, "file");
            Assert.That(await rapi.FileSystem.FileExists(outFile), Is.True);
            Assert.That(Encoding.UTF8.GetString(await rapi.FileSystem.ReadFileContents(outFile)), Is.EqualTo("42"));
        }

        private static async Task<(string[] Files, string[] Directories)> BuildComplexFileTree(
            RapiConnection rapi, string baseDir, string fileContents)
        {
            TestContext.Out.WriteLine("Building file tree...");
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

            TestContext.Out.WriteLine("Creating directories...");
            foreach (var directory in directories)
            {
                var combined = rapi.Path.Combine(baseDir, directory);
                await rapi.FileSystem.CreateDirectory(combined);
            }

            TestContext.Out.WriteLine("Writing file contents...");
            foreach (var file in files)
            {
                var combined = rapi.Path.Combine(baseDir, file);
                await rapi.FileSystem.WriteFileContents(combined, sample);
            }

            TestContext.Out.WriteLine("Successfully built file tree.");
            return (files, directories);
        }

        private static async Task<(RapiConnection Connection, string Root)> Connect()
        {
            var connection = await RapiConnection.Connect(RapiTestHost.Address);
            var root = connection.Path.Combine(connection.FileSystemInfo.TempDirectory!, RapiTestHost.DirectoryName);

            if (!await connection.FileSystem.DirectoryExists(root))
                await connection.FileSystem.CreateDirectory(root);

            return (connection, root);
        }

        private async Task DoOperation(IRapiSftpRpc rpc, string from, string to, bool upload)
        {
            if (_useBackgroundApi)
            {
                var id = Guid.NewGuid().ToString();
                await (upload
                    ? rpc.StartUpload(id, from, to, RapiTestHost.Config.Sftp!)
                    : rpc.StartDownload(id, from, to, RapiTestHost.Config.Sftp!));

                while (true)
                {
                    var status = await rpc.TryGetStatus(id);
                    if (status!.IsCompleted)
                    {
                        if (status.Exception != null)
                            throw new Exception(status.Exception);
                        return;
                    }

                    await Task.Delay(100);
                }
            }

            await (upload
                ? rpc.Upload(from, to, RapiTestHost.Config.Sftp!)
                : rpc.Download(from, to, RapiTestHost.Config.Sftp!));
        }
    }
}
