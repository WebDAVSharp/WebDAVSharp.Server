using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;
using static System.String;

namespace WebDAVSharp.Server
{
    /// <summary>
    ///     This class holds extension methods for various types related to WebDAV#.
    /// </summary>
    internal static class WebDavExtensions
    {
        private static readonly Regex TokenRegex = new Regex(@"<urn:uuid:(?<Token>.*)>");

        /// <summary>
        ///     Gets the Uri to the parent object.
        /// </summary>
        /// <param name="uri">The <see cref="Uri" /> of a resource, for which the parent Uri should be retrieved.</param>
        /// <returns>
        ///     The parent <see cref="Uri" />.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">uri</exception>
        /// <exception cref="System.InvalidOperationException">Cannot get parent of root</exception>
        /// <exception cref="ArgumentNullException"><paramref name="uri" /> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="uri" /> has no parent, it refers to a root resource.</exception>
        public static Uri GetParentUri(this Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));
            if (uri.Segments.Length == 1)
                throw new InvalidOperationException("Cannot get parent of root");

            string url = uri.ToString();
            int index = url.Length - 1;
            if (url[index] == '/')
                index--;
            while (url[index] != '/')
                index--;
            return new Uri(url.Substring(0, index + 1));
        }

        /// <summary>
        ///     Sends a simple response with a specified HTTP status code but no content.
        /// </summary>
        /// <param name="context">The <see cref="IHttpListenerContext" /> to send the response through.</param>
        /// <param name="statusCode">The HTTP status code for the response.</param>
        /// <exception cref="System.ArgumentNullException">context</exception>
        /// <exception cref="ArgumentNullException"><paramref name="context" /> is <c>null</c>.</exception>
        public static void SendSimpleResponse(this IHttpListenerContext context, int statusCode = (int) HttpStatusCode.OK)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.Response.StatusCode = statusCode;
            context.Response.StatusDescription = HttpWorkerRequest.GetStatusDescription(statusCode);
            context.Response.Close();
        }

        /// <summary>
        ///     Gets the prefix <see cref="Uri" /> that matches the specified <see cref="Uri" />.
        /// </summary>
        /// <param name="uri">The <see cref="Uri" /> to find the most specific prefix <see cref="Uri" /> for.</param>
        /// <param name="server">
        ///     The
        ///     <see cref="WebDavServer" /> that hosts the WebDAV server and holds the collection
        ///     of known prefixes.
        /// </param>
        /// <returns>
        ///     The most specific <see cref="Uri" /> for the given <paramref name="uri" />.
        /// </returns>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavInternalServerException">Unable to find correct server root</exception>
        /// <exception cref="WebDavInternalServerException">
        ///     <paramref name="uri" /> specifies a <see cref="Uri" /> that is not
        ///     known to the <paramref name="server" />.
        /// </exception>
        public static Uri GetPrefixUri(this Uri uri, WebDavServer server)
        {
            string url = uri.ToString();

            string exactPrefix = server.Listener.Prefixes
                .FirstOrDefault(item => url.StartsWith(item, StringComparison.OrdinalIgnoreCase));

            if (!IsNullOrEmpty(exactPrefix))
            {
                return new Uri(exactPrefix);
            }

            string wildcardUrl = new UriBuilder(uri)
            {
                Host = "WebDAVSharpSpecialHostTag"
            }
                .ToString().Replace("WebDAVSharpSpecialHostTag", "*");

            string wildcardPrefix = server.Listener.Prefixes
                .FirstOrDefault(item => wildcardUrl.StartsWith(item, StringComparison.OrdinalIgnoreCase));

            if (!IsNullOrEmpty(wildcardPrefix))
            {
                return new Uri(wildcardPrefix.Replace("://*", $"://{uri.Host}"));
            }

            throw new WebDavInternalServerException("Unable to find correct server root");
        }

        /// <summary>
        ///     Retrieves a store item through the specified
        ///     <see cref="Uri" /> from the
        ///     specified
        ///     <see cref="WebDavServer" /> and
        ///     <see cref="IWebDavStore" />.
        /// </summary>
        /// <param name="uri">The <see cref="Uri" /> to retrieve the store item for.</param>
        /// <param name="server">The <see cref="WebDavServer" /> that hosts the <paramref name="store" />.</param>
        /// <param name="store">The <see cref="IWebDavStore" /> from which to retrieve the store item.</param>
        /// <returns>
        ///     The retrieved store item.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     <para>
        ///         <paramref name="uri" /> is <c>null</c>.
        ///     </para>
        ///     <para>
        ///         <paramref name="server" /> is <c>null</c>.
        ///     </para>
        ///     <para>
        ///         <paramref name="store" /> is <c>null</c>.
        ///     </para>
        /// </exception>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavNotFoundException">If the item was not found.</exception>
        /// <exception cref="WebDavConflictException">
        ///     <paramref name="uri" /> refers to a document in a collection, where the
        ///     collection does not exist.
        /// </exception>
        /// <exception cref="WebDavNotFoundException"><paramref name="uri" /> refers to a document that does not exist.</exception>
        public static IWebDavStoreItem GetItem(this Uri uri, WebDavServer server, IWebDavStore store)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));
            if (server == null)
                throw new ArgumentNullException(nameof(server));
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            Uri prefixUri = uri.GetPrefixUri(server);
            IWebDavStoreCollection collection = store.Root;

            IWebDavStoreItem item = null;
            if (prefixUri.Segments.Length == uri.Segments.Length)
                return collection;

            for (int index = prefixUri.Segments.Length; index < uri.Segments.Length; index++)
            {
                string segmentName = Uri.UnescapeDataString(uri.Segments[index]);
                IWebDavStoreItem nextItem = collection.GetItemByName(segmentName.TrimEnd('/', '\\'));
                if (nextItem == null)
                    throw new WebDavNotFoundException(); //throw new WebDavConflictException();

                if (index == uri.Segments.Length - 1)
                    item = nextItem;
                else
                {
                    collection = nextItem as IWebDavStoreCollection;
                    if (collection == null)
                        throw new WebDavNotFoundException();
                }
            }

            if (item == null)
                throw new WebDavNotFoundException();

            return item;
        }

        private static string Match(Regex regex, string html, int i = 1)
        {
            return regex.Match(html).Groups[i].Value.Trim();
        }

        public static Guid? GetLockTokenHeader(this IHttpListenerRequest request)
        {
            if (!request.Headers.AllKeys.Contains("Lock-Token"))
                return null;
            string r = Match(TokenRegex, request.Headers["Lock-Token"]);
            if (IsNullOrEmpty(r))
                return null;
            return new Guid(r);
        }

        public static Guid? GetLockTokenIfHeader(this IHttpListenerRequest request)
        {
            if (!request.Headers.AllKeys.Contains("If"))
                return null;
            string t = request.Headers["If"].Substring(2, request.Headers["If"].Length - 4);
            return new Guid(t);
        }

        public static string ToLockToken(this Guid? token)
        {
            if (token == null)
                throw new NullReferenceException("token");
            return "urn:uuid:" + token;
        }

        /// <summary>
        ///     Gets the Timeout header : Second-number
        /// </summary>
        /// <param name="request">The request with the request included</param>
        /// <param name="store"></param>
        /// <returns>The value of the Timeout header as a string</returns>
        public static double? GetTimeoutHeader(this IHttpListenerRequest request, IWebDavStore store)
        {
            // get the value of the timeout header as a string
            string timeout = request.Headers["Timeout"];

            // check if the string is valid or not infinity
            // if so, try to parse it to an int
            if (!IsNullOrEmpty(timeout) && !timeout.Equals("infinity") && !timeout.Equals("Infinite, Second-4100000000"))
            {
                string num = timeout.Substring(timeout.IndexOf("Second-", StringComparison.Ordinal) + 7);
                double d;
                if (double.TryParse(num, out d))
                {
                    return d;
                }
            }
            if (timeout.Equals("infinity") && timeout.Equals("Infinite, Second-4100000000") && store.LockSystem.AllowInfiniteCheckouts)
                return null;

            return 345600;
        }

        public static void SendException(this IHttpListenerContext context, WebDavException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.StatusDescription = ex.StatusDescription;

            if (ex.Message == context.Response.StatusDescription)
                return;

            byte[] buffer = Encoding.UTF8.GetBytes(ex.Message);
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Flush();
        }
    }
}