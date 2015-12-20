﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WebDAVSharp.Server.Stores.Locks
{
    /// <summary>
    /// Used to store locks on objects
    /// </summary>
    public class WebDaveStoreItemLockInstance
    {
        /// <summary>
        /// The Path locked
        /// </summary>
        public Uri Path
        {
            get;
            private set;
        }


        /// <summary>
        /// Lock Scope
        /// </summary>
        public WebDavLockScope LockScope
        {
            get;
            private set;
        }

        /// <summary>
        /// Lock Type
        /// </summary>
        public WebDavLockType LockType
        {
            get;
            private set;
        }

        /// <summary>
        /// Owner
        /// </summary>
        public string Owner
        {
            get;
            private set;
        }

        /// <summary>
        /// Requested Timeout
        /// </summary>
        public string RequestedTimeout
        {
            get;
            private set;
        }

        /// <summary>
        /// Token Issued
        /// </summary>
        public string Token
        {
            get;
            private set;
        }

        /// <summary>
        /// Request Document
        /// </summary>
        public XmlDocument RequestDocument
        {
            get;
            private set;
        }

        /// <summary>
        /// If null, it's an infinite checkout.
        /// </summary>
        public DateTime? ExpirationDate
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Depth
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lockscope"></param>
        /// <param name="locktype"></param>
        /// <param name="owner"></param>
        /// <param name="requestedlocktimeout"></param>
        /// <param name="token"></param>
        /// <param name="requestdocument"></param>
        /// <param name="depth"></param>
        public WebDaveStoreItemLockInstance(Uri path, WebDavLockScope lockscope, WebDavLockType locktype, string owner, ref string requestedlocktimeout, ref string token, XmlDocument requestdocument, int depth)
        {
            Path = path;
            LockScope = lockscope;
            LockType = locktype;
            Owner = owner;
            Token = token;
            RequestDocument = requestdocument;
            Token = token = "urn:uuid:" + Guid.NewGuid();
            Depth = depth;
            RefreshLock(ref requestedlocktimeout);
        }

        /// <summary>
        /// Refreshes a lock
        /// </summary>
        /// <param name="requestedlocktimeout"></param>
        public void RefreshLock(ref string requestedlocktimeout)
        {
            if (requestedlocktimeout.Contains("Infinite") && WebDavStoreItemLock.AllowInfiniteCheckouts)
            {
                requestedlocktimeout = "Infinite";
                ExpirationDate = null;
                return;
            }
            string seconds = requestedlocktimeout.Substring(requestedlocktimeout.IndexOf("Second-", System.StringComparison.Ordinal) + 7);
            long lseconds;
            if (long.TryParse(seconds, out lseconds))
            {
                if (lseconds > WebDavStoreItemLock.MaxCheckOutSeconds)
                    lseconds = WebDavStoreItemLock.MaxCheckOutSeconds;
                RequestedTimeout = requestedlocktimeout = "Second-" + lseconds;

                //Due to latency, if the seconds granted is less than max seconds - 60 then we give them a 1 minute buffer.
                ExpirationDate = DateTime.Now.AddSeconds(lseconds < long.MaxValue - 60 ? lseconds + 60 : lseconds);
            }
            else
            {
                RequestedTimeout = requestedlocktimeout = "Second-" + WebDavStoreItemLock.MaxCheckOutSeconds;
                ExpirationDate = DateTime.Now.AddSeconds(WebDavStoreItemLock.MaxCheckOutSeconds);
            }
        }

    }
}
