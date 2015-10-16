#if DEBUG
using Common.Logging;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Adapters.AuthenticationTypes;
using WebDAVSharp.Server.MethodHandlers;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Utilities;

namespace WebDAVSharp.Server
{
    /// <summary>
    ///     This class implements the core WebDAV server.
    /// </summary>
    public class WebDavServer : WebDavDisposableBase
    {
        #region Variables

        /// <summary>
        ///     The HTTP user
        /// </summary>
        public static string HttpUser = "HTTP.User";

        private IHttpListener _listener;
        private readonly bool _ownsListener;
        private IWebDavStore _store;
        private Dictionary<string, IWebDavMethodHandler> _methodHandlers;

        /// <summary>
        /// </summary>
        public int DefaultConnectionLimit { get; } = 500;

        /// <summary>
        /// </summary>
        public bool Expect100Continue { get; } = true;

        /// <summary>
        /// </summary>
        public int MaxServicePoints { get; } = 500;

#if DEBUG
        internal static readonly ILog _log = LogManager.GetLogger<WebDavServer>();
#endif

        private bool _isListening;

        #endregion

        #region Properties

        /// <summary>
        ///     Allow users to have Indefinite Locks
        /// </summary>
        public bool AllowInfiniteCheckouts
        {
            get { return _store.LockSystem.AllowInfiniteCheckouts; }
            set { _store.LockSystem.AllowInfiniteCheckouts = value; }
        }

        /// <summary>
        ///     The maximum number of seconds a person can check an item out for.
        /// </summary>
        public long MaxCheckOutSeconds
        {
            get { return _store.LockSystem.MaxCheckOutSeconds; }
            set { _store.LockSystem.MaxCheckOutSeconds = value; }
        }

#if DEBUG
    /// <summary>
    ///     Logging Interface
    /// </summary>
        public static ILog Log
        {
            get
            {
                return _log;
            }
        }
#endif

        /// <summary>
        ///     Gets the <see cref="IWebDavStore" /> this <see cref="WebDavServer" /> is hosting.
        /// </summary>
        /// <value>
        ///     The store.
        /// </value>
        public IWebDavStore Store => _store;

        /// <summary>
        ///     Gets the
        ///     <see cref="IHttpListener" /> that this
        ///     <see cref="WebDavServer" /> uses for
        ///     the web server portion.
        /// </summary>
        /// <value>
        ///     The listener.
        /// </value>
        internal IHttpListener Listener => _listener;

        #endregion

        #region Constructor

        ///// <summary>
        ///// </summary>
        ///// <param name="key"></param>
        //public delegate void ClearCaches(string key);

        //private void DoClearCaches(string key)
        //{
        //    foreach (IWebDavMethodHandler o in _methodHandlers.Values)
        //        o.RemoveCacheObject(key);
        //}

        /// <summary>
        /// </summary>
        /// <param name="store"></param>
        /// <param name="authtype"></param>
        /// <param name="methodHandlers"></param>
        public WebDavServer(ref IWebDavStore store, AuthType authtype, IEnumerable<IWebDavMethodHandler> methodHandlers = null)
        {
            ServicePointManager.DefaultConnectionLimit = DefaultConnectionLimit;
            ServicePointManager.Expect100Continue = Expect100Continue;
            ServicePointManager.MaxServicePoints = MaxServicePoints;
            ServicePointManager.MaxServicePointIdleTime = int.MaxValue;
            _ownsListener = true;
            switch (authtype)
            {
                case AuthType.Basic:
                    _listener = new HttpListenerBasicAdapter();
                    break;
                case AuthType.Negotiate:
                    _listener = new HttpListenerNegotiateAdapter();
                    break;
                case AuthType.Anonymous:
                    _listener = new HttpListenerAnyonymousAdapter();
                    break;
            }
            methodHandlers = methodHandlers ?? WebDavMethodHandlers.BuiltIn;

            IWebDavMethodHandler[] webDavMethodHandlers = methodHandlers as IWebDavMethodHandler[] ?? methodHandlers.ToArray();

            if (!webDavMethodHandlers.Any())
                throw new ArgumentException("The methodHandlers collection is empty", nameof(methodHandlers));
            if (webDavMethodHandlers.Any(methodHandler => methodHandler == null))
                throw new ArgumentException("The methodHandlers collection contains a null-reference", nameof(methodHandlers));
            _store = store;
            //_store.FClearCaches = DoClearCaches;
            var handlersWithNames =
                from methodHandler in webDavMethodHandlers
                from name in methodHandler.Names
                select new
                {
                    name,
                    methodHandler
                };
            _methodHandlers = handlersWithNames.ToDictionary(v => v.name, v => v.methodHandler);
        }

