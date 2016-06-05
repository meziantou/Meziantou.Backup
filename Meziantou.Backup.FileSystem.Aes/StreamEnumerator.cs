using System;
using System.Collections.Generic;
using System.IO;

namespace Meziantou.Backup.FileSystem.Aes
{
    internal class StreamEnumerator : Stream
    {
        private long _position;
        private readonly bool _closeStreams;
        private IEnumerator<Stream> _iterator;
        private Stream _current;

        private Stream Current
        {
            get
            {
                if (_current != null)
                    return _current;

                if (_iterator == null)
                    throw new ObjectDisposedException(GetType().Name);

                if (_iterator.MoveNext())
                {
                    _current = _iterator.Current;
                }

                return _current;
            }
        }

        private void DisposeCurrentStream()
        {
            if (_closeStreams)
            {
                _current?.Dispose();
            }

            _current = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                do
                {
                    DisposeCurrentStream();
                } while (_iterator.MoveNext());

                _iterator.Dispose();
                _iterator = null;
            }

            base.Dispose(disposing);
        }

        public StreamEnumerator(IEnumerable<Stream> source, bool closeStreams)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            _iterator = source.GetEnumerator();
            _closeStreams = closeStreams;
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek => false;
        public override bool CanTimeout => false;

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { return _position; }
            set
            {
                if (value != _position)
                    throw new NotSupportedException();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = 0;
            while (count > 0)
            {
                var stream = Current;
                if (stream == null)
                    break;

                int read = stream.Read(buffer, offset, count);
                result += read;
                count -= read;
                offset += read;
                if (read == 0)
                {
                    DisposeCurrentStream();
                }
            }

            _position += result;
            return result;
        }
    }
}