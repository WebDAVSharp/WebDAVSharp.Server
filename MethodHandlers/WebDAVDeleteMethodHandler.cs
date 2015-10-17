using System.Collections.Generic;
using System.Text;
using System.Web;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Utilities;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    ///     This class implements the <c>DELETE</c> HTTP method for WebDAV#.
    /// </summary>
    internal class WebDavDeleteMethodHandler : WebDavMethodHandlerBase
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
            "DELETE"
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
            IWebDavStoreCollection collection;
            // Get the parent collection of the item
            collection = GetParentCollection(server, store, context.Request.Url);

            // Get the item from the collection
            IWebDavStoreItem item = GetItemFromCollection(collection, context.Request.Url);

            if (store.LockSystem.GetLocks(item).Count > 0)
            {
                StringBuilder sb = new StringBuilder(3000);
                sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?><D:multistatus xmlns:Z=\"urn:schemas-microsoft-com:\" xmlns:D=\"DAV:\"><D:response><D:href>" + context.Request.Url + "</D:href><d:status>HTTP/1.1 423 Locked</d:status><d:error><d:lock-token-submitted/></d:error></d:response> </d:multistatus> ");
                byte[] responseBytes = Encoding.UTF8.GetBytes(sb.ToString());
                context.Response.StatusCode = (int) WebDavStatusCode.MultiStatus;
                context.Response.StatusDescription = HttpWorkerRequest.GetStatusDescription((int) WebDavStatusCode.MultiStatus);

                // set the headers of the response
                context.Response.ContentLength64 = responseBytes.Length;
                context.Response.AdaptedInstance.ContentType = "text/xml";

                // the body
                context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                context.Response.Close();
            }

            // Deletes the item
            collection.Delete(item);

            context.SendSimpleResponse();
        }

        #endregion
    }
}