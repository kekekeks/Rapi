using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Rapi;

namespace RapiAgent.Processes
{
    internal class UnixProcessFactory : IProcessFactory
    {
        private const string PythonScript = @"
import os;
import fcntl;
import sys;
import termios;
try:
    os.setsid()
except:
    pass

try:
    os.chdir(os.environ['__RAPI_DIRECTORY'])
except:
    pass

try:
    fcntl.ioctl(sys.stdin, termios.TIOCSCTTY, 0)
except:
    pass
os.remove(os.environ['__RAPI_SCRIPT_SELF'])
os.execvp(os.environ['__RAPI_TARGET_PATH'], sys.argv)
os._exit(1)
";

        public IProcess Create(ProcessCreationOptions options)
        {
            var stdin = new AnonymousPipeServerStream(PipeDirection.Out);
            var stdout = new AnonymousPipeServerStream(PipeDirection.In);
            var stderr = options.MergeStderr ? null : new AnonymousPipeServerStream(PipeDirection.In);

            var stdinHandle = UnixNative.dup(stdin.ClientSafePipeHandle.DangerousGetHandle().ToInt32());
            var stdoutHandle = UnixNative.dup(stdout.ClientSafePipeHandle.DangerousGetHandle().ToInt32());
            var stderrHandle = stderr == null
                ? (int?)null
                : UnixNative.dup(stderr.ClientSafePipeHandle.DangerousGetHandle().ToInt32());
            
            var fileActions = Marshal.AllocHGlobal(1024);
            _ = UnixNative.posix_spawn_file_actions_init(fileActions);
            _ = UnixNative.posix_spawn_file_actions_adddup2(fileActions, stdinHandle, 0);
            _ = UnixNative.posix_spawn_file_actions_adddup2(fileActions, stdoutHandle, 1);
            _ = UnixNative.posix_spawn_file_actions_adddup2(fileActions, stderrHandle ?? stdoutHandle, 2);
            _ = UnixNative.posix_spawn_file_actions_addclose(fileActions, (int) stdinHandle);
            _ = UnixNative.posix_spawn_file_actions_addclose(fileActions, (int) stdoutHandle);
            if(stderrHandle.HasValue)
                _ = UnixNative.posix_spawn_file_actions_addclose(fileActions, (int) stderrHandle);

            var attributes = Marshal.AllocHGlobal(1024);
            _ = UnixNative.posix_spawnattr_init(attributes);

            var envVars = new List<string?>();

            var denv = options.Environment ?? System.Environment.GetEnvironmentVariables();

            foreach (var variable in denv.Keys)
            {
                if (variable?.ToString() != "TERM")
                {
                    envVars.Add($"{variable}={denv[variable!]}");
                }
            }

            envVars.Add(null);

            var xargs = new List<string?>();
            xargs.Add(options.Path);
            if(options.Arguments != null)
                xargs.AddRange(options.Arguments);
            xargs.Add(null);

            var pythonPaths = new[] {"/usr/bin/python2", "/usr/bin/python3"};

            var pythonPath = pythonPaths.FirstOrDefault(File.Exists);
            if (pythonPath == null)
                throw new Exception("Unable to find python for after-fork actions");

            envVars.Insert(0, "__RAPI_TARGET_PATH=" + options.Path);
            var tempfile = Path.GetTempFileName();
            envVars.Insert(0, "__RAPI_SCRIPT_SELF=" + tempfile);
            envVars.Insert(0, "__RAPI_DIRECTORY=" + (options.WorkingDirectory ?? Directory.GetCurrentDirectory()));
            File.WriteAllText(tempfile, "#!" + pythonPath + "\n" + PythonScript.Replace("\r", ""));
            UnixNative.chmod(tempfile, 0x1C0);

            _ = UnixNative.posix_spawnp(out var pid, tempfile, fileActions, attributes, xargs.ToArray()!,
                envVars.ToArray()!);
            UnixNative.close(stdinHandle);
            UnixNative.close(stdoutHandle);
            if (stderrHandle.HasValue)
                UnixNative.close(stderrHandle.Value);
            
            stdout.ClientSafePipeHandle.Close();
            stdin.ClientSafePipeHandle.Close();
            stderr?.ClientSafePipeHandle.Close();

            return new UnixProcess(pid, stdin, stdout, stderr);
        }

        class UnixProcess : IProcess
        {
            private readonly int _pid;
        
            public UnixProcess(int pid, Stream stdin, Stream stdout, Stream? stderr)
            {
                _pid = pid;
                StdIn = stdin;
                StdoutOrMix = stdout;
                Stderr = stderr;  
                ExitCode = Checker();
            }

            async Task<int> Checker()
            {
                while (true)
                {
                    if (UnixNative.waitpid(_pid, out var status, 1) == _pid)
                    {
                        var code = (status & 0xff00) >> 8;
                        return code;
                    }
                    await Task.Delay(50);
                }
            }

            public int Id => _pid;
            public Stream StdoutOrMix { get; }
            public Stream? Stderr { get; }
            public Stream StdIn { get; }
            public void Kill()
            {
                if (!ExitCode.IsCompleted)
                    UnixNative.kill(-_pid, 9);
            }

            public Task<int> ExitCode { get; }
        }
    }

}
