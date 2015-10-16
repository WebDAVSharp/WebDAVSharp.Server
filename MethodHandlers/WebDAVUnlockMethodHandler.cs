using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    ///     This class implements the <c>PUT</c> HTTP method for WebDAV#.
    /// </summary>
    internal class WebDavUnlockMethodHandler : WebDavMethodHandlerBase
    {
        #region Properties

        /// <summary>
        ///     Gets the collection of the names of the HTTP methods handled by this instance.
        /// </summary>
        /// <value>
        ///     The names.
        /// </value>
        public override IEnumerable<string> Names => new[]
        {
            "UNLOCK"
        };

        #endregion

        #region Functions

        /// <summary>
        ///     Processes the request.
        /// </summary>
        /// <param name="server">The <see cref="WebDavServer" /> through which the request came in from the client.</param>
        /// <param name="context">
        ///     The
        ///     <see cref="IHttpListenerContext" /> object containing both the request and response
        ///     objects to use.
        /// </param>
        /// <param name="store">The <see cref="IWebDavStore" /> that the <see cref="WebDavServer" /> is hosting.</param>
        public override void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store)
        {
            /***************************************************************************************************
            * Send the response
            ***************************************************************************************************/
            IWebDavStoreCollection collection = GetParentCollection(server, store, context.Request.Url);
            // Get the item from the collection
            IWebDavStoreItem storeItem = GetItemFromCollection(collection, context.Request.Url);
            if (storeItem == null)
                throw new WebDavNotFoundException(context.Request.Url.ToString());
            var userIdentity = (WindowsIdentity) Thread.GetData(Thread.GetNamedDataSlot(WebDavServer.HttpUser));
            context.SendSimpleResponse(store.LockSystem.UnLock(storeItem, context.Request.GetLockTokenHeader(), userIdentity.Name));
        }

        #endregion
    }
}