using System;
using System.Security.Principal;
using WebDAVSharp.Server.MethodHandlers;
using WebDAVSharp.Server.Stores.Locks.Interfaces;
using WebDAVSharp.Server.Utilities;

namespace WebDAVSharp.Server.Stores.BaseClasses
{
    /// <summary>
    ///     This class is a base class for <see cref="IWebDavStore" /> implementations.
    /// </summary>
    public abstract class WebDavStoreBase : IWebDavStore
    {
        #region Variables

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavStoreBase" /> class.
        /// </summary>
        /// <param name="root">The root <see cref="IWebDavStoreCollection" />.</param>
        /// <param name="lockSystem"></param>
        /// <exception cref="System.ArgumentNullException">root</exception>
        /// <exception cref="ArgumentNullException"><paramref name="root" /> is <c>null</c>.</exception>
        protected WebDavStoreBase(IWebDavStoreCollection root, IWebDavStoreItemLock lockSystem)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (lockSystem == null)
                throw new ArgumentNullException(nameof(lockSystem));

            LockSystem = lockSystem;

            Root = root;
        }

        /// <summary>
        /// </summary>
        protected WebDavStoreBase(IWebDavStoreItemLock lockSystem)
        {
            LockSystem = lockSystem;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the root collection of this <see cref="IWebDavStore" />.
        /// </summary>
        public virtual IWebDavStoreCollection Root { get; }

        /// <summary>
        /// </summary>
        public IWebDavStoreItemLock LockSystem { get; set; }

        ///// <summary>
        ///// </summary>
        //public WebDavServer.ClearCaches FClearCaches { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="ident"></param>
        public abstract void UserAuthenticated(IIdentity ident);


        private readonly WebDavCacheBase _cache = new WebDavCacheBase();

        /// <summary>
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="user"></param>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <param name="timeToExpire"></param>
        internal void AddCacheObject(WebDavMethodHandlerBase handler, string user, string path, object value, TimeSpan timeToExpire)
        {
            WebDavCacheBase pathCache = (WebDavCacheBase) _cache.GetCachedObject(path);
            WebDavCacheBase handlerCache;

            if (pathCache != null)
            {
                handlerCache = (WebDavCacheBase) pathCache.GetCachedObject(handler.GetType().FullName);
                if (handlerCache != null)
                {
                    handlerCache.AddCacheObject(user, value, timeToExpire);
                    return;
                }
                handlerCache = new WebDavCacheBase();
                handlerCache.AddCacheObject(user, value, timeToExpire);
                pathCache.AddCacheObject(handler.GetType().FullName, handlerCache, timeToExpire);
                return;
            }
            pathCache = new WebDavCacheBase();
            handlerCache = new WebDavCacheBase();
            handlerCache.AddCacheObject(user, value, timeToExpire);
            pathCache.AddCacheObject(handler.GetType().FullName, handlerCache, timeToExpire);
            _cache.AddCacheObject(path, pathCache, timeToExpire);
        }

        /// <summary>
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="user"></param>
        /// <param name="path"></param>
        /// <param name="value"></param>
        public void AddCacheObject(WebDavMethodHandlerBase handler, string user, string path, object value)
        {
            WebDavCacheBase pathCache = (WebDavCacheBase) _cache.GetCachedObject(path);
            WebDavCacheBase handlerCache;

            if (pathCache != null)
            {
                handlerCache = (WebDavCacheBase) pathCache.GetCachedObject(handler.GetType().FullName);
                if (handlerCache != null)
                {
                    handlerCache.AddCacheObject(user, value);
                    return;
                }
                handlerCache = new WebDavCacheBase();
                handlerCache.AddCacheObject(user, value);
                pathCache.AddCacheObject(handler.GetType().FullName, handlerCache);
                return;
            }
            pathCache = new WebDavCacheBase();
            handlerCache = new WebDavCacheBase();
            handlerCache.AddCacheObject(user, value);
            pathCache.AddCacheObject(handler.GetType().FullName, handlerCache);
            _cache.AddCacheObject(path, pathCache);
        }

        /// <summary>
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="user"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public object GetCacheObject(WebDavMethodHandlerBase handler, string user, string path)
        {
            return ((WebDavCacheBase) (((WebDavCacheBase) _cache.GetCachedObject(path))?.GetCachedObject(handler.GetType().FullName)))?.GetCachedObject(user);
        }

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        public void RemoveCacheObject(string key)
        {
            _cache.RemoveCacheObject(key);
        }

        #endregion
    }
}