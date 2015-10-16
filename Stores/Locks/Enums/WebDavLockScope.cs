namespace WebDAVSharp.Server.Stores.Locks.Enums
{
    /// <summary>
    ///     Possible scopes for locks.
    /// </summary>
    public enum WebDavLockScope
    {
        /// <summary>
        ///     Can only have one exclusive Lock
        /// </summary>
        Exclusive,

        /// <summary>
        ///     Can be many.
        /// </summary>
        Shared
    }
}