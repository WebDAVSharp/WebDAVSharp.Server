using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;
using Common.Logging;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Utilities;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    ///     This class implements the <c>PROPFIND</c> HTTP method for WebDAV#.
    /// </summary>
    public class WebDavPropfindMethodHandler : WebDavMethodHandlerBase, IWebDavMethodHandler
    {
        private ILog _log;
        private Uri _requestUri;
        private List<WebDavProperty> _requestedProperties;
        private List<IWebDavStoreItem> _webDavStoreItems;

        /// <summary>
        ///     Gets the collection of the names of the HTTP methods handled by this instance.
        /// </summary>
        /// <value>
        ///     The names.
        /// </value>
        public IEnumerable<string> Names
        {
            get
            {
                return new[]
                {
                    "PROPFIND"
                };
            }
        }

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
        public void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store)
        {
            _log = LogManager.GetCurrentClassLogger();

            /***************************************************************************************************
             * Retreive all the information from the request
             ***************************************************************************************************/

            // Read the headers, ...
            bool isPropname = false;
            int depth = GetDepthHeader(context.Request);
            _requestUri = GetRequestUri(context.Request.Url.ToString());
            try
            {
                _webDavStoreItems = GetWebDavStoreItems(context.Request.Url.GetItem(server, store), depth);
            }
            catch (UnauthorizedAccessException)
            {
                throw new WebDavUnauthorizedException();
            }

            // Get the XmlDocument from the request
            XmlDocument requestDoc = GetXmlDocument(context.Request);

            // See what is requested
            _requestedProperties = new List<WebDavProperty>();
            if (requestDoc.DocumentElement != null)
            {
                if (requestDoc.DocumentElement.LocalName != "propfind")
                    _log.Debug("PROPFIND method without propfind in xml document");
                else
                {
                    XmlNode n = requestDoc.DocumentElement.FirstChild;
                    if (n == null)
                        _log.Debug("propfind element without children");
                    else
                    {
                        switch (n.LocalName)
                        {
                            case "allprop":
                                _requestedProperties = GetAllProperties();
                                break;
                            case "propname":
                                isPropname = true;
                                _requestedProperties = GetAllProperties();
                                break;
                            case "prop":
                                foreach (XmlNode child in n.ChildNodes)
                                    _requestedProperties.Add(new WebDavProperty(child.LocalName, "", child.NamespaceURI));
                                break;
                            default:
                                _requestedProperties.Add(new WebDavProperty(n.LocalName, "", n.NamespaceURI));
                                break;
                        }
                    }
                }
            }
            else
                _requestedProperties = GetAllProperties();

            /***************************************************************************************************
             * Create the body for the response
             ***************************************************************************************************/

            XmlDocument responseDoc = ResponseDocument(context, isPropname);

            /***************************************************************************************************
             * Send the response
             ***************************************************************************************************/

            SendResponse(context, responseDoc);
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
            ILog _log = LogManager.GetCurrentClassLogger();
            var list = new List<IWebDavStoreItem>();

            //IWebDavStoreCollection
            // if the item is a collection
            var collection = iWebDavStoreItem as IWebDavStoreCollection;
            if (collection != null)
            {
                list.Add(collection);
                if (depth == 0)
                    return list;
                foreach (IWebDavStoreItem item in collection.Items)
                {
                    try
                    {
                        list.Add(item);
                    }
                    catch (Exception ex)
                    {
                        _log.Debug(ex.Message + "\r\n" + ex.StackTrace);
                    }
                }
                return list;
            }
            // if the item is a document

            if (!(iWebDavStoreItem is IWebDavStoreDocument))
                throw new WebDavConflictException();
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
        private XmlDocument GetXmlDocument(IHttpListenerRequest request)
        {
            try
            {
                var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                string requestBody = reader.ReadToEnd();
                reader.Close();

                if (!requestBody.Equals(""))
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(requestBody);
                    return xmlDocument;
                }
            }
            catch (Exception)
            {
                _log.Warn("XmlDocument has not been read correctly");
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
        private List<WebDavProperty> GetAllProperties()
        {
            var list = new List<WebDavProperty>
            {
                new WebDavProperty("creationdate"),
                new WebDavProperty("displayname"),
                new WebDavProperty("getcontentlength"),
                new WebDavProperty("getcontenttype"),
                new WebDavProperty("getetag"),
                new WebDavProperty("getlastmodified"),
                new WebDavProperty("resourcetype"),
                new WebDavProperty("supportedlock"),
                new WebDavProperty("ishidden")
            };
            //list.Add(new WebDAVProperty("getcontentlanguage"));
            //list.Add(new WebDAVProperty("lockdiscovery"));
            return list;
        }

        #endregion

        #region BuildResponseBody

        /// <summary>
        ///     Builds the <see cref="XmlDocument" /> containing the response body
        /// </summary>
        /// <param name="context">The <see cref="IHttpListenerContext" /></param>
        /// <param name="propname">The boolean defining the Propfind propname request</param>
        /// <returns>
        ///     The <see cref="XmlDocument" /> containing the response body
        /// </returns>
        private XmlDocument ResponseDocument(IHttpListenerContext context, bool propname)
        {
            // Create the basic response XmlDocument
            var responseDoc = new XmlDocument();
            const string responseXml = "<?xml version=\"1.0\"?><D:multistatus xmlns:D=\"DAV:\"></D:multistatus>";
            responseDoc.LoadXml(responseXml);

            // Generate the manager
            var manager = new XmlNamespaceManager(responseDoc.NameTable);
            manager.AddNamespace("D", "DAV:");
            manager.AddNamespace("Office", "schemas-microsoft-com:office:office");
            manager.AddNamespace("Repl", "http://schemas.microsoft.com/repl/");
            manager.AddNamespace("Z", "urn:schemas-microsoft-com:");

            int count = 0;

            foreach (IWebDavStoreItem webDavStoreItem in _webDavStoreItems)
            {
                // Create the response element
                var responseProperty = new WebDavProperty("response", "");
                XmlElement responseElement = responseProperty.ToXmlElement(responseDoc);

                // The href element
                Uri result;
                if (count == 0)
                    Uri.TryCreate(_requestUri, "", out result);
                else
                    Uri.TryCreate(_requestUri, webDavStoreItem.Name, out result);
                var hrefProperty = new WebDavProperty("href", result.AbsoluteUri);
                responseElement.AppendChild(hrefProperty.ToXmlElement(responseDoc));
                count++;

                // The propstat element
                var propstatProperty = new WebDavProperty("propstat", "");
                XmlElement propstatElement = propstatProperty.ToXmlElement(responseDoc);

                // The prop element
                var propProperty = new WebDavProperty("prop", "");
                XmlElement propElement = propProperty.ToXmlElement(responseDoc);

                foreach (WebDavProperty davProperty in _requestedProperties)
                    propElement.AppendChild(PropChildElement(davProperty, responseDoc, webDavStoreItem, propname));

                // Add the prop element to the propstat element
                propstatElement.AppendChild(propElement);

                // The status element
                var statusProperty = new WebDavProperty("status",
                    "HTTP/1.1 " + context.Response.StatusCode + " " +
                    HttpWorkerRequest.GetStatusDescription(context.Response.StatusCode));
                propstatElement.AppendChild(statusProperty.ToXmlElement(responseDoc));

                // Add the propstat element to the response element
                responseElement.AppendChild(propstatElement);

                // Add the response element to the multistatus element
                responseDoc.DocumentElement.AppendChild(responseElement);
            }

            return responseDoc;
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
        private XmlElement PropChildElement(WebDavProperty webDavProperty, XmlDocument xmlDocument,
            IWebDavStoreItem iWebDavStoreItem, bool isPropname)
        {
            // If Propfind request contains a propname element
            if (isPropname)
            {
                webDavProperty.Value = "";
                return webDavProperty.ToXmlElement(xmlDocument);
            }
            // If not, add the values to webDavProperty
            webDavProperty.Value = GetWebDavPropertyValue(iWebDavStoreItem, webDavProperty);
            XmlElement xmlElement = webDavProperty.ToXmlElement(xmlDocument);

            // If the webDavProperty is the resourcetype property
            // and the webDavStoreItem is a collection
            // add the collection XmlElement as a child to the xmlElement
            if (webDavProperty.Name != "resourcetype" || !iWebDavStoreItem.IsCollection)
                return xmlElement;

            var collectionProperty = new WebDavProperty("collection", "");
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
        private string GetWebDavPropertyValue(IWebDavStoreItem webDavStoreItem, WebDavProperty davProperty)
        {
            switch (davProperty.Name)
            {
                case "creationdate":
                    return webDavStoreItem.CreationDate.ToUniversalTime()
                        .ToString("s") + "Z";
                case "displayname":
                    return webDavStoreItem.Name;
                case "getcontentlanguage":
                    // still to implement !!!
                    return "";
                case "getcontentlength":
                    return (!webDavStoreItem.IsCollection ? "" + ((IWebDavStoreDocument) webDavStoreItem).Size : "");
                case "getcontenttype":
                    return (!webDavStoreItem.IsCollection ? "" + ((IWebDavStoreDocument) webDavStoreItem).MimeType : "");
                case "getetag":
                    return (!webDavStoreItem.IsCollection ? "" + ((IWebDavStoreDocument) webDavStoreItem).Etag : "");
                case "getlastmodified":
                    return webDavStoreItem.ModificationDate.ToUniversalTime()
                        .ToString("R");
                case "lockdiscovery":
                    // still to implement !!!
                    return "";
                case "resourcetype":
                    return "";
                case "supportedlock":
                    // still to implement !!!
                    return "";
                    //webDavProperty.Value = "<D:lockentry><D:lockscope><D:shared/></D:lockscope><D:locktype><D:write/></D:locktype></D:lockentry>";
                case "ishidden":
                    return "" + webDavStoreItem.Hidden;
                default:
                    return "";
            }
        }

        #endregion

        #region SendResponse

        /// <summary>
        ///     Sends the response
        /// </summary>
        /// <param name="context">The <see cref="IHttpListenerContext" /> containing the response</param>
        /// <param name="responseDocument">The <see cref="XmlDocument" /> containing the response body</param>
        private void SendResponse(IHttpListenerContext context, XmlDocument responseDocument)
        {
            // convert the XmlDocument
            byte[] responseBytes = Encoding.UTF8.GetBytes(responseDocument.InnerXml);

            // HttpStatusCode doesn't contain WebDav status codes, but HttpWorkerRequest can handle these WebDav status codes
            context.Response.StatusCode = (int) WebDavStatusCode.MultiStatus;
            context.Response.StatusDescription = HttpWorkerRequest.GetStatusDescription((int) WebDavStatusCode.MultiStatus);
            context.Response.ContentLength64 = responseBytes.Length;
            context.Response.AdaptedInstance.ContentType = "text/xml";
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            context.Response.Close();
        }

        #endregion
    }
}