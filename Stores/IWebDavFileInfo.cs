using System;
using System.IO;

namespace WebDAVSharp.Server.Stores
{
    /// <summary>
    /// </summary>
    public interface IWebDavFileInfo
    {
        /// <summary>
        /// </summary>
        bool Exists { get; set; }

        /// <summary>
        ///     File should be archived.
        /// </summary>
        bool Archive { get; set; }

        /// <summary>
        ///     Compress file contents.
        /// </summary>
        bool Compressed { get; set; }

        /// <summary>
        ///     Device file.
        /// </summary>
        bool Device { get; set; }

        /// <summary>
        /// </summary>
        bool Directory { get; set; }

        /// <summary>
        ///     Encrypted file.
        /// </summary>
        bool Encrypted { get; set; }

        /// <summary>
        ///     Hidden file.
        /// </summary>
        bool Hidden { get; set; }

        /// <summary>
        /// </summary>
        bool IntegrityStream { get; set; }

        /// <summary>
        ///     Normal file.
        /// </summary>
        bool Normal { get; set; }

        /// <summary>
        /// </summary>
        bool NoScrubData { get; set; }

        /// <summary>
        ///     File should not be indexed by the content indexing service.
        /// </summary>
        bool NotContentIndexed { get; set; }

        /// <summary>
        /// </summary>
        bool Offline { get; set; }

        /// <summary>
        ///     Read-only file.
        /// </summary>
        bool ReadOnly { get; set; }

        /// <summary>
        ///     Reparse point.
        /// </summary>
        bool ReparsePoint { get; set; }

        /// <summary>
        ///     Sparse file.
        /// </summary>
        bool SparseFile { get; set; }

        /// <summary>
        ///     System file.
        /// </summary>
        bool System { get; set; }

        /// <summary>
        ///     File is temporary (should be kept in cache and not
        ///     written to disk if possible).
        /// </summary>
        bool Temporary { get; set; }

        /// <summary>
        /// </summary>
        DateTime CreationTime { get; set; }

        /// <summary>
        /// </summary>
        DateTime LastAccessTime { get; set; }

        /// <summary>
        /// </summary>
        DateTime? LastWriteTime { get; set; }

        /// <summary>
        /// </summary>
        void Apply();

        /// <summary>
        /// </summary>
        /// <returns></returns>
        FileAttributes GetAttributes();

        /// <summary>
        /// </summary>
        /// <param name="fa"></param>
        void ApplyAttributes(FileAttributes fa);
    }
}