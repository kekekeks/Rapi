using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace RapiAgent.Processes
{
    public unsafe class Win32
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern JobObjectHandle CreateJobObjectA(IntPtr ignore, IntPtr ignore2);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AssignProcessToJobObject(JobObjectHandle job, ProcessHandle process);

        public enum JobObjectInfoClass
        {
            JobObjectAssociateCompletionPortInformation = 7,
            JobObjectBasicLimitInformation = 2,
            JobObjectBasicUIRestrictions = 4,
            JobObjectCpuRateControlInformation = 15,
            JobObjectEndOfJobTimeInformation = 6,
            JobObjectExtendedLimitInformation = 9,
            JobObjectGroupInformation = 11,
            JobObjectGroupInformationEx = 14,
            JobObjectLimitViolationInformation2 = 35,
            JobObjectNetRateControlInformation = 32,
            JobObjectNotificationLimitInformation = 12,
            JobObjectNotificationLimitInformation2 = 34,
            JobObjectSecurityLimitInformation = 5,
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetInformationJobObject(
            JobObjectHandle hJob,
            JobObjectInfoClass JobObjectInformationClass,
            void* lpJobObjectInformation,
            int cbJobObjectInformationLength
        );

        [StructLayout(LayoutKind.Sequential)]
        public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public JobObjectLimitFlags LimitFlags;
            public IntPtr MinimumWorkingSetSize;
            public IntPtr MaximumWorkingSetSize;
            public int ActiveProcessLimit;
            public IntPtr Affinity;
            public int PriorityClass;
            public int SchedulingClass;
        }

        public enum JobObjectLimitFlags
        {
            JOB_OBJECT_LIMIT_ACTIVE_PROCESS = 0x00000008,
            JOB_OBJECT_LIMIT_AFFINITY = 0x00000010,
            JOB_OBJECT_LIMIT_BREAKAWAY_OK = 0x00000800,
            JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION = 0x00000400,
            JOB_OBJECT_LIMIT_JOB_MEMORY = 0x00000200,
            JOB_OBJECT_LIMIT_JOB_TIME = 0x00000004,
            JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000,
            JOB_OBJECT_LIMIT_PRESERVE_JOB_TIME = 0x00000040,
            JOB_OBJECT_LIMIT_PRIORITY_CLASS = 0x00000020,
            JOB_OBJECT_LIMIT_PROCESS_MEMORY = 0x00000100,
            JOB_OBJECT_LIMIT_PROCESS_TIME = 0x00000002,
            JOB_OBJECT_LIMIT_SCHEDULING_CLASS = 0x00000080,
            JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK = 0x00001000,
            JOB_OBJECT_LIMIT_SUBSET_AFFINITY = 0x00004000,
            JOB_OBJECT_LIMIT_WORKINGSET = 0x00000001
        }
        public struct IO_COUNTERS {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }
        public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS                       IoInfo;
            public IntPtr                            ProcessMemoryLimit;
            public IntPtr                            JobMemoryLimit;
            public IntPtr                            PeakProcessMemoryUsed;
            public IntPtr                            PeakJobMemoryUsed;
        }
    
        [Flags]
        public enum ProcessCreationFlags
        {
            CREATE_BREAKAWAY_FROM_JOB=0x01000000,
            CREATE_DEFAULT_ERROR_MODE=0x04000000,
            CREATE_NEW_CONSOLE=0x00000010,
            CREATE_NEW_PROCESS_GROUP=0x00000200,
            CREATE_NO_WINDOW=0x08000000,
            CREATE_PROTECTED_PROCESS=0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL=0x02000000,
            CREATE_SECURE_PROCESS=0x00400000,
            CREATE_SEPARATE_WOW_VDM=0x00000800,
            CREATE_SHARED_WOW_VDM=0x00001000,
            CREATE_SUSPENDED=0x00000004,
            CREATE_UNICODE_ENVIRONMENT=0x00000400,
            DEBUG_ONLY_THIS_PROCESS=0x00000002,
            DEBUG_PROCESS=0x00000001,
            DETACHED_PROCESS=0x00000008,
            EXTENDED_STARTUPINFO_PRESENT=0x00080000,
            INHERIT_PARENT_AFFINITY=0x00010000,
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFOW {
            public int  cb;
            public IntPtr  lpReserved;
            public IntPtr  lpDesktop;
            public IntPtr  lpTitle;
            public int  dwX;
            public int  dwY;
            public int  dwXSize;
            public int  dwYSize;
            public int  dwXCountChars;
            public int  dwYCountChars;
            public int  dwFillAttribute;
            public StartInfoFlags  dwFlags;
            public short   wShowWindow;
            public short  cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }
        
        public enum StartInfoFlags
        {
            STARTF_FORCEONFEEDBACK=0x00000040,
            STARTF_FORCEOFFFEEDBACK=0x00000080,
            STARTF_PREVENTPINNING=0x00002000,
            STARTF_RUNFULLSCREEN=0x00000020,
            STARTF_TITLEISAPPID=0x00001000,
            STARTF_TITLEISLINKNAME=0x00000800,
            STARTF_UNTRUSTEDSOURCE=0x00008000,
            STARTF_USECOUNTCHARS=0x00000008,
            STARTF_USEFILLATTRIBUTE=0x00000010,
            STARTF_USEHOTKEY=0x00000200,
            STARTF_USEPOSITION=0x00000004,
            STARTF_USESHOWWINDOW=0x00000001,
            STARTF_USESIZE=0x00000002,
            STARTF_USESTDHANDLES=0x00000100
        }
        
        public struct PROCESS_INFORMATION {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int  dwProcessId;
            public int  dwThreadId;
        }
        
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessW(
            string?               lpApplicationName,
            string                lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool                  bInheritHandles,
            ProcessCreationFlags dwCreationFlags,
            IntPtr                lpEnvironment,
            string?              lpCurrentDirectory,
            STARTUPINFOW*        lpStartupInfo,
            PROCESS_INFORMATION* lpProcessInformation
        );

        [DllImport("kernel32.dll")]
        public static extern bool ResumeThread(IntPtr thread);

        [DllImport("kernel32.dll")]
        public static extern int WaitForSingleObject(Win32Handle handle, int timeout);

        [DllImport("kernel32.dll")]
        public static extern bool GetExitCodeProcess(ProcessHandle handle, out int code);

        public class Win32Handle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public Win32Handle(bool ownsHandle) : base(ownsHandle)
            {
            }

            public Win32Handle(bool ownsHandle, IntPtr handle) : base(ownsHandle)
            {
                this.handle = handle;
            }
            
            protected override bool ReleaseHandle()
            {
                CloseHandle(handle);
                return true;
            }

            public Win32Handle() : base(true)
            {
                
            }
        }

        public class JobObjectHandle : Win32Handle
        {
            public JobObjectHandle(bool ownsHandle) : base(ownsHandle)
            {
            }

            public JobObjectHandle()
            {
                
            }
        }
        
        public class ProcessHandle : Win32Handle
        {
            public ProcessHandle(IntPtr handle) : base(true, handle)
            {
                
            }
            
            public ProcessHandle(bool ownsHandle) : base(ownsHandle)
            {
            }

            public ProcessHandle()
            {
                
            }
        }
    }
}