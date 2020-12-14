// The ComStream class is used for the contact property types.
// The types can have unexpected behavior if they're changed by callers,
// so this provides an immutable stream implementation.
// The volatile functions are implemented (not tested)
// in case a separate ReadonlyStream needs to be implemented.
//#define FEATURE_MUTABLE_COM_STREAMS

namespace ControlzEx.Standard
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;

    // disambiguate with System.Runtime.InteropServices.STATSTG
    using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

    // This is adapted from Microsoft KB article 321340
    /// <summary>
    /// Wraps an IStream interface pointer from COM into a form consumable by .Net.
    /// </summary>
    /// <remarks>
    /// This implementation is immutable, though it's possible that the underlying
    /// stream can be changed in another context.
    /// </remarks>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal sealed class ComStream : Stream
    {
        private const int STATFLAG_NONAME = 1;

        private IStream _source;

        private void _Validate()
        {
            if (null == _source)
            {
                throw new ObjectDisposedException("this");
            }
        }

        /// <summary>
        /// Wraps a native IStream interface into a CLR Stream subclass.
        /// </summary>
        /// <param name="stream">
        /// The stream that this object wraps.
        /// </param>
        /// <remarks>
        /// Note that the parameter is passed by ref.  On successful creation it is
        /// zeroed out to the caller.  This object becomes responsible for the lifetime
        /// management of the wrapped IStream.
        /// </remarks>
        public ComStream(ref IStream stream)
        {
            Verify.IsNotNull(stream, "stream");
            _source = stream;
            // Zero out caller's reference to this.  The object now owns the memory.
            stream = null!;
        }

        #region Overridden Stream Methods

        // Experimentally, the base class seems to deal with the IDisposable pattern.
        // Overridden implementations aren't called, but Close is as part of the Dispose call.
        public override void Close()
        {
            if (null != _source)
            {
#if FEATURE_MUTABLE_COM_STREAMS
                Flush();
#endif
                Utility.SafeRelease(ref _source!);
            }
        }

        public override bool CanRead
        {
            get
            {
                // For the context of this class, this should be true...
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                // This should be true...
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
#if FEATURE_MUTABLE_COM_STREAMS
                // Really don't know that this is true...
                return true;
#endif
                return false;
            }
        }

        public override void Flush()
        {
#if FEATURE_MUTABLE_COM_STREAMS
            _Validate();
            // Don't have enough context of the underlying object to reliably do anything here.
            try
            {
                _source.Commit(STGC_DEFAULT);
            }
            catch { }
#endif
        }

        public override long Length
        {
            get
            {
                _Validate();

                STATSTG statstg;
                _source.Stat(out statstg, STATFLAG_NONAME);
                return statstg.cbSize;
            }
        }

        public override long Position
        {
            get { return Seek(0, SeekOrigin.Current); }
            set { Seek(value, SeekOrigin.Begin); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _Validate();

            IntPtr pcbRead = IntPtr.Zero;

            try
            {
                pcbRead = Marshal.AllocHGlobal(sizeof(Int32));

                // PERFORMANCE NOTE: This buffer doesn't need to be allocated if offset == 0
                var tempBuffer = new byte[count];
                _source.Read(tempBuffer, count, pcbRead);
                Array.Copy(tempBuffer, 0, buffer, offset, Marshal.ReadInt32(pcbRead));

                return Marshal.ReadInt32(pcbRead);
            }
            finally
            {
                Utility.SafeFreeHGlobal(ref pcbRead);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _Validate();

            IntPtr plibNewPosition = IntPtr.Zero;

            try
            {
                plibNewPosition = Marshal.AllocHGlobal(sizeof(Int64));
                _source.Seek(offset, (int)origin, plibNewPosition);

                return Marshal.ReadInt64(plibNewPosition);
            }
            finally
            {
                Utility.SafeFreeHGlobal(ref plibNewPosition);
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
#if FEATURE_MUTABLE_COM_STREAMS
            _Validate();
            _source.SetSize(value);
#endif
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
#if FEATURE_MUTABLE_COM_STREAMS
            _Validate();

            // PERFORMANCE NOTE: This buffer doesn't need to be allocated if offset == 0
            byte[] tempBuffer = new byte[buffer.Length - offset];
            Array.Copy(buffer, offset, tempBuffer, 0, tempBuffer.Length);
            _source.Write(tempBuffer, tempBuffer.Length, IntPtr.Zero);
#endif
        }

        #endregion
    }

#if CONSIDER_ADDING
    /// <summary>
    /// Wraps an existing stream in a read-only interface.  The stream can still be modified externally.
    /// </summary>
    internal class ReadonlyStream : Stream
    {
        private Stream _stream;

        public ReadonlyStream(Stream source)
        {
            Verify.IsNotNull(source, "source");
            _stream = source;
        }

        public override bool CanRead
        {
            get
            {
                return _stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override void Flush() { }

        public override long Length
        {
            get
            {
                return _stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return _stream.Position;
            }
            set
            {
                _stream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("The stream doesn't support modifications.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("The stream doesn't support modifications.");
        }

        public override void Close()
        {
            base.Close();
        }
    }

    /// <summary>
    /// Wraps a string to provide read-only Stream semantics.
    /// </summary>
    internal class StringStream : Stream
    {
        private string _source;
        private int _position;

        public StringStream(string source)
        {
            _source = source;
            _position = 0;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Length
        {
            get { return _source.Length * 2; }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                Validate.BoundedInteger(0, (int)value, (int)Length + 1, "value");
                _position = (int)value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int cbRead = 0;
            for (; cbRead < count; ++cbRead)
            {
                if (Length <= Position)
                {
                    break;
                }
                buffer[offset + cbRead] = (byte)(0xFF & (_source[(int)Position / 2] >> ((0 == Position % 2) ? 0 : 8)));
                ++Position;
            }
            return cbRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
                default:
                    throw new FormatException("Bad value for origin");
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
#endif
}
