using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using Common.Logging;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Utilities;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    ///     This class implements the <c>PROPPATCH</c> HTTP method for WebDAV#.
    /// </summary>
    internal class WebDavProppatchMethodHandler : WebDavMethodHandlerBase, IWebDavMethodHandler
    {
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
                    "PROPPATCH"
                };
            }
        }

        /// <summary>
        ///     Processes the request.
        /// </summary>
        /// <param name="server">The <see cref="WebDavServer" /> through which the request came in from the client.</param>
        /// <param name="context">
        ///     The <see cref="IHttpListenerContext" /> object containing both the request and response objects to use.
        /// </param>
        /// <param name="store">The <see cref="IWebDavStore" /> that the <see cref="WebDavServer" /> is hosting.</param>
        public void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store)
        {
            ILog log = LogManager.GetCurrentClassLogger();

            /***************************************************************************************************
             * Retreive al the information from the request
             ***************************************************************************************************/

            // Get the URI to the location
            Uri requestUri = context.Request.Url;

            // Initiate the XmlNamespaceManager and the XmlNodes
            XmlNamespaceManager manager = null;
            XmlNode propNode = null;

            // try to read the body
            try
            {
                var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                string requestBody = reader.ReadToEnd();

                if (!requestBody.Equals(""))
                {
                    var requestDocument = new XmlDocument();
                    requestDocument.LoadXml(requestBody);

                    if (requestDocument.DocumentElement != null)
                    {
                        if (requestDocument.DocumentElement.LocalName != "propertyupdate")
                            log.Debug("PROPPATCH method without propertyupdate element in xml document");

                        manager = new XmlNamespaceManager(requestDocument.NameTable);
                        manager.AddNamespace("D", "DAV:");
                        manager.AddNamespace("Office", "schemas-microsoft-com:office:office");
                        manager.AddNamespace("Repl", "http://schemas.microsoft.com/repl/");
                        manager.AddNamespace("Z", "urn:schemas-microsoft-com:");

                        propNode = requestDocument.DocumentElement.SelectSingleNode("D:set/D:prop", manager);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn(ex.Message);
            }

            /***************************************************************************************************
             * Take action
             ***************************************************************************************************/

            // Get the parent collection of the item
            IWebDavStoreCollection collection = GetParentCollection(server, store, context.Request.Url);

            // Get the item from the collection
            IWebDavStoreItem item = GetItemFromCollection(collection, context.Request.Url);

            var fileInfo = new FileInfo(item.ItemPath);

            if (propNode != null && fileInfo.Exists)
            {
                foreach (XmlNode node in propNode.ChildNodes)
                {
                    switch (node.LocalName)
                    {
                        case "Win32CreationTime":
                            fileInfo.CreationTime = Convert.ToDateTime(node.InnerText).ToUniversalTime();
                            break;
                        case "Win32LastAccessTime":
                            fileInfo.LastAccessTime = Convert.ToDateTime(node.InnerText).ToUniversalTime();
                            break;
                        case "Win32LastModifiedTime":
                            fileInfo.LastWriteTime = Convert.ToDateTime(node.InnerText).ToUniversalTime();
                            break;
                        case "Win32FileAttributes":
                            //fileInfo.Attributes = 
                            //fileInfo.Attributes = Convert.ToDateTime(node.InnerText);
                            break;
                    }
                }
            }


            /***************************************************************************************************
             * Create the body for the response
             ***************************************************************************************************/

            // Create the basic response XmlDocument
            var responseDoc = new XmlDocument();
            const string responseXml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><D:multistatus " +
                                       "xmlns:Z=\"urn:schemas-microsoft-com:\" xmlns:D=\"DAV:\">" +
                                       "<D:response></D:response></D:multistatus>";
            responseDoc.LoadXml(responseXml);

            // Select the response node
            XmlNode responseNode = responseDoc.DocumentElement.SelectSingleNode("D:response", manager);

            // Add the elements

            // The href element
            var hrefProperty = new WebDavProperty("href", requestUri.ToString());
            responseNode.AppendChild(hrefProperty.ToXmlElement(responseDoc));

            // The propstat element
            var propstatProperty = new WebDavProperty("propstat", "");
            XmlElement propstatElement = propstatProperty.ToXmlElement(responseDoc);

            // The propstat/status element
            var statusProperty = new WebDavProperty("status",
                "HTTP/1.1 " + context.Response.StatusCode + " " +
                HttpWorkerRequest.GetStatusDescription(context.Response.StatusCode));
            propstatElement.AppendChild(statusProperty.ToXmlElement(responseDoc));

            // The other propstat children
            foreach (WebDavProperty property in from XmlNode child in propNode.ChildNodes
                where child.Name.ToLower()
                    .Contains("creationtime") || child.Name.ToLower()
                        .Contains("fileattributes") || child.Name.ToLower()
                            .Contains("lastaccesstime") || child.Name.ToLower()
                                .Contains("lastmodifiedtime")
                let node = propNode.SelectSingleNode(child.Name, manager)
                select node != null
                    ? new WebDavProperty(child.LocalName, "", node.NamespaceURI)
                    : new WebDavProperty(child.LocalName, "", ""))
                propstatElement.AppendChild(property.ToXmlElement(responseDoc));

            responseNode.AppendChild(propstatElement);

            /***************************************************************************************************
            * Send the response
            ***************************************************************************************************/

            // convert the StringBuilder
            string resp = responseDoc.InnerXml;
            byte[] responseBytes = Encoding.UTF8.GetBytes(resp);


            // HttpStatusCode doesn't contain WebDav status codes, but HttpWorkerRequest can handle these WebDav status codes
            context.Response.StatusCode = (int) WebDavStatusCode.MultiStatus;
            context.Response.StatusDescription =
                HttpWorkerRequest.GetStatusDescription((int) WebDavStatusCode.MultiStatus);

            // set the headers of the response
            context.Response.ContentLength64 = responseBytes.Length;
            context.Response.AdaptedInstance.ContentType = "text/xml";

            // the body
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

            context.Response.Close();
        }
    }
}