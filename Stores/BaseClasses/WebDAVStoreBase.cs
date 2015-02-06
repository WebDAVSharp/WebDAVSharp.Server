using System;

namespace WebDAVSharp.Server.Stores.BaseClasses
{
    /// <summary>
    /// This class is a base class for <see cref="IWebDavStore" /> implementations.
    /// </summary>
    public abstract class WebDavStoreBase : IWebDavStore
    {
        private readonly IWebDavStoreCollection _root;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebDavStoreBase" /> class.
        /// </summary>
        /// <param name="root">The root <see cref="IWebDavStoreCollection" />.</param>
        /// <exception cref="System.ArgumentNullException">root</exception>
        /// <exception cref="ArgumentNullException"><paramref name="root" /> is <c>null</c>.</exception>
        protected WebDavStoreBase(IWebDavStoreCollection root)
        {
            if (root == null)
                throw new ArgumentNullException("root");

            _root = root;
        }

        #region IWebDAVStore Members

        /// <summary>
        /// Gets the root collection of this <see cref="IWebDavStore" />.
        /// </summary>
        public IWebDavStoreCollection Root
        {
            get
            {
                return _root;
            }
        }

        #endregion
    }
}