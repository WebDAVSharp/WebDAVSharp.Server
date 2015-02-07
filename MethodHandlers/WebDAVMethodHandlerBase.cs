using System;
using System.Linq;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    /// This is the base class for <see cref="IWebDavMethodHandler" /> implementations.
    /// </summary>
    internal abstract class WebDavMethodHandlerBase
        {
        private const int DepthInfinity = -1;

        /// <summary>
        /// Get the parent collection from the requested
        /// <see cref="Uri" />.
        /// <see cref="WebDavException" /> 409 Conflict possible.
        /// </summary>
        /// <param name="server">The <see cref="WebDavServer" /> through which the request came in from the client.</param>
        /// <param name="store">The <see cref="IWebDavStore" /> that the <see cref="WebDavServer" /> is hosting.</param>
        /// <param name="childUri">The <see cref="Uri" /> object containing the specific location of the child</param>
        /// <returns>
        /// The parrent collection as an <see cref="IWebDavStoreCollection" />
        /// </returns>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavUnauthorizedException"></exception>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavConflictException">
        /// </exception>
        /// <exception cref="WebDavUnauthorizedException">When the user is unauthorized and doesn't have access</exception>
        /// <exception cref="WebDavConflictException">When the parent collection doesn't exist</exception>
        public static IWebDavStoreCollection GetParentCollection(WebDavServer server, IWebDavStore store, Uri childUri)
        {
            Uri parentCollectionUri = childUri.GetParentUri();
            IWebDavStoreCollection collection;
            try
            {
                collection = parentCollectionUri.GetItem(server, store) as IWebDavStoreCollection;
            }
            catch (UnauthorizedAccessException)
            {
                throw  new WebDavUnauthorizedException();
            }
            catch(WebDavNotFoundException)
            {
                throw new WebDavConflictException();
            }
            if (collection == null)
                throw new WebDavConflictException();

            return collection;
        }

        /// <summary>
        /// Get the item in the collection from the requested
        /// <see cref="Uri" />.
        /// <see cref="WebDavException" /> 409 Conflict possible.
        /// </summary>
        /// <param name="collection">The parent collection as a <see cref="IWebDavStoreCollection" /></param>
        /// <param name="childUri">The <see cref="Uri" /> object containing the specific location of the child</param>
        /// <returns>
        /// The <see cref="IWebDavStoreItem" /> from the <see cref="IWebDavStoreCollection" />
        /// </returns>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavUnauthorizedException">If user is not authorized to get access to the item</exception>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavNotFoundException">If item not found.</exception>
        public static IWebDavStoreItem GetItemFromCollection(IWebDavStoreCollection collection, Uri childUri)
        {
            IWebDavStoreItem item;
            try
            {
                item = collection.GetItemByName(Uri.UnescapeDataString(childUri.Segments.Last().TrimEnd('/', '\\')));
            }
            catch (UnauthorizedAccessException)
            {
                throw new WebDavUnauthorizedException();
            }
            catch (WebDavNotFoundException)
            {
                throw new WebDavNotFoundException();
            }
            if (item == null)
                throw new WebDavNotFoundException();

            return item;
        }

        /// <summary>
        /// Gets the Depth header : 0, 1 or infinity
        /// </summary>
        /// <param name="request">The <see cref="IHttpListenerContext" /> with the response included</param>
        /// <returns>
        /// The values 0, 1 or -1 (for infinity)
        /// </returns>
        public static int GetDepthHeader(IHttpListenerRequest request)
        {
            // get the value of the depth header as a string
            string depth = request.Headers["Depth"];

            // check if the string is valid or not infinity
            // if so, try to parse it to an int
            if (String.IsNullOrEmpty(depth) || depth.Equals("infinity"))
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
        /// Gets the Overwrite header : T or F
        /// </summary>
        /// <param name="request">The <see cref="IHttpListenerRequest"/> has the header included</param>
        /// <returns>The <see cref="bool"/> true if overwrite, false if no overwrite</returns>
        public static bool GetOverwriteHeader(IHttpListenerRequest request)
        {
            // get the value of the Overwrite header as a string
            string overwrite = request.Headers["Overwrite"];

            // check if the string is valid and if it equals T
            return overwrite != null && overwrite.Equals("T");
            // else, return false
        }

        /// <summary>
        /// Gets the Timeout header : Second-number
        /// </summary>
        /// <param name="request">The request with the request included</param>
        /// <returns>The value of the Timeout header as a string</returns>
        public static string GetTimeoutHeader(IHttpListenerRequest request)
        {
            // get the value of the timeout header as a string
            string timeout = request.Headers["Timeout"];

            // check if the string is valid or not infinity
            // if so, try to parse it to an int
            if (!String.IsNullOrEmpty(timeout) && !timeout.Equals("infinity") &&
                !timeout.Equals("Infinite, Second-4100000000"))
                return timeout;
            // else, return the timeout value as if it was requested to be 4 days
            return "Second-345600";
        }

        /// <summary>
        /// Gets the Destination header as an URI
        /// </summary>
        /// <param name="request">The <see cref="IHttpListenerRequest"/> has the header included</param>
        /// <returns>The <see cref="Uri"/> containing the destination</returns>
        public static Uri GetDestinationHeader(IHttpListenerRequest request)
        {
            // get the value of the Destination header as a string
            string destinationUri = request.Headers["Destination"];

            // check if the string is valid 
            if (!String.IsNullOrEmpty(destinationUri))
                return new Uri(destinationUri);
            // else, throw exception
            throw new WebDavConflictException();
        }
    }
}
