using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Stores.Locks;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    /// This class implements the <c>PUT</c> HTTP method for WebDAV#.
    /// </summary>
    internal class WebDavUnlockMethodHandler : WebDavMethodHandlerBase, IWebDavMethodHandler
    {

        #region Properties

        /// <summary>
        /// Gets the collection of the names of the HTTP methods handled by this instance.
        /// </summary>
        /// <value>
        /// The names.
        /// </value>
        public IEnumerable<string> Names
        {
            get
            {
                return new[]
                {
                    "UNLOCK"
                };
            }
        }

        #endregion

        #region Functions
        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="server">The <see cref="WebDavServer" /> through which the request came in from the client.</param>
        /// <param name="context">The 
        /// <see cref="IHttpListenerContext" /> object containing both the request and response
        /// objects to use.</param>
        /// <param name="store">The <see cref="IWebDavStore" /> that the <see cref="WebDavServer" /> is hosting.</param>
        public void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store)
        {
            /***************************************************************************************************
            * Send the response
            ***************************************************************************************************/
            WindowsIdentity Identity = (WindowsIdentity)Thread.GetData(Thread.GetNamedDataSlot(WebDavServer.HttpUser));
            context.SendSimpleResponse(WebDavStoreItemLock.UnLock(context.Request.Url, GetLockTokenHeader(context.Request), Identity.Name));
        }

        #endregion

    }
}