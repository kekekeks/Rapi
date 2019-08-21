using System;
using System.IO;

namespace RapiAgent.Processes
{
    internal class UnixPtyStreamBase : Stream
    {
        protected int Fd { get; private set; }

        public UnixPtyStreamBase(int fd)
        {
            Fd = fd;
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Close()
        {
            if (Fd == -1)
                return;
            UnixNative.close(Fd);
            Fd = -1;
        }
    }

    internal class UnixPtyReadStream : UnixPtyStreamBase
    {
        public override unsafe int Read(byte[] buffer, int offset, int count)
        {
            fixed (byte* ptr = buffer)
                return Math.Max(0, UnixNative.read(Fd, new IntPtr(ptr), count));
        }

        public UnixPtyReadStream(int fd) : base(fd)
        {
        }

        public override bool CanRead => true;

    }

    internal class UnixPtyWriteStream : UnixPtyStreamBase
    {
        private readonly object _lock = new object();
        
        public UnixPtyWriteStream(int fd) : base(fd)
        {
        }

        public override bool CanWrite => true;

        public override unsafe void Write(byte[] buffer, int offset, int count)
        {
            lock(_lock)
            fixed (byte* ptr = buffer)
                UnixNative.write(Fd, new IntPtr(ptr), count);
        }
        
        public override void Close()
        {
            if (Fd != -1)
            {
                var buf = new byte[] {4};
                Write(buf, 0, 1);
                base.Close();
            }
        }
    }
}