        /// <summary>
        ///     This constructor uses a Negotiate Listener if one isn't passed.
        ///     Initializes a new instance of the <see cref="WebDavServer" /> class.
        /// </summary>
        /// <param name="store">
        ///     The
        ///     <see cref="IWebDavStore" /> store object that will provide
        ///     collections and documents for this
        ///     <see cref="WebDavServer" />.
        /// </param>
        /// <param name="listener">
        ///     The
        ///     <see cref="IHttpListener" /> object that will handle the web server portion of
        ///     the WebDAV server; or
        ///     <c>null</c> to use a fresh one.
        /// </param>
        /// <param name="methodHandlers">
        ///     A collection of HTTP method handlers to use by this
        ///     <see cref="WebDavServer" />;
        ///     or
        ///     <c>null</c> to use the built-in method handlers.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        ///     <para>
        ///         <paramref name="listener" /> is <c>null</c>.
        ///     </para>
        ///     <para>- or -</para>
        ///     <para>
        ///         <paramref name="store" /> is <c>null</c>.
        ///     </para>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     <para>
        ///         <paramref name="methodHandlers" /> is empty.
        ///     </para>
        ///     <para>- or -</para>
        ///     <para>
        ///         <paramref name="methodHandlers" /> contains a <c>null</c>-reference.
        ///     </para>
        /// </exception>
        public WebDavServer(ref IWebDavStore store, IHttpListener listener = null, IEnumerable<IWebDavMethodHandler> methodHandlers = null)
        {
            ServicePointManager.DefaultConnectionLimit = DefaultConnectionLimit;
            ServicePointManager.Expect100Continue = Expect100Continue;
            ServicePointManager.MaxServicePoints = MaxServicePoints;
            ServicePointManager.MaxServicePointIdleTime = int.MaxValue;
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            if (listener == null)
            {
                listener = new HttpListenerNegotiateAdapter();
                _ownsListener = true;
            }
            methodHandlers = methodHandlers ?? WebDavMethodHandlers.BuiltIn;

            IWebDavMethodHandler[] webDavMethodHandlers = methodHandlers as IWebDavMethodHandler[] ?? methodHandlers.ToArray();

            if (!webDavMethodHandlers.Any())
                throw new ArgumentException("The methodHandlers collection is empty", nameof(methodHandlers));
            if (webDavMethodHandlers.Any(methodHandler => methodHandler == null))
                throw new ArgumentException("The methodHandlers collection contains a null-reference", nameof(methodHandlers));

            _listener = listener;
            _store = store;
            //_store.FClearCaches = DoClearCaches;
            var handlersWithNames =
                from methodHandler in webDavMethodHandlers
                from name in methodHandler.Names
                select new
                {
                    name,
                    methodHandler
                };
            _methodHandlers = handlersWithNames.ToDictionary(v => v.name, v => v.methodHandler);
        }

        #endregion

        #region Functions

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        ///     unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (_ownsListener)
                _listener.Dispose();
        }


        private List<HttpReciever> _httpRecievers;

        /// <summary>
        ///     Starts this
        ///     <see cref="WebDavServer" /> and returns once it has
        ///     been started successfully.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     This WebDAVServer instance is already running, call to Start is
        ///     invalid at this point
        /// </exception>
        /// <exception cref="ObjectDisposedException">This <see cref="WebDavServer" /> instance has been disposed of.</exception>
        /// <exception cref="InvalidOperationException">The server is already running.</exception>
        public void Start(String url)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Listener.Prefixes.Add(url);
            EnsureNotDisposed();
            if (_isListening)
                throw new InvalidOperationException("This WebDAVServer instance is already running, call to Start is invalid at this point");
            _isListening = true;

            Listener.Start();

            _httpRecievers = new List<HttpReciever>();

            for (int i = 0; i < DefaultConnectionLimit; i++)
            {
                HttpReciever hr = new HttpReciever(i.ToString(CultureInfo.InvariantCulture), ref _listener, ref _methodHandlers, ref _store, this);
                _httpRecievers.Add(hr);
                hr.Start();
            }
        }


        /// <summary>
        ///     Starts this
        ///     <see cref="WebDavServer" /> and returns once it has
        ///     been stopped successfully.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     This WebDAVServer instance is not running, call to Stop is invalid
        ///     at this point
        /// </exception>
        /// <exception cref="ObjectDisposedException">This <see cref="WebDavServer" /> instance has been disposed of.</exception>
        /// <exception cref="InvalidOperationException">The server is not running.</exception>
        public void Stop()
        {
            foreach (HttpReciever httpReciever in _httpRecievers)
            {
                httpReciever.Stop();
            }
            EnsureNotDisposed();
        }

        #endregion
    }
}