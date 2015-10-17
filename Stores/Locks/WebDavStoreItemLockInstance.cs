using System;
using System.Xml;
using WebDAVSharp.Server.Stores.Locks.Enums;
using WebDAVSharp.Server.Stores.Locks.Interfaces;

namespace WebDAVSharp.Server.Stores.Locks
{
    /// <summary>
    ///     Used to store locks on objects
    /// </summary>
    public class WebDavStoreItemLockInstance : WebDavStoreItemLockInstanceBase
    {
        /// <summary>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lockscope"></param>
        /// <param name="locktype"></param>
        /// <param name="owner"></param>
        /// <param name="requestedlocktimeout"></param>
        /// <param name="token"></param>
        /// <param name="requestdocument"></param>
        /// <param name="depth"></param>
        /// <param name="lockSystem"></param>
        /// <param name="createdate"></param>
        public WebDavStoreItemLockInstance(string path, WebDavLockScope lockscope, WebDavLockType locktype, string owner,
            double? requestedlocktimeout, Guid? token, XmlDocument requestdocument, int depth, IWebDavStoreItemLock lockSystem, DateTime? createdate = null)
            : base(path, lockscope, locktype, owner, requestedlocktimeout, token, requestdocument, depth, lockSystem, createdate)
        {
        }
    }
}