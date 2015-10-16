using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using WebDAVSharp.Server.Stores.Locks.Enums;
using WebDAVSharp.Server.Stores.Locks.Interfaces;

namespace WebDAVSharp.Server.Stores.Locks
{
    /// <summary>
    ///     This class provides the locking functionality.
    /// </summary>
    public class WebDavStoreItemLock : WebDavStoreItemLockBase
    {
        /// <summary>
        /// </summary>
        public Dictionary<string, List<IWebDavStoreItemLockInstance>> ObjectLocks = new Dictionary<string, List<IWebDavStoreItemLockInstance>>();

        /// <summary>
        /// </summary>
        /// <param name="storeItem"></param>
        public void CleanLocks(IWebDavStoreItem storeItem)
        {
            lock (ObjectLocks)
            {
                if (!ObjectLocks.ContainsKey(storeItem.ItemPath))
                    return;
                foreach (
                    IWebDavStoreItemLockInstance ilock in ObjectLocks[storeItem.ItemPath].ToList()
                        .Where(ilock => ilock.ExpirationDate != null && (DateTime) ilock.ExpirationDate < DateTime.Now)
                    )
                    ObjectLocks[storeItem.ItemPath].Remove(ilock);
            }
        }

        /// <summary>
        ///     This function will refresh an existing lock.
        /// </summary>
        /// <param name="storeItem">Target URI to the file or folder </param>
        /// <param name="locktoken">The token issued when the lock was established</param>
        /// <param name="requestedlocktimeout">The requested timeout</param>
        /// <param name="requestDocument">
        ///     Output parameter, returns the Request document that was used when the lock was
        ///     established.
        /// </param>
        /// <returns></returns>
        public override int RefreshLock(IWebDavStoreItem storeItem, Guid? locktoken, double? requestedlocktimeout, out XmlDocument requestDocument)
        {
            CleanLocks(storeItem);
            //Refreshing an existing lock

            //If a lock doesn't exist then lets just reply with a Precondition Failed.
            //412 (Precondition Failed), with 'lock-token-matches-request-uri' precondition code - The LOCK request was 
            //made with an If header, indicating that the client wishes to refresh the given lock. However, the Request-URI 
            //did not fall within the scope of the lock identified by the token. The lock may have a scope that does not 
            //include the Request-URI, or the lock could have disappeared, or the token may be invalid.
            requestDocument = null;

            lock (ObjectLocks)
            {
                IWebDavStoreItemLockInstance ilock = ObjectLocks[storeItem.ItemPath].FirstOrDefault(d => (d.Token == locktoken));
                if (ilock == null)
                {
#if DEBUG
                    WebDavServer.Log.Debug("Lock Refresh Failed , Lock does not exist.");
#endif
                    return 412;
                }
#if DEBUG
                WebDavServer.Log.Debug("Lock Refresh Successful.");
#endif
                ilock.RefreshLock(requestedlocktimeout);
                requestDocument = ilock.RequestDocument;

                return (int) HttpStatusCode.OK;
            }
        }

