using System;
using System.IO;

namespace WebDAVSharp.Server.Stores.BaseClasses
{
    /// <summary>
    /// </summary>
    public abstract class WebDavFileInfoBase : IWebDavFileInfo
    {
        /// <summary>
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///     Whether the file exists
        /// </summary>
        public bool Exists { get; set; }

        /// <summary>
        /// </summary>
        public bool Archive { get; set; }

        /// <summary>
        /// </summary>
        public bool Compressed { get; set; }

        /// <summary>
        /// </summary>
        public bool Device { get; set; }

        /// <summary>
        /// </summary>
        public bool Directory { get; set; }

        /// <summary>
        /// </summary>
        public bool Encrypted { get; set; }

        /// <summary>
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// </summary>
        public bool IntegrityStream { get; set; }

        /// <summary>
        /// </summary>
        public bool Normal { get; set; }

        /// <summary>
        /// </summary>
        public bool NoScrubData { get; set; }

        /// <summary>
        /// </summary>
        public bool NotContentIndexed { get; set; }

        /// <summary>
        /// </summary>
        public bool Offline { get; set; }

        /// <summary>
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// </summary>
        public bool ReparsePoint { get; set; }

        /// <summary>
        /// </summary>
        public bool SparseFile { get; set; }

        /// <summary>
        /// </summary>
        public bool System { get; set; }

        /// <summary>
        /// </summary>
        public bool Temporary { get; set; }

        /// <summary>
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>
        /// </summary>
        public DateTime? LastWriteTime { get; set; }

        /// <summary>
        /// </summary>
        public abstract void Apply();

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public FileAttributes GetAttributes()
        {
            FileAttributes fa = new FileAttributes();
            if (Archive)
                fa = fa | FileAttributes.Archive;
            if (Compressed)
                fa = fa | FileAttributes.Compressed;
            if (Device)
                fa = fa | FileAttributes.Device;
            if (Directory)
                fa = fa | FileAttributes.Directory;
            if (Encrypted)
                fa = fa | FileAttributes.Encrypted;
            if (Hidden)
                fa = fa | FileAttributes.Hidden;
            if (IntegrityStream)
                fa = fa | FileAttributes.IntegrityStream;
            if (Normal)
                fa = fa | FileAttributes.Normal;
            if (NoScrubData)
                fa = fa | FileAttributes.NoScrubData;
            if (NotContentIndexed)
                fa = fa | FileAttributes.NotContentIndexed;
            if (Offline)
                fa = fa | FileAttributes.Offline;
            if (ReadOnly)
                fa = fa | FileAttributes.ReadOnly;
            if (ReparsePoint)
                fa = fa | FileAttributes.ReparsePoint;
            if (SparseFile)
                fa = fa | FileAttributes.SparseFile;
            if (System)
                fa = fa | FileAttributes.System;
            if (Temporary)
                fa = fa | FileAttributes.Temporary;
            return fa;
        }

        /// <summary>
        /// </summary>
        /// <param name="fa"></param>
        public void ApplyAttributes(FileAttributes fa)
        {
            Archive = (fa & FileAttributes.Archive) == FileAttributes.Archive;
            Compressed = (fa & FileAttributes.Compressed) == FileAttributes.Compressed;
            Device = (fa & FileAttributes.Device) == FileAttributes.Device;
            Directory = (fa & FileAttributes.Directory) == FileAttributes.Directory;
            Encrypted = (fa & FileAttributes.Encrypted) == FileAttributes.Encrypted;
            Hidden = (fa & FileAttributes.Hidden) == FileAttributes.Hidden;
            IntegrityStream = (fa & FileAttributes.IntegrityStream) == FileAttributes.IntegrityStream;
            Normal = (fa & FileAttributes.Normal) == FileAttributes.Normal;
            NoScrubData = (fa & FileAttributes.NoScrubData) == FileAttributes.NoScrubData;
            NotContentIndexed = (fa & FileAttributes.NotContentIndexed) == FileAttributes.NotContentIndexed;
            Offline = (fa & FileAttributes.Offline) == FileAttributes.Offline;
            ReadOnly = (fa & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            ReparsePoint = (fa & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
            SparseFile = (fa & FileAttributes.SparseFile) == FileAttributes.SparseFile;
            System = (fa & FileAttributes.System) == FileAttributes.System;
            Temporary = (fa & FileAttributes.Temporary) == FileAttributes.Temporary;
        }
    }
}