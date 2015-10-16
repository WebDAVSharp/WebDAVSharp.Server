using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Stores.Locks.Interfaces;
using WebDAVSharp.Server.Utilities;
using static System.String;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    ///     This class implements the <c>PROPFIND</c> HTTP method for WebDAV#.
    /// </summary>
    internal class WebDavPropfindMethodHandler : WebDavMethodHandlerBase
    {
        #region Variables

        private static readonly List<WebDavProperty> List = new List<WebDavProperty>
        {
            new WebDavProperty("displayname", WebDavProperty.StoreItemTypeFlag.Any),
            new WebDavProperty("lockdiscovery", WebDavProperty.StoreItemTypeFlag.Any),
            new WebDavProperty("supportedlock", WebDavProperty.StoreItemTypeFlag.Any),
            new WebDavProperty("isfolder", WebDavProperty.StoreItemTypeFlag.Collection), //New
            new WebDavProperty("iscollection", WebDavProperty.StoreItemTypeFlag.Collection), //New
            new WebDavProperty("ishidden", WebDavProperty.StoreItemTypeFlag.Collection),
            new WebDavProperty("getcontenttype", WebDavProperty.StoreItemTypeFlag.Collection),
            new WebDavProperty("getcontentlength", WebDavProperty.StoreItemTypeFlag.Any),
            new WebDavProperty("resourcetype", WebDavProperty.StoreItemTypeFlag.Collection),
            new WebDavProperty("authoritative-directory", Empty, "http://schemas.microsoft.com/repl/", "Repl", WebDavProperty.StoreItemTypeFlag.Collection),
            new WebDavProperty("getlastmodified", WebDavProperty.StoreItemTypeFlag.Any),
            new WebDavProperty("creationdate", WebDavProperty.StoreItemTypeFlag.Any),
            new WebDavProperty("repl-uid", Empty, "http://schemas.microsoft.com/repl/", "Repl", WebDavProperty.StoreItemTypeFlag.Any),
            new WebDavProperty("resourcetag", Empty, "http://schemas.microsoft.com/repl/", "Repl", WebDavProperty.StoreItemTypeFlag.Any),
            new WebDavProperty("getetag", WebDavProperty.StoreItemTypeFlag.Any),
            new WebDavProperty("win32fileattributes", WebDavProperty.StoreItemTypeFlag.Any)
        };

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the collection of the names of the HTTP methods handled by this instance.
        /// </summary>
        /// <value>
        ///     The names.
        /// </value>
        public override IEnumerable<string> Names => new[]
        {
            "PROPFIND"
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
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavUnauthorizedException"></exception>
        public override void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store)
        {
            /***************************************************************************************************
             * Retreive all the information from the request
             ***************************************************************************************************/

            // Read the headers, ...
            bool isPropname = false;
            int depth = GetDepthHeader(context.Request);
            Uri requestUri = GetRequestUri(context.Request.Url.ToString());


            IWebDavStoreItem item = context.Request.Url.GetItem(server, store);

            List<IWebDavStoreItem> webDavStoreItems = GetWebDavStoreItems(item, depth);

            // Get the XmlDocument from the request
            XmlDocument requestDoc = GetXmlDocument(context.Request);

            // See what is requested
            List<WebDavProperty> requestedProperties = new List<WebDavProperty>();
            if (requestDoc.DocumentElement != null)
            {
                if (requestDoc.DocumentElement.LocalName != "propfind")
                {
#if DEBUG
                    WebDavServer.Log.Debug("PROPFIND method without propfind in xml document");
#endif
                }
                else
                {
                    XmlNode n = requestDoc.DocumentElement.FirstChild;
                    if (n == null)
                    {
#if DEBUG
                        WebDavServer.Log.Debug("propfind element without children");
#endif
                    }
                    else
                    {
                        switch (n.LocalName)
                        {
                            case "allprop":
                                requestedProperties = GetAllProperties();
                                break;
                            case "propname":
                                isPropname = true;
                                requestedProperties = GetAllProperties();
                                break;
                            case "prop":
                                requestedProperties.AddRange(from XmlNode child in n.ChildNodes
                                    select new WebDavProperty(child.LocalName, "", child.NamespaceURI));
                                break;
                            default:
                                requestedProperties.Add(new WebDavProperty(n.LocalName, "", n.NamespaceURI));
                                break;
                        }
                    }
                }
            }
            else
                requestedProperties = GetAllProperties();

            /***************************************************************************************************
             * Create the body for the response
             * Send the response
             ***************************************************************************************************/

            string sdoc = ResponseDocument(context, isPropname, requestUri, requestedProperties, webDavStoreItems, store.LockSystem, store);
            SendResponse(context, sdoc);
        }

        #region RetrieveInformation

        /// <summary>
        ///     Get the URI to the location
        ///     If no slash at the end of the URI, this method adds one
        /// </summary>
        /// <param name="uri">The <see cref="string" /> that contains the URI</param>
        /// <returns>
        ///     The <see cref="Uri" /> that contains the given uri
        /// </returns>
        private static Uri GetRequestUri(string uri)
        {
            return new Uri(uri.EndsWith("/") ? uri : uri + "/");
        }

        /// <summary>
        ///     Convert the given
        ///     <see cref="IWebDavStoreItem" /> to a
        ///     <see cref="List{T}" /> of
        ///     <see cref="IWebDavStoreItem" />
        ///     This list depends on the "Depth" header
        /// </summary>
        /// <param name="iWebDavStoreItem">The <see cref="IWebDavStoreItem" /> that needs to be converted</param>
        /// <param name="depth">The "Depth" header</param>
        /// <returns>
        ///     A <see cref="List{T}" /> of <see cref="IWebDavStoreItem" />
        /// </returns>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavConflictException"></exception>
        private static List<IWebDavStoreItem> GetWebDavStoreItems(IWebDavStoreItem iWebDavStoreItem, int depth)
        {
            List<IWebDavStoreItem> list = new List<IWebDavStoreItem>();

            // if the item is a collection
            IWebDavStoreCollection collection = iWebDavStoreItem as IWebDavStoreCollection;
            if (collection != null)
            {
                list.Add(collection);
                if (depth == 0)
                    return list;

                foreach (IWebDavStoreItem item in collection.Items.Where(item => !list.Contains(item)))
                    list.Add(item);

                return list;
            }
            // if the item is not a document, throw conflict exception
            if (!(iWebDavStoreItem is IWebDavStoreDocument))
                throw new WebDavConflictException();

            // add the item to the list
            list.Add(iWebDavStoreItem);

            return list;
        }

        /// <summary>
        ///     Reads the XML body of the
        ///     <see cref="IHttpListenerRequest" />
        ///     and converts it to an
        ///     <see cref="XmlDocument" />
        /// </summary>
        /// <param name="request">The <see cref="IHttpListenerRequest" /></param>
        /// <returns>
        ///     The <see cref="XmlDocument" /> that contains the request body
        /// </returns>
        private static XmlDocument GetXmlDocument(IHttpListenerRequest request)
        {
            try
            {
                string requestBody;
                using (StreamReader reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    requestBody = reader.ReadToEnd();
                    reader.Close();
                }

                if (!IsNullOrEmpty(requestBody))
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(requestBody);
                    return xmlDocument;
                }
            }
            catch (Exception)
            {
#if DEBUG
                WebDavServer.Log.Warn("XmlDocument has not been read correctly");
#endif
                return new XmlDocument();
            }

            return new XmlDocument();
        }

        /// <summary>
        ///     Adds the standard properties for an Propfind allprop request to a <see cref="List{T}" /> of
        ///     <see cref="WebDavProperty" />
        /// </summary>
        /// <returns>
        ///     The list with all the <see cref="WebDavProperty" />
        /// </returns>
        private static List<WebDavProperty> GetAllProperties()
        {
            return List;
        }

        #endregion

        #region BuildResponseBody

        internal string CreateFragment(bool isRoot, XmlDocument responseDoc, IWebDavStoreItem webDavStoreItem, IHttpListenerContext context, bool propname, Uri requestUri, List<WebDavProperty> requestedProperties, IWebDavStoreItemLock lockSystem)
        {
            // Create the response element
            WebDavProperty responseProperty = new WebDavProperty("response", Empty);
            XmlElement responseElement = responseProperty.ToXmlElement(responseDoc);

            // The href element
            Uri result;
            if (isRoot)
                Uri.TryCreate(requestUri, Empty, out result);
            else
                Uri.TryCreate(requestUri, webDavStoreItem.Name, out result);

            WebDavProperty hrefProperty = new WebDavProperty("href", result.AbsoluteUri);
            responseElement.AppendChild(hrefProperty.ToXmlElement(responseDoc));

            // The propstat element
            WebDavProperty propstatProperty = new WebDavProperty("propstat", Empty);
            XmlElement propstatElement = propstatProperty.ToXmlElement(responseDoc);

            // The prop element
            WebDavProperty propProperty = new WebDavProperty("prop", Empty);
            XmlElement propElement = propProperty.ToXmlElement(responseDoc);


            //All properties but lockdiscovery and supportedlock can be handled here.
            foreach (WebDavProperty davProperty in requestedProperties)
            {
                XmlNode toAdd;
                switch (davProperty.Name)
                {
                    case "lockdiscovery":
                        toAdd = LockDiscovery(webDavStoreItem, ref responseDoc, lockSystem);
                        break;
                    case "supportedlock":
                        toAdd = SupportedLocks(ref responseDoc);
                        break;
                    default:
                        toAdd = PropChildElement(davProperty, responseDoc, webDavStoreItem, propname);
                        break;
                }

                if (toAdd != null)
                    propElement.AppendChild(toAdd);
            }


            // Add the prop element to the propstat element
            propstatElement.AppendChild(propElement);

            // The status element
            WebDavProperty statusProperty = new WebDavProperty("status", "HTTP/1.1 " + context.Response.StatusCode + " " + HttpWorkerRequest.GetStatusDescription(context.Response.StatusCode));
            propstatElement.AppendChild(statusProperty.ToXmlElement(responseDoc));

            // Add the propstat element to the response element
            responseElement.AppendChild(propstatElement);

            if (responseDoc.DocumentElement == null)
                throw new Exception("Not Possible.");

            return responseElement.OuterXml.Replace("xmlns:D=\"DAV:\"", "");
        }


        /// <summary>
        ///     Builds the <see cref="XmlDocument" /> containing the response body
        /// </summary>
        /// <param name="context">The <see cref="IHttpListenerContext" /></param>
        /// <param name="propname">The boolean defining the Propfind propname request</param>
        /// <param name="requestUri"></param>
        /// <param name="requestedProperties"></param>
        /// <param name="webDavStoreItems"></param>
        /// <param name="lockSystem"></param>
        /// <param name="store"></param>
        /// <returns>
        ///     The <see cref="XmlDocument" /> containing the response body
        /// </returns>
        private string ResponseDocument(IHttpListenerContext context, bool propname, Uri requestUri, List<WebDavProperty> requestedProperties, List<IWebDavStoreItem> webDavStoreItems, IWebDavStoreItemLock lockSystem, IWebDavStore store)
        {
            // Create the basic response XmlDocument
            XmlDocument responseDoc = new XmlDocument();
            const string responseXml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><D:multistatus xmlns:D=\"DAV:\" xmlns:Office=\"urn:schemas-microsoft-com:office:office\" xmlns:Repl=\"http://schemas.microsoft.com/repl/\" xmlns:Z=\"urn:schemas-microsoft-com:\"></D:multistatus>";
            responseDoc.LoadXml(responseXml);
            // Generate the manager
            XmlNamespaceManager manager = new XmlNamespaceManager(responseDoc.NameTable);
            manager.AddNamespace("D", "DAV:");
            manager.AddNamespace("Office", "schemas-microsoft-com:office:office");
            manager.AddNamespace("Repl", "http://schemas.microsoft.com/repl/");
            manager.AddNamespace("Z", "urn:schemas-microsoft-com:");
            List<string> xmlFragments = new List<string>();
            int count = 0;
            foreach (IWebDavStoreItem webDavStoreItem in webDavStoreItems)
            {
                string frag;
                if (count == 0)
                {
                    frag = CreateFragment(true, responseDoc, webDavStoreItem, context, propname, requestUri, requestedProperties, lockSystem);
                }
                else
                {
                    WindowsIdentity userIdentity = (WindowsIdentity) Thread.GetData(Thread.GetNamedDataSlot(WebDavServer.HttpUser));
                    string cacheValue = (string) store.GetCacheObject(this, userIdentity.Name, webDavStoreItem.ItemPath);
                    if (cacheValue != null)
                    {
                        xmlFragments.Add(cacheValue);
                        continue;
                    }
                    frag = CreateFragment(false, responseDoc, webDavStoreItem, context, propname, requestUri, requestedProperties, lockSystem);
                    store.AddCacheObject(this, userIdentity.Name, webDavStoreItem.ItemPath, frag);
                }

                count++;

                xmlFragments.Add(frag);
            }

            StringBuilder sb = new StringBuilder(5000);
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?><D:multistatus xmlns:D=\"DAV:\" xmlns:Office=\"urn:schemas-microsoft-com:office:office\" xmlns:Repl=\"http://schemas.microsoft.com/repl/\" xmlns:Z=\"urn:schemas-microsoft-com:\">");
            foreach (string xmlFragment in xmlFragments)
                sb.Append(xmlFragment);
            sb.Append("</D:multistatus>");

            string t = sb.ToString();

            return t;
        }

        /// <summary>
        ///     Gives the
        ///     <see cref="XmlElement" /> of a
        ///     <see cref="WebDavProperty" />
        ///     with or without values
        ///     or with or without child elements
        /// </summary>
        /// <param name="webDavProperty">The <see cref="WebDavProperty" /></param>
        /// <param name="xmlDocument">The <see cref="XmlDocument" /> containing the response body</param>
        /// <param name="iWebDavStoreItem">The <see cref="IWebDavStoreItem" /></param>
        /// <param name="isPropname">The boolean defining the Propfind propname request</param>
        /// <returns>
        ///     The <see cref="XmlElement" /> of the <see cref="WebDavProperty" /> containing a value or child elements
        /// </returns>
        private static XmlElement PropChildElement(WebDavProperty webDavProperty, XmlDocument xmlDocument, IWebDavStoreItem iWebDavStoreItem, bool isPropname)
        {
            // If Propfind request contains a propname element
            if (isPropname)
            {
                webDavProperty.Value = Empty;
                return webDavProperty.ToXmlElement(xmlDocument);
            }

            // If not, add the values to webDavProperty
            webDavProperty.Value = GetWebDavPropertyValue(iWebDavStoreItem, webDavProperty);
            if (webDavProperty.Value == null)
                return null;

            XmlElement xmlElement = webDavProperty.ToXmlElement(xmlDocument);


            // If the webDavProperty is the resourcetype property
            // and the webDavStoreItem is a collection
            // add the collection XmlElement as a child to the xmlElement
            if (!webDavProperty.Name.Equals("resourcetype", StringComparison.InvariantCultureIgnoreCase) || !iWebDavStoreItem.IsCollection)
                return xmlElement;

            WebDavProperty collectionProperty = new WebDavProperty("collection", Empty);
            xmlElement.AppendChild(collectionProperty.ToXmlElement(xmlDocument));
            return xmlElement;
        }

        /// <summary>
        ///     Gets the correct value for a <see cref="WebDavProperty" />
        /// </summary>
        /// <param name="webDavStoreItem">The <see cref="IWebDavStoreItem" /> defines the values</param>
        /// <param name="davProperty">The <see cref="WebDavProperty" /> that needs a value</param>
        /// <returns>
        ///     A <see cref="string" /> containing the value
        /// </returns>
        private static string GetWebDavPropertyValue(IWebDavStoreItem webDavStoreItem, WebDavProperty davProperty)
        {
            if (webDavStoreItem is IWebDavStoreCollection && davProperty.StoreItemType == WebDavProperty.StoreItemTypeFlag.Document)
                return null;
            if (webDavStoreItem is IWebDavStoreDocument && davProperty.StoreItemType == WebDavProperty.StoreItemTypeFlag.Collection)
                return null;
            switch (davProperty.Name)
            {
                case "creationdate":
                    return webDavStoreItem.CreationDate.ToUniversalTime().ToString("s") + "Z";
                case "displayname":
                    return webDavStoreItem.Name;
                case "getcontentlanguage":
                    //todo getcontentlanguage
                    return Empty;
                case "getcontentlength":
                    return webDavStoreItem.Size.ToString(CultureInfo.InvariantCulture);
                case "getcontenttype":
                    return webDavStoreItem.MimeType;
                case "getetag":
                    return "\"{" + webDavStoreItem.Etag + "},0\"";
                case "getlastmodified":
                    return webDavStoreItem.ModificationDate.ToUniversalTime().ToString("R");
                case "resourcetype":
                    //todo Add resourceType
                    return "";
                case "ishidden":
                    return webDavStoreItem.GetFileInfo().Hidden ? "1" : "0";
                case "win32fileattributes":
                    string s = Convert.ToString((int) webDavStoreItem.GetFileInfo().GetAttributes(), 16);
                    char[] bits = s.PadLeft(8, '0').ToCharArray();
                    return new string(bits);
                case "isfolder":
                    return (webDavStoreItem is IWebDavStoreCollection) ? "t" : "";
                case "iscollection":
                    return (webDavStoreItem is IWebDavStoreCollection) ? "1" : "0";
                case "authoritative-directory":
                    return "t";
                case "repl-uid":
                    return "rid:{" + webDavStoreItem.GetRepl_uId() + "}";
                case "resourcetag":
                    return "rt:" + webDavStoreItem.GetRepl_uId() + "@00000000000";
                default:
                    return Empty;
            }
        }

        /// <summary>
        ///     Returns an XML Fragment which details the supported locks on this implementation.
        ///     15.10 supportedlock Property
        ///     Name:
        ///     supportedlock
        ///     Purpose:
        ///     To provide a listing of the lock capabilities supported by the resource.
        ///     Protected:
        ///     MUST be protected. Servers, not clients, determine what lock mechanisms are supported.
        ///     COPY/MOVE behavior:
        ///     This property value is dependent on the kind of locks supported at the destination, not on the value of the
        ///     property at the source resource. Servers attempting to COPY to a destination should not attempt to set this
        ///     property at the destination.
        ///     Description:
        ///     Returns a listing of the combinations of scope and access types that may be specified in a lock request on the
        ///     resource. Note that the actual contents are themselves controlled by access controls, so a server is not required
        ///     to provide information the client is not authorized to see. This property is NOT lockable with respect to write
        ///     locks (Section 7).
        /// </summary>
        /// <param name="responsedoc"></param>
        /// <returns></returns>
        private static XmlNode SupportedLocks(ref XmlDocument responsedoc)
        {
            XmlNode node = new WebDavProperty("supportedlock").ToXmlElement(responsedoc);

            XmlNode lockentry = new WebDavProperty("lockentry").ToXmlElement(responsedoc);
            node.AppendChild(lockentry);

            XmlNode lockscope = new WebDavProperty("lockscope").ToXmlElement(responsedoc);
            lockentry.AppendChild(lockscope);

            XmlNode exclusive = new WebDavProperty("exclusive").ToXmlElement(responsedoc);
            lockscope.AppendChild(exclusive);

            XmlNode locktype = new WebDavProperty("locktype").ToXmlElement(responsedoc);
            lockentry.AppendChild(locktype);

            XmlNode write = new WebDavProperty("write").ToXmlElement(responsedoc);
            locktype.AppendChild(write);

            XmlNode lockentry1 = new WebDavProperty("lockentry").ToXmlElement(responsedoc);
            node.AppendChild(lockentry1);

            XmlNode lockscope1 = new WebDavProperty("lockscope").ToXmlElement(responsedoc);
            lockentry1.AppendChild(lockscope1);

            XmlNode shared = new WebDavProperty("shared").ToXmlElement(responsedoc);
            lockscope1.AppendChild(shared);

            XmlNode locktype1 = new WebDavProperty("locktype").ToXmlElement(responsedoc);
            lockentry1.AppendChild(locktype1);

            XmlNode write1 = new WebDavProperty("write").ToXmlElement(responsedoc);
            locktype1.AppendChild(write1);

            return node;
        }

        /// <summary>
        ///     Returns the XML Format according to RFC
        ///     Name:
        ///     lockdiscovery
        ///     Purpose:
        ///     Describes the active locks on a resource
        ///     Protected:
        ///     MUST be protected. Clients change the list of locks through LOCK and UNLOCK, not through PROPPATCH.
        ///     COPY/MOVE behavior:
        ///     The value of this property depends on the lock state of the destination, not on the locks of the source resource.
        ///     Recall
        ///     that locks are not moved in a MOVE operation.
        ///     Description:
        ///     Returns a listing of who has a lock, what type of lock he has, the timeout type and the time remaining on the
        ///     timeout,
        ///     and the associated lock token. Owner information MAY be omitted if it is considered sensitive. If there are no
        ///     locks, but
        ///     the server supports locks, the property will be present but contain zero 'activelock' elements. If there are one or
        ///     more locks,
        ///     an 'activelock' element appears for each lock on the resource. This property is NOT lockable with respect to write
        ///     locks (Section 7).
        /// </summary>
        /// <param name="storeItem"></param>
        /// <param name="responsedoc"></param>
        /// <param name="lockSystem"></param>
        /// <returns></returns>
        private static XmlNode LockDiscovery(IWebDavStoreItem storeItem, ref XmlDocument responsedoc, IWebDavStoreItemLock lockSystem)
        {
            XmlNode node = new WebDavProperty("lockdiscovery").ToXmlElement(responsedoc);
            foreach (IWebDavStoreItemLockInstance ilock in lockSystem.GetLocks(storeItem))
            {
                XmlNode activelock = new WebDavProperty("activelock").ToXmlElement(responsedoc);
                node.AppendChild(activelock);

                XmlNode locktype = new WebDavProperty("locktype").ToXmlElement(responsedoc);
                activelock.AppendChild(locktype);

                XmlNode locktypeitem = new WebDavProperty(ilock.LockType.ToString().ToLower()).ToXmlElement(responsedoc);
                locktype.AppendChild(locktypeitem);

                XmlNode lockscope = new WebDavProperty("lockscope").ToXmlElement(responsedoc);
                activelock.AppendChild(lockscope);

                XmlNode lockscopeitem = new WebDavProperty(ilock.LockScope.ToString().ToLower()).ToXmlElement(responsedoc);
                lockscope.AppendChild(lockscopeitem);

                XmlNode depth = new WebDavProperty("depth").ToXmlElement(responsedoc);
                depth.InnerText = ilock.Depth.ToString(CultureInfo.InvariantCulture);
                activelock.AppendChild(depth);

                XmlNode owner = new WebDavProperty("owner").ToXmlElement(responsedoc);
                owner.InnerText = ilock.Owner;
                activelock.AppendChild(owner);

                XmlNode timeout = new WebDavProperty("timeout").ToXmlElement(responsedoc);
                timeout.InnerText = ilock.RequestedTimeout;
                activelock.AppendChild(timeout);

                XmlNode locktoken = new WebDavProperty("locktoken").ToXmlElement(responsedoc);
                activelock.AppendChild(locktoken);

                XmlNode tokenhref = new WebDavProperty("href").ToXmlElement(responsedoc);
                tokenhref.InnerText = ilock.Token.ToLockToken();
                locktoken.AppendChild(tokenhref);

                XmlNode lockroot = new WebDavProperty("lockroot").ToXmlElement(responsedoc);
                activelock.AppendChild(lockroot);

                XmlNode lockroothref = new WebDavProperty("href").ToXmlElement(responsedoc);
                lockroothref.InnerText = ilock.Path;
                lockroot.AppendChild(lockroothref);
            }

            return node;
        }

        #endregion

        #region SendResponse

        /// <summary>
        ///     Sends the response
        /// </summary>
        /// <param name="context">The <see cref="IHttpListenerContext" /> containing the response</param>
        /// <param name="responseDocument">The <see cref="XmlDocument" /> containing the response body</param>
        private static void SendResponse(IHttpListenerContext context, string responseDocument)
        {
            // convert the XmlDocument
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseDocument);

            // HttpStatusCode doesn't contain WebDav status codes, but HttpWorkerRequest can handle these WebDav status codes
            context.Response.StatusCode = (int) WebDavStatusCode.MultiStatus;
            context.Response.StatusDescription = HttpWorkerRequest.GetStatusDescription((int) WebDavStatusCode.MultiStatus);

            context.Response.ContentLength64 = responseBytes.Length;
            context.Response.AdaptedInstance.ContentType = "text/xml";
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

            context.Response.Close();
        }

        #endregion

        #endregion
    }
}