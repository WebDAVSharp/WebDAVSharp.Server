using System;
using System.Net;
using System.Security.Principal;

namespace WebDAVSharp.Server.Adapters.AuthenticationTypes
{
    /// <summary>
    ///     This
    ///     <see cref="IHttpListener" /> implementation wraps around a
    ///     <see cref="HttpListener" /> instance.
    /// </summary>
    internal sealed class HttpListenerNegotiateAdapter : WebDavDisposableBase, IHttpListener
    {
        #region Private Variables

        private readonly HttpListener _listener;

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="HttpListenerNegotiateAdapter" /> class.
        /// </summary>
        internal HttpListenerNegotiateAdapter()
        {
            _listener = new HttpListener
            {
                AuthenticationSchemes = AuthenticationSchemes.Negotiate,
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

        #endregion

        #region Function Overrides

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        ///     unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (_listener.IsListening)
                _listener.Close();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the internal instance that was adapted for WebDAV#.
        /// </summary>
        /// <value>
        ///     The adapted instance.
        /// </value>
        public HttpListener AdaptedInstance => _listener;

        /// <summary>
        ///     Gets the Uniform Resource Identifier (
        ///     <see cref="Uri" />) prefixes handled by the
        ///     adapted
        ///     <see cref="HttpListener" /> object.
        /// </summary>
        public HttpListenerPrefixCollection Prefixes => _listener.Prefixes;

        #endregion

        #region Public Functions

        /// <summary>
        ///     Allows the adapted <see cref="HttpListener" /> to receive incoming requests.
        /// </summary>
        public void Start()
        {
            _listener.Start();
        }

        /// <summary>
        ///     Causes the adapted <see cref="HttpListener" /> to stop receiving incoming requests.
        /// </summary>
        public void Stop()
        {
            _listener.Stop();
        }

        public IIdentity GetIdentity(IHttpListenerContext context)
        {
            return context.AdaptedInstance.User.Identity;
        }

        #endregion
    }
}