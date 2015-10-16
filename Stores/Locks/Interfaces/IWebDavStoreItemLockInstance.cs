using System;
using System.Xml;
using WebDAVSharp.Server.Stores.Locks.Enums;

namespace WebDAVSharp.Server.Stores.Locks.Interfaces
{
    /// <summary>
    /// </summary>
    public interface IWebDavStoreItemLockInstance
    {
        /// <summary>
        ///     The Path locked
        /// </summary>
        string Path { get; }

        /// <summary>
        /// </summary>
        IWebDavStoreItemLock LockSystem { get; set; }

        /// <summary>
        ///     Lock Scope
        /// </summary>
        WebDavLockScope LockScope { get; }

        /// <summary>
        /// </summary>
        double? RequestedLockTimeout { get; set; }

        /// <summary>
        /// </summary>
        DateTime CreateDate { get; set; }

        /// <summary>
        ///     Lock Type
        /// </summary>
        WebDavLockType LockType { get; }

        /// <summary>
        ///     Owner
        /// </summary>
        string Owner { get; }

        /// <summary>
        ///     Requested Timeout
        /// </summary>
        string RequestedTimeout { get; set; }

        /// <summary>
        ///     Token Issued
        /// </summary>
        Guid? Token { get; set; }

        /// <summary>
        ///     Request Document
        /// </summary>
        XmlDocument RequestDocument { get; }

        /// <summary>
        ///     If null, it's an infinite checkout.
        /// </summary>
        DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// </summary>
        int Depth { get; }

        /// <summary>
        ///     Refreshes a lock
        /// </summary>
        /// <param name="requestedlocktimeout"></param>
        void RefreshLock(double? requestedlocktimeout);
    }
}