using System;
using System.IO;
using WebDAVSharp.Server.Stores.BaseClasses;
using static System.String;

namespace WebDAVSharp.Server.Stores.DiskStore
{
    /// <summary>
    ///     This class implements a disk-based
    ///     <see cref="IWebDavStoreItem" /> which can be either
    ///     a folder on disk (
    ///     <see cref="WebDavDiskStoreCollection" />) or a file on disk
    ///     (
    ///     <see cref="WebDavDiskStoreDocument" />).
    /// </summary>
    public class WebDavDiskStoreItem : WebDavStoreItemBase
    {
        #region Variables

        private readonly string _path;

        #endregion

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavDiskStoreItem" /> class.
        /// </summary>
        /// <param name="parentCollection">
        ///     The parent
        ///     <see cref="WebDavDiskStoreCollection" /> that contains this
        ///     <see cref="WebDavDiskStoreItem" />;
        ///     or
        ///     <c>null</c> if this is the root
        ///     <see cref="WebDavDiskStoreCollection" />.
        /// </param>
        /// <param name="path">The path that this <see cref="WebDavDiskStoreItem" /> maps to.</param>
        /// <param name="store"></param>
        /// <exception cref="System.ArgumentNullException">path</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path" /> is <c>null</c> or empty.</exception>
        protected WebDavDiskStoreItem(WebDavDiskStoreCollection parentCollection, string path, IWebDavStore store)
            : base(parentCollection, path, store)
        {
            if (IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));
            _path = path;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the path to this <see cref="WebDavDiskStoreItem" />.
        /// </summary>
        public override string ItemPath => _path;

        /// <summary>
        ///     Gets or sets the name of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Unable to rename item</exception>
        public new string Name
        {
            get { return Path.GetFileName(_path); }

            set { throw new InvalidOperationException("Unable to rename item"); }
        }

        /// <summary>
        ///     Gets if this <see cref="IWebDavStoreItem" /> is a collection.
        /// </summary>
        public new bool IsCollection
        {
            get
            {
                // get the file attributes for file or directory
                FileAttributes attr = File.GetAttributes(_path);

                //detect whether its a directory or file
                return (attr & FileAttributes.Directory) == FileAttributes.Directory;
            }
        }

        /// <summary>
        ///     Gets the creation date of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public override DateTime CreationDate => File.GetCreationTime(_path);

        /// <summary>
        ///     Gets the modification date of this <see cref="IWebDavStoreItem" />.
        /// </summary>
        public override DateTime ModificationDate => File.GetLastWriteTime(_path);


        /// <summary>
        ///     Returns the fileInfo.
        /// </summary>
        /// <returns></returns>
        public override IWebDavFileInfo GetFileInfo()
        {
            FileInfo fi = new FileInfo(ItemPath);

            if (!fi.Exists)
                return new WebDavDiskStoreFileInfo
                {
                    Exists = false
                };
            return new WebDavDiskStoreFileInfo
            {
                Path = ItemPath,
                Exists = true,
                CreationTime = fi.CreationTime,
                LastAccessTime = fi.LastAccessTime,
                LastWriteTime = fi.LastWriteTime,
                Archive = (fi.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly,
                Compressed = (fi.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed,
                Device = (fi.Attributes & FileAttributes.Device) == FileAttributes.Device,
                Directory = (fi.Attributes & FileAttributes.Directory) == FileAttributes.Directory,
                Encrypted = (fi.Attributes & FileAttributes.Encrypted) == FileAttributes.Encrypted,
                Hidden = (fi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden,
                IntegrityStream = (fi.Attributes & FileAttributes.IntegrityStream) == FileAttributes.IntegrityStream,
                Normal = (fi.Attributes & FileAttributes.Normal) == FileAttributes.Normal,
                NoScrubData = (fi.Attributes & FileAttributes.NoScrubData) == FileAttributes.NoScrubData,
                NotContentIndexed = (fi.Attributes & FileAttributes.NotContentIndexed) == FileAttributes.NotContentIndexed,
                Offline = (fi.Attributes & FileAttributes.Offline) == FileAttributes.Offline,
                ReadOnly = (fi.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly,
                ReparsePoint = (fi.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint,
                SparseFile = (fi.Attributes & FileAttributes.SparseFile) == FileAttributes.SparseFile,
                System = (fi.Attributes & FileAttributes.System) == FileAttributes.System,
                Temporary = (fi.Attributes & FileAttributes.Temporary) == FileAttributes.Temporary
            };
        }

        #endregion
    }
}