using System;

namespace Meziantou.OneDrive
{
    public class OneDriveException : Exception
    {
        public OneDriveException(Error error, string message = null, Exception innerException = null)
        : base(message, innerException)
        {
            Error = error;
        }

        public Error Error { get; private set; }

        public bool IsMatch(OneDriveErrorCode errorCode)
        {
            return IsMatch(errorCode.ToString());
        }

        public bool IsMatch(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode))
            {
                throw new ArgumentException("errorCode cannot be null or empty", nameof(errorCode));
            }

            var currentError = Error;

            while (currentError != null)
            {
                if (string.Equals(currentError.Code, errorCode, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                currentError = currentError.InnerError;
            }

            return false;
        }

        public override string ToString()
        {
            if (Error != null)
            {
                return Error.ToString();
            }

            return null;
        }
    }
}