        /// <summary>
        ///     Locks the request Path.
        /// </summary>
        /// <param name="storeItem">URI to the item to be locked</param>
        /// <param name="lockscope">The lock Scope used for locking</param>
        /// <param name="locktype">The lock Type used for locking</param>
        /// <param name="lockowner">The owner of the lock</param>
        /// <param name="requestedlocktimeout">The requested timeout</param>
        /// <param name="locktoken">Out parameter, returns the issued token</param>
        /// <param name="requestDocument">the Request Document</param>
        /// <param name="depth">How deep to lock, 0,1, or infinity</param>
        /// <returns></returns>
        public override int Lock(IWebDavStoreItem storeItem, WebDavLockScope lockscope, WebDavLockType locktype, string lockowner,
            double? requestedlocktimeout, out Guid? locktoken, XmlDocument requestDocument, int depth)
        {
            CleanLocks(storeItem);
#if DEBUG
            WebDavServer.Log.Info("Lock Requested Timeout:" + requestedlocktimeout);
#endif
            locktoken = null;
            Guid tmpLockToken = Guid.NewGuid();

            lock (ObjectLocks)
            {
                /*
            The table below describes the behavior that occurs when a lock request is made on a resource.
            Current State   Shared Lock OK      Exclusive Lock OK
            None	            True	            True
            Shared Lock     	True            	False
            Exclusive Lock	    False	            False*

            Legend: True = lock may be granted. False = lock MUST NOT be granted. *=It is illegal for a principal to request the same lock twice.

            The current lock state of a resource is given in the leftmost column, and lock requests are listed in the first row. The intersection of a row and column gives the result of a lock request. For example, if a shared lock is held on a resource, and an exclusive lock is requested, the table entry is "false", indicating that the lock must not be granted.            
             */


                //if ObjectLocks doesn't contain the path, then this is a new lock and regardless
                //of whether it is Exclusive or Shared it is successful.
                if (!ObjectLocks.ContainsKey(storeItem.ItemPath))
                {
                    ObjectLocks.Add(storeItem.ItemPath, new List<IWebDavStoreItemLockInstance>());
                    ObjectLocks[storeItem.ItemPath].Add(new WebDavStoreItemLockInstance(storeItem.ItemPath, lockscope, locktype, lockowner, requestedlocktimeout, tmpLockToken, requestDocument, depth, this));
                    locktoken = tmpLockToken;
#if DEBUG
                    WebDavServer.Log.Debug("Created New Lock (" + lockscope + "), URI had no locks.  Timeout:" +
                                           requestedlocktimeout);
#endif
                    return (int) HttpStatusCode.OK;
                }
                if (ObjectLocks[storeItem.ItemPath].Count == 0)
                {
                    ObjectLocks[storeItem.ItemPath].Add(new WebDavStoreItemLockInstance(storeItem.ItemPath, lockscope, locktype, lockowner, requestedlocktimeout, tmpLockToken, requestDocument, depth, this));
                    locktoken = tmpLockToken;
                    return (int) HttpStatusCode.OK;
                }

                //The fact that ObjectLocks contains this URI means that there is already a lock on this object,
                //This means the lock fails because you can only have 1 exclusive lock.
                switch (lockscope)
                {
                    case WebDavLockScope.Exclusive:
#if DEBUG
                    WebDavServer.Log.Debug("Lock Creation Failed (Exclusive), URI already has a lock.");
#endif
                        return 423;
                    case WebDavLockScope.Shared:
                        if (ObjectLocks[storeItem.ItemPath].Any(itemLock => itemLock.LockScope == WebDavLockScope.Exclusive))
                        {
#if DEBUG
                        WebDavServer.Log.Debug("Lock Creation Failed (Shared), URI has exclusive lock.");
#endif
                            return 423;
                        }
                        break;
                }

                //If the scope is shared and all other locks on this uri are shared we are ok, otherwise we fail.
                //423 (Locked), potentially with 'no-conflicting-lock' precondition code - 
                //There is already a lock on the resource that is not compatible with the 
                //requested lock (see lock compatibility table above).

                //If it gets to here, then we are most likely creating another shared lock on the file.

                #region Create New Lock

                ObjectLocks[storeItem.ItemPath].Add(new WebDavStoreItemLockInstance(storeItem.ItemPath, lockscope, locktype, lockowner, requestedlocktimeout, tmpLockToken, requestDocument, depth, this));
                locktoken = tmpLockToken;
#if DEBUG
                WebDavServer.Log.Debug("Created New Lock (" + lockscope + "), URI had no locks.  Timeout:" +
                                       requestedlocktimeout);
#endif

                #endregion

                return (int) HttpStatusCode.OK;
            }
        }

        /// <summary>
        ///     Unlocks the URI passed if the token matches a lock token in use.
        /// </summary>
        /// <param name="storeItem">URI to resource</param>
        /// <param name="locktoken">Token used to lock.</param>
        /// <param name="owner">Owner.</param>
        /// <returns></returns>
        public override int UnLock(IWebDavStoreItem storeItem, Guid? locktoken, string owner)
        {
            CleanLocks(storeItem);
            if (locktoken == null)
            {
#if DEBUG
                WebDavServer.Log.Debug("Unlock failed, No Token!.");
#endif
                return (int) HttpStatusCode.BadRequest;
            }

            lock (ObjectLocks)
            {
                if (!ObjectLocks.ContainsKey(storeItem.ItemPath))
                {
#if DEBUG
                    WebDavServer.Log.Debug("Unlock failed, Lock does not exist!.");
#endif
                    return (int) HttpStatusCode.Conflict;
                }

                WebDavStoreItemLockInstance ilock = (WebDavStoreItemLockInstance) ObjectLocks[storeItem.ItemPath].FirstOrDefault(d => d.Token == locktoken && d.Owner == owner);
                if (ilock == null)
                    return (int) HttpStatusCode.Conflict;
                //Remove the lock
                ObjectLocks[storeItem.ItemPath].Remove(ilock);

                //if there are no locks left of the uri then remove it from ObjectLocks.
                if (ObjectLocks[storeItem.ItemPath].Count == 0)
                    ObjectLocks.Remove(storeItem.ItemPath);
#if DEBUG
                WebDavServer.Log.Debug("Unlock successful.");
#endif
                return (int) HttpStatusCode.NoContent;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="storeItem"></param>
        /// <returns></returns>
        public override List<IWebDavStoreItemLockInstance> GetLocks(IWebDavStoreItem storeItem)
        {
            lock (ObjectLocks)
            {
                return ObjectLocks.ContainsKey(storeItem.ItemPath) ? ObjectLocks[storeItem.ItemPath].ToList() : new List<IWebDavStoreItemLockInstance>();
            }
        }
    }
}