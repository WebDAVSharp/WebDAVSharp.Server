using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using WebDAVSharp.Server.Exceptions;

namespace WebDAVSharp.Server.Stores.DiskStore
{
    /// <summary>
    ///     This class implements a disk-based <see cref="WebDavDiskStoreDocument" /> mapped to a file.
    /// </summary>
    [DebuggerDisplay("File ({Name})")]
    public sealed class WebDavDiskStoreDocument : WebDavDiskStoreItem, IWebDavStoreDocument
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebDavDiskStoreDocument" /> class.
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
        /// <exception cref="ArgumentNullException"><paramref name="path" /> is <c>null</c> or empty.</exception>
        public WebDavDiskStoreDocument(WebDavDiskStoreCollection parentCollection, string path, IWebDavStore store)
            : base(parentCollection, path, store)
        {
            // Do nothing here
        }

        #endregion

        #region Functions

        /// <summary>
        /// </summary>
        public override long Size => new FileInfo(ItemPath).Length;

        /// <summary>
        ///     Opens a <see cref="Stream" /> object for the document, in read-only mode.
        /// </summary>
        /// <returns>
        ///     The <see cref="Stream" /> object that can be read from.
        /// </returns>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavUnauthorizedException">
        ///     If the user is unauthorized or has no
        ///     access
        /// </exception>
        public Stream OpenReadStream()
        {
            Stream stream;
            try
            {
                // Impersonate the current user and create the file stream for opening the file
                WindowsImpersonationContext wic = UserIdentity.Impersonate();
                stream = new FileStream(ItemPath, FileMode.Open, FileAccess.Read, FileShare.None);
                wic.Undo();
            }
            catch
            {
                throw new WebDavUnauthorizedException();
            }
            return stream;
        }

        /// <summary>
        ///     Opens a <see cref="Stream" /> object for the document, in write-only mode.
        /// </summary>
        /// <param name="append">
        ///     A value indicating whether to append to the existing document;
        ///     if
        ///     <c>false</c>, the existing content will be dropped.
        /// </param>
        /// <returns>
        ///     The <see cref="Stream" /> object that can be written to.
        /// </returns>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavUnauthorizedException">
        ///     If the user is unauthorized or has no
        ///     access
        /// </exception>
        public Stream OpenWriteStream(bool append)
        {
            if (append)
            {
                FileStream result = new FileStream(ItemPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                result.Seek(0, SeekOrigin.End);
                return result;
            }

            Stream stream;
            try
            {
                // Impersonate the current user and create the file stream for writing the file
                WindowsImpersonationContext wic = UserIdentity.Impersonate();
                stream = new FileStream(ItemPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                wic.Undo();
            }
            catch
            {
                throw new WebDavUnauthorizedException();
            }
            return stream;
        }

        #endregion
    }
}