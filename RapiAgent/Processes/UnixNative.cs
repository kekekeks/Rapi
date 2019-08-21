using System;
using System.Runtime.InteropServices;

namespace RapiAgent.Processes
{
    internal class UnixNativeDelegates
    {
        [DllImport("libdl.so.2", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen_lin(string path, int flags);

        [DllImport("libdl.so.2", EntryPoint = "dlsym")]
        private static extern IntPtr dlsym_lin(IntPtr handle, string symbol);

        [DllImport("libSystem.dylib", EntryPoint = "dlopen")]
        private static extern IntPtr dlopen_mac(string path, int flags);

        [DllImport("libSystem.dylib", EntryPoint = "dlsym")]
        private static extern IntPtr dlsym_mac(IntPtr handle, string symbol);

        public static T GetProc<T>()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var dl = dlopen_mac("libSystem.dylib", 2);

                var name = typeof(T).Name;
                var a = dlsym_mac(dl, name);
                return Marshal.GetDelegateForFunctionPointer<T>(a);
            }
            else
            {
                var dl = dlopen_lin("libc.6.so", 2);
                var a = dlsym_lin(dl, typeof(T).Name);
                return Marshal.GetDelegateForFunctionPointer<T>(a);
            }
        }

        public static T GetProc<T>(string function)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var dl = dlopen_mac("libSystem.dylib", 2);
                var a = dlsym_mac(dl, function);
                return Marshal.GetDelegateForFunctionPointer<T>(a);
            }
            else
            {
                var dl = dlopen_lin("libc.6.so", 2);
                var a = dlsym_lin(dl, function);
                return Marshal.GetDelegateForFunctionPointer<T>(a);
            }
        }

        public delegate void dup2(int oldfd, int newfd);

        public delegate int fork();

        public delegate void setsid();

        public delegate int ioctl(int fd, UInt64 ctl, IntPtr arg);

        public delegate void close(int fd);
        public delegate void kill(int pid, int signal);

        public delegate int open([MarshalAs(UnmanagedType.LPStr)] string file, int flags);

        public delegate int chdir([MarshalAs(UnmanagedType.LPStr)] string path);

        public delegate IntPtr ptsname(int fd);

        public delegate int grantpt(int fd);

        public delegate int unlockpt(int fd);

        public unsafe delegate void execve([MarshalAs(UnmanagedType.LPStr)] string path,
            [MarshalAs(UnmanagedType.LPArray)] string[] argv, [MarshalAs(UnmanagedType.LPArray)] string[] envp);

        public delegate int read(int fd, IntPtr buffer, int length);

        public delegate int write(int fd, IntPtr buffer, int length);
        public delegate int fcntl(int fd, int cmd, int flags);

        public delegate void free(IntPtr ptr);
        public delegate void chmod(string dir, int perms);

        public delegate int waitpid(int pid, out int status, int options);

        public delegate int pipe(IntPtr[] fds);

        public delegate int setpgid(int pid, int pgid);

        public delegate int posix_spawn_file_actions_adddup2(IntPtr file_actions, int fildes, int newfildes);

        public delegate int posix_spawn_file_actions_addclose(IntPtr file_actions, int fildes);

        public delegate int posix_spawn_file_actions_init(IntPtr file_actions);

        public delegate int posix_spawnattr_init(IntPtr attributes);

        public delegate int posix_spawnp(out int pid, string path, IntPtr fileActions, IntPtr attrib,
            string[] argv, string[] envp);

        public delegate int dup(int fd);

        public delegate void _exit(int code);

        public delegate int getdtablesize();
    }

    internal static class UnixNative
    {
        public const int O_RDONLY = 0x0000;
        public const int O_WRONLY = 0x0001;
        public const int O_RDWR = 0x0002;
        public const int O_ACCMODE = 0x0003;

        public const int O_CREAT = 0x0100; /* second byte, away from DOS bits */
        public const int O_EXCL = 0x0200;
        public const int O_NOCTTY = 0x0400;
        public const int O_TRUNC = 0x0800;
        public const int O_APPEND = 0x1000;
        public const int O_NONBLOCK = 0x2000;

        public const int F_GETFL = 3;
        public const int F_SETFL = 4;

        public static readonly ulong TIOCSWINSZ =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 0x80087467 : 0x5414;

        public const int _SC_OPEN_MAX = 5;

        public const int EAGAIN = 11; /* Try again */

        public const int EINTR = 4; /* Interrupted system call */

        public const int ENOENT = 2;

        public static readonly ulong TIOCSCTTY =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? (ulong) 0x20007484 : 0x540E;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct winsize
        {
            public ushort ws_row; /* rows, in characters */
            public ushort ws_col; /* columns, in characters */
            public ushort ws_xpixel; /* horizontal size, pixels */
            public ushort ws_ypixel; /* vertical size, pixels */
        };

        public static UnixNativeDelegates.open open = UnixNativeDelegates.GetProc<UnixNativeDelegates.open>();
        public static UnixNativeDelegates.chdir chdir = UnixNativeDelegates.GetProc<UnixNativeDelegates.chdir>();
        public static UnixNativeDelegates.write write = UnixNativeDelegates.GetProc<UnixNativeDelegates.write>();
        public static UnixNativeDelegates.grantpt grantpt = UnixNativeDelegates.GetProc<UnixNativeDelegates.grantpt>();

        public static UnixNativeDelegates.unlockpt unlockpt =
            UnixNativeDelegates.GetProc<UnixNativeDelegates.unlockpt>();

        public static UnixNativeDelegates.ptsname ptsname = UnixNativeDelegates.GetProc<UnixNativeDelegates.ptsname>();

        public static UnixNativeDelegates.posix_spawn_file_actions_init posix_spawn_file_actions_init =
            UnixNativeDelegates.GetProc<UnixNativeDelegates.posix_spawn_file_actions_init>();

        public static UnixNativeDelegates.posix_spawn_file_actions_adddup2 posix_spawn_file_actions_adddup2 =
            UnixNativeDelegates.GetProc<UnixNativeDelegates.posix_spawn_file_actions_adddup2>();

        public static UnixNativeDelegates.posix_spawn_file_actions_addclose posix_spawn_file_actions_addclose =
            UnixNativeDelegates.GetProc<UnixNativeDelegates.posix_spawn_file_actions_addclose>();

        public static UnixNativeDelegates.posix_spawnattr_init posix_spawnattr_init =
            UnixNativeDelegates.GetProc<UnixNativeDelegates.posix_spawnattr_init>();

        public static UnixNativeDelegates.posix_spawnp posix_spawnp =
            UnixNativeDelegates.GetProc<UnixNativeDelegates.posix_spawnp>();

        public static UnixNativeDelegates.dup dup = UnixNativeDelegates.GetProc<UnixNativeDelegates.dup>();
        public static UnixNativeDelegates.read read = UnixNativeDelegates.GetProc<UnixNativeDelegates.read>();
        public static UnixNativeDelegates.setsid setsid = UnixNativeDelegates.GetProc<UnixNativeDelegates.setsid>();
        public static UnixNativeDelegates.ioctl ioctl = UnixNativeDelegates.GetProc<UnixNativeDelegates.ioctl>();
        public static UnixNativeDelegates.execve execve = UnixNativeDelegates.GetProc<UnixNativeDelegates.execve>();
        public static UnixNativeDelegates.close close = UnixNativeDelegates.GetProc<UnixNativeDelegates.close>();
        public static UnixNativeDelegates.chmod chmod = UnixNativeDelegates.GetProc<UnixNativeDelegates.chmod>();
        public static UnixNativeDelegates.fcntl fcntl = UnixNativeDelegates.GetProc<UnixNativeDelegates.fcntl>();
        public static UnixNativeDelegates.waitpid waitpid = UnixNativeDelegates.GetProc<UnixNativeDelegates.waitpid>();
        public static UnixNativeDelegates.kill kill = UnixNativeDelegates.GetProc<UnixNativeDelegates.kill>();

        public static IntPtr StructToPtr(object obj)
        {
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(obj));
            Marshal.StructureToPtr(obj, ptr, false);
            return ptr;
        }
    }
}