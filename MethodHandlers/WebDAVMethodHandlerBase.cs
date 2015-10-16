using System;
using System.Collections.Generic;
using System.Linq;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;
using static System.String;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    ///     This is the base class for <see cref="IWebDavMethodHandler" /> implementations.
    /// </summary>
    public abstract class WebDavMethodHandlerBase : IWebDavMethodHandler
    {
        #region Variables

        private const int DepthInfinity = -1;

        //public WindowsIdentity UserIdentity;

        #endregion

        /// <summary>
        /// </summary>
        public abstract IEnumerable<string> Names { get; }

        /// <summary>
        /// </summary>
        /// <param name="server"></param>
        /// <param name="context"></param>
        /// <param name="store"></param>
        public abstract void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store);

        #region Static Functions

        /// <summary>
        ///     Get the parent collection from the requested
        ///     <see cref="Uri" />.
        ///     <see cref="WebDavException" /> 409 Conflict possible.
        /// </summary>
        /// <param name="server">The <see cref="WebDavServer" /> through which the request came in from the client.</param>
        /// <param name="store">The <see cref="IWebDavStore" /> that the <see cref="WebDavServer" /> is hosting.</param>
        /// <param name="childUri">The <see cref="Uri" /> object containing the specific location of the child</param>
        /// <returns>
        ///     The parrent collection as an <see cref="IWebDavStoreCollection" />
        /// </returns>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavUnauthorizedException"></exception>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavConflictException">
        /// </exception>
        /// <exception cref="WebDavUnauthorizedException">When the user is unauthorized and doesn't have access</exception>
        /// <exception cref="WebDavConflictException">When the parent collection doesn't exist</exception>
        public static IWebDavStoreCollection GetParentCollection(WebDavServer server, IWebDavStore store, Uri childUri)
        {
            Uri parentCollectionUri = childUri.GetParentUri();
            return parentCollectionUri.GetItem(server, store) as IWebDavStoreCollection;
        }

        /// <summary>
        ///     Get the item in the collection from the requested
        ///     <see cref="Uri" />.
        ///     <see cref="WebDavException" /> 409 Conflict possible.
        /// </summary>
        /// <param name="collection">The parent collection as a <see cref="IWebDavStoreCollection" /></param>
        /// <param name="childUri">The <see cref="Uri" /> object containing the specific location of the child</param>
        /// <returns>
        ///     The <see cref="IWebDavStoreItem" /> from the <see cref="IWebDavStoreCollection" />
        /// </returns>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavUnauthorizedException">
        ///     If user is not authorized to get access to
        ///     the item
        /// </exception>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavNotFoundException">If item not found.</exception>
        public static IWebDavStoreItem GetItemFromCollection(IWebDavStoreCollection collection, Uri childUri)
        {
            IWebDavStoreItem item = collection.GetItemByName(Uri.UnescapeDataString(childUri.Segments.Last().TrimEnd('/', '\\')));
            if (item == null)
                throw new WebDavNotFoundException();
            item.Href = childUri;

            return item;
        }

        /// <summary>
        ///     Gets the Depth header : 0, 1 or infinity
        /// </summary>
        /// <param name="request">The <see cref="IHttpListenerContext" /> with the response included</param>
        /// <returns>
        ///     The values 0, 1 or -1 (for infinity)
        /// </returns>
        public static int GetDepthHeader(IHttpListenerRequest request)
        {
            // get the value of the depth header as a string
            string depth = request.Headers["Depth"];

            // check if the string is valid or not infinity
            // if so, try to parse it to an int
            if (IsNullOrEmpty(depth) || depth.Equals("infinity"))
                return DepthInfinity;
            int value;
            if (!int.TryParse(depth, out value))
                return DepthInfinity;
            if (value == 0 || value == 1)
                return value;
            // else, return the infinity value
            return DepthInfinity;
        }

        /// <summary>
        ///     Gets the Overwrite header : T or F
        /// </summary>
        /// <param name="request">The <see cref="IHttpListenerRequest" /> has the header included</param>
        /// <returns>The <see cref="bool" /> true if overwrite, false if no overwrite</returns>
        public static bool GetOverwriteHeader(IHttpListenerRequest request)
        {
            // get the value of the Overwrite header as a string
            string overwrite = request.Headers["Overwrite"];

            // check if the string is valid and if it equals T
            return overwrite != null && overwrite.Equals("T");
            // else, return false
        }


        /// <summary>
        ///     Gets the Destination header as an URI
        /// </summary>
        /// <param name="request">The <see cref="IHttpListenerRequest" /> has the header included</param>
        /// <returns>The <see cref="Uri" /> containing the destination</returns>
        public static Uri GetDestinationHeader(IHttpListenerRequest request)
        {
            // get the value of the Destination header as a string
            string destinationUri = request.Headers["Destination"];

            // check if the string is valid 
            if (!IsNullOrEmpty(destinationUri))
                return new Uri(destinationUri);
            // else, throw exception
            throw new WebDavConflictException();
        }

        #endregion
    }
}