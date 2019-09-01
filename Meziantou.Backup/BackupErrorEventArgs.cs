using System;
using System.ComponentModel;

namespace Meziantou.Backup
{
    public sealed class BackupErrorEventArgs : CancelEventArgs
    {
        public Exception Exception { get; }
        public int RetryCount { get; }

        public BackupErrorEventArgs(Exception exception, int retryCount)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            Exception = exception;
            RetryCount = retryCount;
        }
    }
}