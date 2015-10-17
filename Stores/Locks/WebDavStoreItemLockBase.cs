using System;
using System.Collections.Generic;
using System.Xml;
using WebDAVSharp.Server.Stores.Locks.Enums;
using WebDAVSharp.Server.Stores.Locks.Interfaces;

namespace WebDAVSharp.Server.Stores.Locks
{
    /// <summary>
    /// </summary>
    public abstract class WebDavStoreItemLockBase : IWebDavStoreItemLock
    {
        /// <summary>
        /// </summary>
        public bool AllowInfiniteCheckouts { get; set; }

        /// <summary>
        /// </summary>
        public long MaxCheckOutSeconds { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="storeItem"></param>
        /// <param name="locktoken"></param>
        /// <param name="requestedlocktimeout"></param>
        /// <param name="requestDocument"></param>
        /// <returns></returns>
        public virtual int RefreshLock(IWebDavStoreItem storeItem, Guid? locktoken, double? requestedlocktimeout, out XmlDocument requestDocument)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="lockscope"></param>
        /// <param name="locktype"></param>
        /// <param name="lockowner"></param>
        /// <param name="requestedlocktimeout"></param>
        /// <param name="locktoken"></param>
        /// <param name="requestDocument"></param>
        /// <param name="depth"></param>
        /// <param name="storeItem"></param>
        /// <returns></returns>
        public virtual int Lock(IWebDavStoreItem storeItem, WebDavLockScope lockscope, WebDavLockType locktype, string lockowner, double? requestedlocktimeout, out Guid? locktoken, XmlDocument requestDocument, int depth)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="storeItem"></param>
        /// <param name="locktoken"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public virtual int UnLock(IWebDavStoreItem storeItem, Guid? locktoken, string owner)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// </summary>
        /// <param name="storeItem"></param>
        /// <returns></returns>
        public virtual List<IWebDavStoreItemLockInstance> GetLocks(IWebDavStoreItem storeItem)
        {
            throw new NotImplementedException();
        }
    }
}