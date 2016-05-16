using System;
using System.ComponentModel;

namespace Meziantou.OneDrive
{
    public class ChunkUploadErrorEventArgs : CancelEventArgs
    {
        public ChunkUploadErrorEventArgs(OneDriveClient oneDriveClient, OneDriveItem parent, Exception exception, byte[] chunk, long @from, long to, int attemptCount)
        {
            OneDriveClient = oneDriveClient;
            Parent = parent;
            Exception = exception;
            Chunk = chunk;
            From = from;
            To = to;
            AttemptCount = attemptCount;
        }

        public OneDriveClient OneDriveClient { get; }
        public OneDriveItem Parent { get; }
        public Exception Exception { get; }
        public byte[] Chunk { get; }
        public long From { get; }
        public long To { get; }
        public int AttemptCount { get; }
    }
}