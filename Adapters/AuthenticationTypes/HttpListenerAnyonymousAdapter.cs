using System;
using System.Net;
using System.Security.Principal;

namespace WebDAVSharp.Server.Adapters.AuthenticationTypes
{
    internal class HttpListenerAnyonymousAdapter : WebDavDisposableBase, IHttpListener
    {
        public HttpListenerAnyonymousAdapter()
        {
            AdaptedInstance = new HttpListener
            {
                AuthenticationSchemes = AuthenticationSchemes.Anonymous,
                UnsafeConnectionNtlmAuthentication = false,
                TimeoutManager =
                {
                    RequestQueue = TimeSpan.FromMinutes(5),
                    DrainEntityBody = TimeSpan.FromMinutes(5),
                    EntityBody = TimeSpan.FromMinutes(5),
                    HeaderWait = TimeSpan.FromMinutes(5),
                    IdleConnection = TimeSpan.FromMinutes(5),
                    MinSendBytesPerSecond = 300
                },
                IgnoreWriteExceptions = true
            };
        }

        public HttpListener AdaptedInstance { get; }
        public HttpListenerPrefixCollection Prefixes => AdaptedInstance.Prefixes;

        public void Start()
        {
            AdaptedInstance.Start();
        }

        public void Stop()
        {
            AdaptedInstance.Stop();
        }

        public IIdentity GetIdentity(IHttpListenerContext context)
        {
            return WindowsIdentity.GetCurrent();
        }

        protected override void Dispose(bool disposing)
        {
            if (AdaptedInstance.IsListening)
                AdaptedInstance.Close();
        }
    }
}