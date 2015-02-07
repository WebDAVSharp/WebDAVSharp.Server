using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    /// This class implements the <c>PUT</c> HTTP method for WebDAV#.
    /// </summary>
    internal class WebDavPutMethodHandler : WebDavMethodHandlerBase, IWebDavMethodHandler
    {
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
                    "PUT"
                };
            }
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="server">The <see cref="WebDavServer" /> through which the request came in from the client.</param>
        /// <param name="context">The 
        /// <see cref="IHttpListenerContext" /> object containing both the request and response
        /// objects to use.</param>
        /// <param name="store">The <see cref="IWebDavStore" /> that the <see cref="WebDavServer" /> is hosting.</param>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavMethodNotAllowedException"></exception>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavLengthRequiredException">If the ContentLength header was not found</exception>
        public void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store)
        {
            // Get the parent collection
            IWebDavStoreCollection parentCollection = GetParentCollection(server, store, context.Request.Url);

            // Gets the item name from the url
            string itemName = Uri.UnescapeDataString(context.Request.Url.Segments.Last().TrimEnd('/', '\\'));

            IWebDavStoreItem item = parentCollection.GetItemByName(itemName);
            IWebDavStoreDocument doc;
            if (item != null)
            {
                doc = item as IWebDavStoreDocument;
                if (doc == null)
                    throw new WebDavMethodNotAllowedException();
            }
            else
            {
                doc = parentCollection.CreateDocument(itemName);
            }

            if (context.Request.ContentLength64 < 0)
                throw new WebDavLengthRequiredException();

            using (Stream stream = doc.OpenWriteStream(false))
            {
                long left = context.Request.ContentLength64;
                byte[] buffer = new byte[4096];
                while (left > 0)
                {
                    int toRead = Convert.ToInt32(Math.Min(left, buffer.Length));
                    int inBuffer = context.Request.InputStream.Read(buffer, 0, toRead);
                    stream.Write(buffer, 0, inBuffer);

                    left -= inBuffer;
                }
            }

            context.SendSimpleResponse(HttpStatusCode.Created);
        }
    }
}