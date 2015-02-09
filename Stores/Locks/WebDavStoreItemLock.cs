using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;


namespace WebDAVSharp.Server.Stores.Locks
{
    /// <summary>
    /// This class provides the locking functionality.
    /// 
    /// </summary>
    public class WebDavStoreItemLock
    {

        #region Variables
        /// <summary>
        /// Allow Objects to be checked out forever
        /// </summary>
        internal static bool AllowInfiniteCheckouts = false;

        /// <summary>
        /// Max amount of seconds a item can be checkout for.
        /// </summary>
        internal static long MaxCheckOutSeconds = long.MaxValue;

        /// <summary>
        /// Used to store the locks per URI
        /// </summary>
        private static readonly Dictionary<Uri, List<WebDaveStoreItemLockInstance>> ObjectLocks = new Dictionary<Uri, List<WebDaveStoreItemLockInstance>>();

        #endregion

        /// <summary>
        /// This function removes any expired locks for the path.
        /// </summary>
        /// <param name="path"></param>
        private static void CleanLocks(Uri path)
        {
            lock (ObjectLocks)
            {
                if (!ObjectLocks.ContainsKey(path))
                    return;
                foreach (
                    WebDaveStoreItemLockInstance ilock in ObjectLocks[path].ToList()
                        .Where(ilock => ilock.ExpirationDate != null && (DateTime)ilock.ExpirationDate < DateTime.Now)
                    )
                    ObjectLocks[path].Remove(ilock);
            }
        }

        /// <summary>
        /// This function will refresh an existing lock.
        /// </summary>
        /// <param name="path">Target URI to the file or folder </param>
        /// <param name="locktoken">The token issued when the lock was established</param>
        /// <param name="requestedlocktimeout">The requested timeout</param>
        /// <param name="requestDocument">Output parameter, returns the Request document that was used when the lock was established.</param>
        /// <returns></returns>
        public static int RefreshLock(Uri path, string locktoken, ref string requestedlocktimeout,
            out XmlDocument requestDocument)
        {
            CleanLocks(path);
            //Refreshing an existing lock

            //If a lock doesn't exist then lets just reply with a Precondition Failed.
            //412 (Precondition Failed), with 'lock-token-matches-request-uri' precondition code - The LOCK request was 
            //made with an If header, indicating that the client wishes to refresh the given lock. However, the Request-URI 
            //did not fall within the scope of the lock identified by the token. The lock may have a scope that does not 
            //include the Request-URI, or the lock could have disappeared, or the token may be invalid.
            requestDocument = null;
            if (!ObjectLocks.ContainsKey(path))
                return 412;

            string tmptoken = locktoken;
            lock (ObjectLocks)
            {
                WebDaveStoreItemLockInstance ilock = ObjectLocks[path].FirstOrDefault(d => (d.Token == tmptoken));
                if (ilock == null)
                {
                    WebDavServer.Log.Debug("Lock Refresh Failed , Lock does not exist.");
                    return 412;
                }
                WebDavServer.Log.Debug("Lock Refresh Successful.");
                ilock.RefreshLock(ref requestedlocktimeout);
                requestDocument = ilock.RequestDocument;

                return (int)HttpStatusCode.OK;
            }
        }

