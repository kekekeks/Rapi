using System;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rapi;

using static RapiAgent.Processes.Win32;

namespace RapiAgent.Processes
{
    unsafe class Win32ProcessFactory : IProcessFactory
    {
        public IProcess Create(ProcessCreationOptions options)
        {
            var job = CreateJobObjectA(IntPtr.Zero, IntPtr.Zero);
            if(job.IsInvalid)
                throw new Win32Exception();
            var jobInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation =
                {
                    LimitFlags = JobObjectLimitFlags.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                }
            };
            if (!SetInformationJobObject(job, JobObjectInfoClass.JobObjectExtendedLimitInformation, &jobInfo,
                Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()))
                throw new Win32Exception();

            var stdin = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            var stdout = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            var stderr = options.MergeStderr ? null : new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            var sb = new StringBuilder();

            var appName = options.Path;
            PasteArguments.AppendArgument(sb, appName);
            if (!Path.IsPathRooted(appName))
            {
                if (appName.Contains(Path.DirectorySeparatorChar) || appName.Contains(Path.AltDirectorySeparatorChar))
                    throw new ArgumentException("Should provide either full exe path or just file name");
                appName = null;
            }
            
            foreach (var a in options.Arguments ?? Array.Empty<string>())
                PasteArguments.AppendArgument(sb, a);

            var startInfo = new STARTUPINFOW()
            {
                cb = Marshal.SizeOf<STARTUPINFOW>(),
                dwFlags = StartInfoFlags.STARTF_USESTDHANDLES,
                hStdInput = stdin.ClientSafePipeHandle.DangerousGetHandle(),
                hStdOutput = stdout.ClientSafePipeHandle.DangerousGetHandle(),
                hStdError = (stderr ?? stdout).ClientSafePipeHandle.DangerousGetHandle()
            };
            
            
            var procInfo = new PROCESS_INFORMATION();

            string envString = null;
            if (options.Environment?.Count > 0)
            {
                var sysEnv = System.Environment.GetEnvironmentVariables();
                var env = sysEnv.Keys.Cast<string>().ToDictionary(x => x, x => sysEnv[x]);
                foreach (var kp in options.Environment)
                    env[kp.Key] = kp.Value;
                envString = string.Join('\0', env.Select(kp => $"{kp.Key}={kp.Value}"))
                            + "\0\0\0";

            }
            
            if (!CreateProcessW(appName, sb.ToString(), IntPtr.Zero, IntPtr.Zero, true,
                ProcessCreationFlags.CREATE_NO_WINDOW
                | ProcessCreationFlags.CREATE_SUSPENDED
                | ProcessCreationFlags.DETACHED_PROCESS
                | ProcessCreationFlags.CREATE_NEW_PROCESS_GROUP
                | ProcessCreationFlags.DETACHED_PROCESS,
                envString, options.WorkingDirectory, &startInfo, &procInfo))
                throw new Win32Exception();
            
            var hProc = new ProcessHandle(procInfo.hProcess);

            if (!AssignProcessToJobObject(job, hProc))
            {
                try
                {
                    throw new Win32Exception();
                }
                finally
                {
                    hProc.Dispose();
                    job.Dispose();
                }
            }

            ResumeThread(procInfo.hThread);
            CloseHandle(procInfo.hThread);
            
            stdin.ClientSafePipeHandle.Dispose();
            stdout.ClientSafePipeHandle.Dispose();
            stderr?.ClientSafePipeHandle.Dispose();
            
            return new Win32Process(job, hProc, procInfo.dwProcessId,
                stdin, stdout, stderr);
        }
        
        class Win32Process : IProcess
        {
            private readonly JobObjectHandle _job;
            private readonly ProcessHandle _proc;

            public Win32Process(JobObjectHandle job, ProcessHandle proc, int pid,
                Stream stdin, Stream stdout, Stream stderr)
            {
                _job = job;
                _proc = proc;
                Id = pid;
                StdIn = stdin;
                StdoutOrMix = stdout;
                Stderr = stderr;
                var tcs = new TaskCompletionSource<int>();
                ExitCode = tcs.Task;

                new Thread(() =>
                {
                    int rv;
                    while (!GetExitCodeProcess(_proc, out rv) || rv == 259)
                        WaitForSingleObject(_proc, 1000);
                    _proc.Dispose();
                    tcs.SetResult(rv);
                })
                {
                    IsBackground = true
                }.Start();
            }

            public int Id { get; }

            public Stream StdoutOrMix { get; }

            public Stream Stderr { get; }

            public Stream StdIn { get; }

            public void Kill()
            {
                _job.Dispose();
            }

            public Task<int> ExitCode { get; }
        }

        // https://github.com/dotnet/corefx/blob/09e2417cd0505df4558535651efb1bbcffdf0c59/src/Common/src/CoreLib/System/PasteArguments.cs
        static class PasteArguments
        {
            internal static void AppendArgument(StringBuilder stringBuilder, string argument)
            {
                if (stringBuilder.Length != 0)
                {
                    stringBuilder.Append(' ');
                }

                // Parsing rules for non-argv[0] arguments:
                //   - Backslash is a normal character except followed by a quote.
                //   - 2N backslashes followed by a quote ==> N literal backslashes followed by unescaped quote
                //   - 2N+1 backslashes followed by a quote ==> N literal backslashes followed by a literal quote
                //   - Parsing stops at first whitespace outside of quoted region.
                //   - (post 2008 rule): A closing quote followed by another quote ==> literal quote, and parsing remains in quoting mode.
                if (argument.Length != 0 && ContainsNoWhitespaceOrQuotes(argument))
                {
                    // Simple case - no quoting or changes needed.
                    stringBuilder.Append(argument);
                }
                else
                {
                    stringBuilder.Append(Quote);
                    int idx = 0;
                    while (idx < argument.Length)
                    {
                        char c = argument[idx++];
                        if (c == Backslash)
                        {
                            int numBackSlash = 1;
                            while (idx < argument.Length && argument[idx] == Backslash)
                            {
                                idx++;
                                numBackSlash++;
                            }

                            if (idx == argument.Length)
                            {
                                // We'll emit an end quote after this so must double the number of backslashes.
                                stringBuilder.Append(Backslash, numBackSlash * 2);
                            }
                            else if (argument[idx] == Quote)
                            {
                                // Backslashes will be followed by a quote. Must double the number of backslashes.
                                stringBuilder.Append(Backslash, numBackSlash * 2 + 1);
                                stringBuilder.Append(Quote);
                                idx++;
                            }
                            else
                            {
                                // Backslash will not be followed by a quote, so emit as normal characters.
                                stringBuilder.Append(Backslash, numBackSlash);
                            }

                            continue;
                        }

                        if (c == Quote)
                        {
                            // Escape the quote so it appears as a literal. This also guarantees that we won't end up generating a closing quote followed
                            // by another quote (which parses differently pre-2008 vs. post-2008.)
                            stringBuilder.Append(Backslash);
                            stringBuilder.Append(Quote);
                            continue;
                        }

                        stringBuilder.Append(c);
                    }

                    stringBuilder.Append(Quote);
                }
            }

            private static bool ContainsNoWhitespaceOrQuotes(string s)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    if (char.IsWhiteSpace(c) || c == Quote)
                    {
                        return false;
                    }
                }

                return true;
            }

            private const char Quote = '\"';
            private const char Backslash = '\\';
        }
    }
}