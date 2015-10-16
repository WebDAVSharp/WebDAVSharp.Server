using System.IO;

namespace WebDAVSharp.Server.Stores
{
    /// <summary>
    ///     This interface must be implemented by classes that will function as a store document.
    /// </summary>
    public interface IWebDavStoreDocument : IWebDavStoreItem
    {
        /// <summary>
        ///     Opens a <see cref="Stream" /> object for the document, in read-only mode.
        /// </summary>
        /// <returns>
        ///     The <see cref="Stream" /> object that can be read from.
        /// </returns>
        Stream OpenReadStream();

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
        Stream OpenWriteStream(bool append);
    }
}