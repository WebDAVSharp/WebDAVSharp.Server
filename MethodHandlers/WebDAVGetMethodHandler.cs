using System.Collections.Generic;
using System.IO;
using System.Net;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    ///     This class implements the <c>GET</c> HTTP method for WebDAV#.
    /// </summary>
    internal sealed class WebDavGetMethodHandler : WebDavMethodHandlerBase
    {
        #region Properties

        /// <summary>
        ///     Gets the collection of the names of the verbs handled by this instance.
        /// </summary>
        /// <value>
        ///     The names.
        /// </value>
        public override IEnumerable<string> Names => new[]
        {
            "GET"
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
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavNotFoundException"></exception>
        /// <exception cref="WebDavNotFoundException">
        ///     <para>
        ///         <paramref name="context" /> specifies a request for a store item that does not exist.
        ///     </para>
        ///     <para>- or -</para>
        ///     <para>
        ///         <paramref name="context" /> specifies a request for a store item that is not a document.
        ///     </para>
        /// </exception>
        /// <exception cref="WebDavConflictException">
        ///     <paramref name="context" /> specifies a request for a store item using a
        ///     collection path that does not exist.
        /// </exception>
        public override void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store)
        {
            IWebDavStoreCollection collection = GetParentCollection(server, store, context.Request.Url);
            IWebDavStoreItem item = GetItemFromCollection(collection, context.Request.Url);

            IWebDavStoreDocument doc = item as IWebDavStoreDocument;

            if (doc == null)
                throw new WebDavNotFoundException();

            long docSize = doc.Size;

            if (docSize == 0)
            {
                context.Response.StatusCode = (int) HttpStatusCode.OK;
                context.Response.ContentLength64 = 0;
            }


            using (Stream stream = doc.OpenReadStream())
            {
                if (stream == null)
                {
                    context.Response.StatusCode = (int) HttpStatusCode.OK;
                    context.Response.ContentLength64 = 0;
                }
                else
                {
                    context.Response.StatusCode = (int) HttpStatusCode.OK;

                    if (docSize > 0)
                        context.Response.ContentLength64 = docSize;


                    byte[] buffer = new byte[docSize + 1];
                    int inBuffer;

                    while ((inBuffer = stream.Read(buffer, 0, buffer.Length)) > 0)
                        context.Response.OutputStream.Write(buffer, 0, inBuffer);

                    context.Response.OutputStream.Flush();
                }
            }

            context.Response.Close();
        }

        #endregion
    }
}