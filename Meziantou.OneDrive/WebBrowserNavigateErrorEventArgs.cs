using System.ComponentModel;

namespace Meziantou.OneDrive
{
    internal class WebBrowserNavigateErrorEventArgs : CancelEventArgs
    {
        public WebBrowserNavigateErrorEventArgs(string url, string frame, int statusCode)
        {
            Url = url;
            Frame = frame;
            StatusCode = statusCode;
        }

        public string Url { get; private set; }
        public string Frame { get; private set; }
        public int StatusCode { get; private set; }
    }
}
