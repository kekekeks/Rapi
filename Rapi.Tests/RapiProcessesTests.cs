using System;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rapi.Tests
{
    [TestFixture]
    public class RapiProcessesTests
    {
        [TestCase("Hello, world!")]
        [TestCase("Привет, мир!")]
        public async Task ShouldSpawnProcessAndReadStdOut(string phrase)
        {
            const string pid = "rapi_should_read_stdout";
            var (connection, shell, args, _) = await Connect();

            await connection.Processes.Start(pid, new ProcessCreationOptions(shell, args));
            await connection.Processes.WriteStdIn(pid, Encoding.UTF8.GetBytes($"echo {phrase}\r\n"));
            await connection.Processes.CloseStdIn(pid);

            while (await connection.Processes.GetExitCode(pid) == null)
                await Task.Delay(TimeSpan.FromSeconds(0.1));

            var code = await connection.Processes.GetExitCode(pid);
            var stdout = await connection.Processes.GetOutput(pid, false);
            var stderr = await connection.Processes.GetOutput(pid, true);

            // When there is no stderr output, then we receive
            // an empty string on all operating systems.
            Assert.That(code, Is.EqualTo(0));
            Assert.That(Encoding.UTF8.GetString(stdout!), Does.Contain(phrase));
            Assert.That(stderr, Is.Empty);
        }

        [Test]
        public async Task ShouldSpawnProcessAndReadStdErr()
        {
            const string pid = "rapi_should_read_stderr";
            var (connection, shell, args, error) = await Connect();

            await connection.Processes.Start(pid, new ProcessCreationOptions(shell, args));
            await connection.Processes.WriteStdIn(pid, Encoding.UTF8.GetBytes(error));
            await connection.Processes.CloseStdIn(pid);

            while (await connection.Processes.GetExitCode(pid) == null)
                await Task.Delay(TimeSpan.FromSeconds(0.1));

            var code = await connection.Processes.GetExitCode(pid);
            var stdout = await connection.Processes.GetOutput(pid, false);
            var stderr = await connection.Processes.GetOutput(pid, true);

            // When there is no stdout, on Windows we still receive the text
            // of the command we've just typed in and a header.
            Assert.That(code, Is.Not.EqualTo(0));
            Assert.That(stdout, Is.Not.Null);
            Assert.That(Encoding.UTF8.GetString(stderr!), Is.Not.Empty);
        }

        [Test]
        public async Task ShouldSpawnProcessAndReadStdOutMergedWithStdErr()
        {
            const string pid = "rapi_should_read_std_merged";
            var (connection, shell, args, error) = await Connect();

            await connection.Processes.Start(pid, new ProcessCreationOptions(shell, args) { MergeStderr = true });
            await connection.Processes.WriteStdIn(pid, Encoding.UTF8.GetBytes(error));
            await connection.Processes.CloseStdIn(pid);

            while (await connection.Processes.GetExitCode(pid) == null)
                await Task.Delay(TimeSpan.FromSeconds(0.1));

            var code = await connection.Processes.GetExitCode(pid);
            var stdout = await connection.Processes.GetOutput(pid, false);
            var stderr = await connection.Processes.GetOutput(pid, true);

            // When MergeStderr is set to true in ProcessCreationOptions,
            // we receive null for error output string.
            Assert.That(code, Is.Not.EqualTo(0));
            Assert.That(Encoding.UTF8.GetString(stdout!), Is.Not.Empty);
            Assert.That(stderr, Is.Null);
        }

        private static async Task<(RapiConnection Connection, string Shell, string[]? Args, string Err)> Connect()
        {
            var connection = await RapiConnection.Connect(RapiTestHost.Address);
            var windows = connection.SystemInfo.Platform!.IsWindows;
            var shell = windows ? "cmd.exe" : "bash";
            var args = windows ? new[] { "/D" } : null;
            var err = windows ? "echo x 1>&2 && exit 1\r\n" : "ls *.blah";
            return (connection, shell, args, err);
        }
    }
}
