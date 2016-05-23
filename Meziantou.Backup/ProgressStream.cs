using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meziantou.Backup
{
    internal class StreamReadEventArgs : EventArgs
    {
        public StreamReadEventArgs(byte[] bytes, int offset, int count)
        {
            Bytes = bytes;
            Offset = offset;
            Count = count;
        }

        public byte[] Bytes { get; }
        public int Offset { get; }
        public int Count { get; }
    }

    internal class ProgressStream : Stream
    {
        private readonly Stream _stream;
        private readonly bool _ownStream;

        public event EventHandler<StreamReadEventArgs> StreamRead;

        public ProgressStream(Stream stream, bool ownStream = true)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            _stream = stream;
            _ownStream = ownStream;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _stream.Read(buffer, offset, count);
            OnStreamRead(new StreamReadEventArgs(buffer, offset, read));
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        protected virtual void OnStreamRead(StreamReadEventArgs e)
        {
            StreamRead?.Invoke(this, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (_ownStream)
            {
                _stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
