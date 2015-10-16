using System.Security.Principal;
using WebDAVSharp.Server.MethodHandlers;
using WebDAVSharp.Server.Stores.Locks.Interfaces;

namespace WebDAVSharp.Server.Stores
{
    /// <summary>
    ///     This interface must be implemented by classes that serve as stores of collections and
    ///     documents for the
    ///     <see cref="WebDavServer" />.
    /// </summary>
    public interface IWebDavStore
    {
        /// <summary>
        ///     Gets the root collection of this <see cref="IWebDavStore" />.
        /// </summary>
        /// <value>
        ///     The root.
        /// </value>
        IWebDavStoreCollection Root { get; }

        /// <summary>
        /// </summary>
        IWebDavStoreItemLock LockSystem { get; set; }

        //   /// <summary>
        //  /// </summary>
        // WebDavServer.ClearCaches FClearCaches { get; set; }

        /// <summary>
        /// </summary>
        void UserAuthenticated(IIdentity ident);

        /// <summary>
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="user"></param>
        /// <param name="path"></param>
        /// <param name="value"></param>
        void AddCacheObject(WebDavMethodHandlerBase handler, string user, string path, object value);

        /// <summary>
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="user"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        object GetCacheObject(WebDavMethodHandlerBase handler, string user, string path);

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        void RemoveCacheObject(string key);
    }
}