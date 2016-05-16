using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Meziantou.OneDrive
{
    internal class ExtendedWebBrowser : WebBrowser
    {
        private AxHost.ConnectionPointCookie _cookie;
        private WebBrowser2EventHelper _helper;

        public event EventHandler<WebBrowserNavigateErrorEventArgs> NavigateError;

        protected override void CreateSink()
        {
            base.CreateSink();
            _helper = new WebBrowser2EventHelper(this);
            _cookie = new AxHost.ConnectionPointCookie(ActiveXInstance, _helper, typeof(DWebBrowserEvents2));
        }

        protected override void DetachSink()
        {
            if (_cookie != null)
            {
                _cookie.Disconnect();
                _cookie = null;
            }
            base.DetachSink();
        }

        protected virtual void OnNavigateError(object sender, WebBrowserNavigateErrorEventArgs e)
        {
            NavigateError?.Invoke(this, e);
        }

        private class WebBrowser2EventHelper : StandardOleMarshalObject, DWebBrowserEvents2
        {
            private readonly ExtendedWebBrowser _parent;

            public WebBrowser2EventHelper(ExtendedWebBrowser parent)
            {
                _parent = parent;
            }

            public void NavigateError(object pDisp, ref object url, ref object frame, ref object statusCode, ref bool cancel)
            {
                WebBrowserNavigateErrorEventArgs e = new WebBrowserNavigateErrorEventArgs((string)url, (string)frame, (int)statusCode);
                _parent.OnNavigateError(_parent, e);
                cancel = e.Cancel;
            }
        }

        [ComImport]
        [Guid("34A715A0-6587-11D0-924A-0020AFC7AC4D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface DWebBrowserEvents2
        {
            [DispId(271)]
            void NavigateError(object pDisp, ref object URL, ref object frame, ref object statusCode, ref bool cancel);
        }
    }
}
