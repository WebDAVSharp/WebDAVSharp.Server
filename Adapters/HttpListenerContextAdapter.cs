using System;
using System.Net;

namespace WebDAVSharp.Server.Adapters
{
    /// <summary>
    ///     This
    ///     <see cref="IHttpListenerContext" /> implementation wraps around a
    ///     <see cref="HttpListenerContext" /> instance.
    /// </summary>
    public sealed class HttpListenerContextAdapter : IHttpListenerContext
    {
        #region Public Functions

        /// <summary>
        ///     Initializes a new instance of the <see cref="HttpListenerContextAdapter" /> class.
        /// </summary>
        /// <param name="context">The <see cref="HttpListenerContext" /> to adapt for WebDAV#.</param>
        /// <exception cref="System.ArgumentNullException">context</exception>
        /// <exception cref="ArgumentNullException"><paramref name="context" /> is <c>null</c>.</exception>
        public HttpListenerContextAdapter(HttpListenerContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            AdaptedInstance = context;
            _request = new HttpListenerRequestAdapter(context.Request);
            _response = new HttpListenerResponseAdapter(context.Response);
        }

        #endregion

        #region Private Variables

        private readonly HttpListenerRequestAdapter _request;
        private readonly HttpListenerResponseAdapter _response;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the internal instance that was adapted for WebDAV#.
        /// </summary>
        /// <value>
        ///     The adapted instance.
        /// </value>
        public HttpListenerContext AdaptedInstance { get; }

        /// <summary>
        ///     Gets the <see cref="IHttpListenerRequest" /> request adapter.
        /// </summary>
        public IHttpListenerRequest Request => _request;

        /// <summary>
        ///     Gets the <see cref="IHttpListenerResponse" /> response adapter.
        /// </summary>
        public IHttpListenerResponse Response => _response;

        #endregion
    }
}