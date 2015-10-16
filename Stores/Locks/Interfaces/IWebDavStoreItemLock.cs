using System;
using System.Collections.Generic;
using System.Xml;
using WebDAVSharp.Server.Stores.Locks.Enums;

namespace WebDAVSharp.Server.Stores.Locks.Interfaces
{
    /// <summary>
    /// </summary>
    public interface IWebDavStoreItemLock
    {
        /// <summary>
        /// </summary>
        bool AllowInfiniteCheckouts { get; set; }

        /// <summary>
        /// </summary>
        long MaxCheckOutSeconds { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="storeItem"></param>
        /// <param name="locktoken"></param>
        /// <param name="requestedlocktimeout"></param>
        /// <param name="requestDocument"></param>
        /// <returns></returns>
        int RefreshLock(IWebDavStoreItem storeItem, Guid? locktoken, double? requestedlocktimeout, out XmlDocument requestDocument);

        /// <summary>
        /// </summary>
        /// <param name="storeItem"></param>
        /// <param name="lockscope"></param>
        /// <param name="locktype"></param>
        /// <param name="lockowner"></param>
        /// <param name="requestedlocktimeout"></param>
        /// <param name="locktoken"></param>
        /// <param name="requestDocument"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        int Lock(IWebDavStoreItem storeItem, WebDavLockScope lockscope, WebDavLockType locktype, string lockowner, double? requestedlocktimeout, out Guid? locktoken, XmlDocument requestDocument, int depth);

        /// <summary>
        /// </summary>
        /// <param name="storeItem"></param>
        /// <param name="locktoken"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        int UnLock(IWebDavStoreItem storeItem, Guid? locktoken, string owner);

        /// <summary>
        /// </summary>
        /// <param name="storeItem"></param>
        /// <returns></returns>
        List<IWebDavStoreItemLockInstance> GetLocks(IWebDavStoreItem storeItem);
    }
}