        /// <summary>
        /// Locks the request Path.
        /// </summary>
        /// <param name="path">URI to the item to be locked</param>
        /// <param name="lockscope">The lock Scope used for locking</param>
        /// <param name="locktype">The lock Type used for locking</param>
        /// <param name="lockowner">The owner of the lock</param>
        /// <param name="requestedlocktimeout">The requested timeout</param>
        /// <param name="locktoken">Out parameter, returns the issued token</param>
        /// <param name="requestDocument">the Request Document</param>
        /// <param name="depth">How deep to lock, 0,1, or infinity</param>
        /// <returns></returns>
        public static int Lock(Uri path, WebDavLockScope lockscope, WebDavLockType locktype, string lockowner,
            ref string requestedlocktimeout, out string locktoken, XmlDocument requestDocument, int depth)
        {
            CleanLocks(path);
            WebDavServer.Log.Info("Lock Requested Timeout:" + requestedlocktimeout);
            locktoken = string.Empty;
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
                if (!ObjectLocks.ContainsKey(path))
                {
                    ObjectLocks.Add(path, new List<WebDaveStoreItemLockInstance>());

                    ObjectLocks[path].Add(new WebDaveStoreItemLockInstance(path, lockscope, locktype, lockowner,
                        ref requestedlocktimeout, ref locktoken,
                        requestDocument, depth));

                    WebDavServer.Log.Debug("Created New Lock (" + lockscope + "), URI had no locks.  Timeout:" +
                                           requestedlocktimeout);

                    return (int)HttpStatusCode.OK;
                }

                //The fact that ObjectLocks contains this URI means that there is already a lock on this object,
                //This means the lock fails because you can only have 1 exclusive lock.
                if (lockscope == WebDavLockScope.Exclusive)
                {
                    WebDavServer.Log.Debug("Lock Creation Failed (Exclusive), URI already has a lock.");
                    return 423;
                }

                //If the scope is shared and all other locks on this uri are shared we are ok, otherwise we fail.
                if (lockscope == WebDavLockScope.Shared)
                    if (ObjectLocks[path].Any(itemLock => itemLock.LockScope == WebDavLockScope.Exclusive))
                    {
                        WebDavServer.Log.Debug("Lock Creation Failed (Shared), URI has exclusive lock.");
                        return 423;
                    }
                //423 (Locked), potentially with 'no-conflicting-lock' precondition code - 
                //There is already a lock on the resource that is not compatible with the 
                //requested lock (see lock compatibility table above).

                //If it gets to here, then we are most likely creating another shared lock on the file.

                #region Create New Lock

                ObjectLocks[path].Add(new WebDaveStoreItemLockInstance(path, lockscope, locktype, lockowner,
                    ref requestedlocktimeout, ref locktoken,
                    requestDocument, depth));

                WebDavServer.Log.Debug("Created New Lock (" + lockscope + "), URI had no locks.  Timeout:" +
                                       requestedlocktimeout);

                #endregion

                return (int)HttpStatusCode.OK;
            }
        }

        /// <summary>
        /// Unlocks the URI passed if the token matches a lock token in use.
        /// </summary>
        /// <param name="path">URI to resource</param>
        /// <param name="locktoken">Token used to lock.</param>
        /// <param name="owner">Owner.</param>
        /// <returns></returns>
        public static int UnLock(Uri path, string locktoken, string owner)
        {
            CleanLocks(path);
            if (string.IsNullOrEmpty(locktoken))
            {
                WebDavServer.Log.Debug("Unlock failed, No Token!.");
                return (int)HttpStatusCode.BadRequest;
            }

            lock (ObjectLocks)
            {
                if (!ObjectLocks.ContainsKey(path))
                {
                    WebDavServer.Log.Debug("Unlock failed, Lock does not exist!.");
                    return (int)HttpStatusCode.Conflict;
                }

                WebDaveStoreItemLockInstance ilock = ObjectLocks[path].FirstOrDefault(d => d.Token == locktoken && d.Owner == owner);
                if (ilock == null)
                    return (int)HttpStatusCode.Conflict;
                //Remove the lock
                ObjectLocks[path].Remove(ilock);

                //if there are no locks left of the uri then remove it from ObjectLocks.
                if (ObjectLocks[path].Count == 0)
                    ObjectLocks.Remove(path);

                WebDavServer.Log.Debug("Unlock successful.");
                return (int)HttpStatusCode.NoContent;
            }
        }

        /// <summary>
        /// Returns all the locks on the path
        /// </summary>
        /// <param name="path">URI to resource</param>
        /// <returns></returns>
        public static List<WebDaveStoreItemLockInstance> GetLocks(Uri path)
        {
            return ObjectLocks.ContainsKey(path) ? ObjectLocks[path].ToList() : new List<WebDaveStoreItemLockInstance>();
        }
    }
}