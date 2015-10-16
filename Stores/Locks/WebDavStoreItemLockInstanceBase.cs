using System;
using System.Xml;
using WebDAVSharp.Server.Stores.Locks.Enums;
using WebDAVSharp.Server.Stores.Locks.Interfaces;

namespace WebDAVSharp.Server.Stores.Locks
{
    /// <summary>
    /// </summary>
    public abstract class WebDavStoreItemLockInstanceBase : IWebDavStoreItemLockInstance
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
        protected WebDavStoreItemLockInstanceBase(string path, WebDavLockScope lockscope, WebDavLockType locktype, string owner, double? requestedlocktimeout, Guid? token, XmlDocument requestdocument, int depth, IWebDavStoreItemLock lockSystem, DateTime? createdate = null)
        {
            Path = path;
            LockScope = lockscope;
            LockType = locktype;
            Owner = owner;
            Token = token;
            RequestDocument = requestdocument;
            Token = token; // token = "urn:uuid:" + Guid.NewGuid();     
            Depth = depth;
            LockSystem = lockSystem;

            if (createdate == null)
                CreateDate = DateTime.Now;
            else
                CreateDate = (DateTime) createdate;

            RequestedLockTimeout = requestedlocktimeout;

            RefreshLock(requestedlocktimeout);
        }

        /// <summary>
        ///     Refreshes a lock
        /// </summary>
        /// <param name="seconds"></param>
        public void RefreshLock(double? seconds)
        {
            if (seconds == null)
            {
                if (LockSystem.AllowInfiniteCheckouts)
                {
                    ExpirationDate = null;
                    RequestedTimeout = "Infinite, Second-4100000000";
                    return;
                }
                RequestedTimeout = "Second-" + LockSystem.MaxCheckOutSeconds;
                ExpirationDate = CreateDate.AddSeconds(LockSystem.MaxCheckOutSeconds);
                return;
            }
            if (seconds > LockSystem.MaxCheckOutSeconds)
                seconds = LockSystem.MaxCheckOutSeconds;
            ExpirationDate = CreateDate + TimeSpan.FromSeconds((double) seconds);
            RequestedTimeout = "Second-" + seconds;
        }

        /// <summary>
        /// </summary>
        public double? RequestedLockTimeout { get; set; }

        /// <summary>
        ///     The Path locked
        /// </summary>
        public virtual string Path { get; }

        /// <summary>
        /// </summary>
        public IWebDavStoreItemLock LockSystem { get; set; }

        /// <summary>
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        ///     Lock Scope
        /// </summary>
        public virtual WebDavLockScope LockScope { get; }

        /// <summary>
        ///     Lock Type
        /// </summary>
        public virtual WebDavLockType LockType { get; }

        /// <summary>
        ///     Owner
        /// </summary>
        public virtual string Owner { get; }

        /// <summary>
        ///     Requested Timeout
        /// </summary>
        public string RequestedTimeout { get; set; }

        /// <summary>
        ///     Token Issued
        /// </summary>
        public Guid? Token { get; set; }

        /// <summary>
        ///     Request Document
        /// </summary>
        public XmlDocument RequestDocument { get; }

        /// <summary>
        ///     If null, it's an infinite checkout.
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// </summary>
        public int Depth { get; }
    }
}