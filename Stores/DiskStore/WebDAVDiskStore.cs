using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using WebDAVSharp.Server.Stores.BaseClasses;
using WebDAVSharp.Server.Stores.Locks.Interfaces;

namespace WebDAVSharp.Server.Stores.DiskStore
{
    /// <summary>
    ///     This class implements a disk-based <see cref="IWebDavStore" />.
    /// </summary>
    [DebuggerDisplay("Disk Store ({RootPath})")]
    public sealed class WebDavDiskStore : WebDavStoreBase
    {
        #region Variables

        private readonly string _rootPath;

        #endregion

        #region Constructor

        ///
        public WebDavDiskStore(IWebDavStoreCollection root, IWebDavStoreItemLock lockSystem)
            : base(root, lockSystem)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavDiskStore" /> class.
        /// </summary>
        /// <param name="rootPath">The root path of a folder on disk to host in this <see cref="WebDavDiskStore" />.</param>
        /// <param name="lockSystem"></param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        /// <exception cref="ArgumentNullException"><paramref name="rootPath" /> is <c>null</c>.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="rootPath" /> specifies a folder that does not exist.</exception>
        public WebDavDiskStore(string rootPath, IWebDavStoreItemLock lockSystem)
            : base(lockSystem)
        {
            if (rootPath == null)
                throw new ArgumentNullException(nameof(rootPath));
            if (!Directory.Exists(rootPath))
                throw new DirectoryNotFoundException(rootPath);

            _rootPath = rootPath;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the root path for the folder that is hosted in this <see cref="WebDavDiskStore" />.
        /// </summary>
        /// <value>
        ///     The root path.
        /// </value>
        public string RootPath => _rootPath;

        /// <summary>
        ///     Gets the root collection of this <see cref="IWebDavStore" />.
        /// </summary>
        public override IWebDavStoreCollection Root => new WebDavDiskStoreCollection(null, _rootPath, this);

        /// <summary>
        /// </summary>
        /// <param name="ident"></param>
        public override void UserAuthenticated(IIdentity ident)
        {
        }

        #endregion
    }
}