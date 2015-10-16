using System;

namespace WebDAVSharp.Server.Stores.BaseClasses
{
    /// <summary>
    ///     This is the base class for <see cref="IWebDavStoreItem" /> implementations.
    /// </summary>
    public class WebDavStoreDocumentBase : WebDavStoreItemBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavStoreItemBase" /> class.
        /// </summary>
        /// <param name="parentCollection">
        ///     The parent <see cref="IWebDavStoreCollection" /> that contains this
        ///     <see cref="IWebDavStoreItem" /> implementation.
        /// </param>
        /// <param name="name">The name of this <see cref="IWebDavStoreItem" /></param>
        /// <param name="store"></param>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <c>null</c>.</exception>
        protected WebDavStoreDocumentBase(IWebDavStoreCollection parentCollection, string name, IWebDavStore store)
            : base(parentCollection, name, store)
        {
        }
    }
}