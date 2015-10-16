using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Utilities;
using static System.String;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    ///     This class implements the <c>PROPPATCH</c> HTTP method for WebDAV#.
    /// </summary>
    internal class WebDavProppatchMethodHandler : WebDavMethodHandlerBase
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
            "PROPPATCH"
        };

        #endregion

        #region Functions

        private static byte[] GetBytes(string bitString)
        {
            int i = Convert.ToInt32(bitString, 2);
            return BitConverter.GetBytes(i);

            //return 
            //    Enumerable.Range(0, bitString.Length / 8).
            //    Select(pos => Convert.ToByte(
            //        bitString.Substring(pos * 8, 8),
            //        2)
            //    ).ToArray();
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
        public override void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store)
        {
            /***************************************************************************************************
             * Retreive al the information from the request
             ***************************************************************************************************/

            // Get the URI to the location
            Uri requestUri = context.Request.Url;

            // Initiate the XmlNamespaceManager and the XmlNodes
            XmlNode propNode = null;


            StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
            string requestBody = reader.ReadToEnd();

            if (!IsNullOrEmpty(requestBody))
            {
                XmlDocument requestDocument = new XmlDocument();
                requestDocument.LoadXml(requestBody);

                if (requestDocument.DocumentElement != null)
                {
                    var manager = new XmlNamespaceManager(requestDocument.NameTable);
                    manager.AddNamespace("D", "DAV:");
                    manager.AddNamespace("Office", "schemas-microsoft-com:office:office");
                    manager.AddNamespace("Repl", "http://schemas.microsoft.com/repl/");
                    manager.AddNamespace("Z", "urn:schemas-microsoft-com:");
                    propNode = requestDocument.DocumentElement.SelectSingleNode("D:set/D:prop", manager);
                }
            }


            /***************************************************************************************************
             * Take action
             ***************************************************************************************************/
            // Get the parent collection of the item
            IWebDavStoreCollection collection = GetParentCollection(server, store, context.Request.Url);
            // Get the item from the collection
            IWebDavStoreItem item = GetItemFromCollection(collection, context.Request.Url);


            IWebDavFileInfo fileInfo = item.GetFileInfo();

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
                            FileAttributes fa = //(FileAttributes)int.Parse(node.InnerText);
                                (FileAttributes) Convert.ToInt32(node.InnerText, 16);
                            //(node.InnerText.All(c => c == '0' && c == '1')) ?
                            //(FileAttributes)BitConverter.ToInt32(GetBytes(node.InnerText), 0);// :
                            // (FileAttributes)int.Parse(node.InnerText);
                            fileInfo.ApplyAttributes(fa);
                            fileInfo.Apply();
                            break;
                    }
                }
            }
#if DoNotRun

    /***************************************************************************************************
             * Create the body for the response
             ***************************************************************************************************/

    // Create the basic response XmlDocument
            XmlDocument responseDoc = new XmlDocument();
            const string responseXml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><D:multistatus " +
                                       "xmlns:Z=\"urn:schemas-microsoft-com:\" xmlns:D=\"DAV:\">" +
                                       "<D:response></D:response></D:multistatus>";
            responseDoc.LoadXml(responseXml);

            // Select the response node
            XmlNode responseNode = responseDoc.DocumentElement.SelectSingleNode("D:response", manager);

            // Add the elements

            // The href element
            WebDavProperty hrefProperty = new WebDavProperty("href", requestUri.ToString());
            responseNode.AppendChild(hrefProperty.ToXmlElement(responseDoc));

            // The propstat element
            WebDavProperty propstatProperty = new WebDavProperty("propstat", string.Empty);
            XmlElement propstatElement = propstatProperty.ToXmlElement(responseDoc);

            // The propstat/status element
            WebDavProperty statusProperty = new WebDavProperty("status", "HTTP/1.1 " + context.Response.StatusCode + " " +
                                                                         HttpWorkerRequest.GetStatusDescription(context.Response.StatusCode));
            propstatElement.AppendChild(statusProperty.ToXmlElement(responseDoc));

            // The other propstat children
            foreach (WebDavProperty property in from XmlNode child in propNode.ChildNodes
                                                where 
                                                    child.Name.ToLower().Contains("creationtime") || 
                                                    child.Name.ToLower().Contains("fileattributes") || 
                                                    child.Name.ToLower().Contains("lastaccesstime") || 
                                                    child.Name.ToLower().Contains("lastmodifiedtime")

                                                let node = propNode.SelectSingleNode(child.Name, manager)
                                                select new WebDavProperty(child.LocalName, string.Empty, node != null ? node.NamespaceURI : string.Empty))

                propstatElement.AppendChild(property.ToXmlElement(responseDoc));

            responseNode.AppendChild(propstatElement);


            /***************************************************************************************************
            * Send the response
            ***************************************************************************************************/

            //</D:propstat>

            // convert the StringBuilder
            string resp = responseDoc.InnerXml;
#else
            StringBuilder sb = new StringBuilder(4000);
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?><D:multistatus xmlns:Z=\"urn:schemas-microsoft-com:\" xmlns:D=\"DAV:\"><D:response><D:href>");
            sb.Append(requestUri);
            sb.Append("</D:href>");
            if (propNode != null)
            {
                string statusCode = context.Response.StatusCode.ToString();
                string statusCodeDesc = HttpWorkerRequest.GetStatusDescription(context.Response.StatusCode);
                foreach (XmlNode child in propNode.ChildNodes.Cast<XmlNode>().Where(child => child.Name.ToLower().Contains("creationtime") || child.Name.ToLower().Contains("fileattributes") || child.Name.ToLower().Contains("lastaccesstime") || child.Name.ToLower().Contains("lastmodifiedtime")))
                {
                    sb.Append("<D:propstat><D:prop>");
                    sb.Append(child.LocalName);
                    sb.Append("</D:prop><D:status>HTTP/1.1 ");
                    sb.Append(statusCode);
                    sb.Append(" ");
                    sb.Append(statusCodeDesc);
                    sb.Append("</D:status></D:propstat>");
                }
            }
            sb.Append("</D:response></D:multistatus>");
            string resp = sb.ToString();

#endif
            byte[] responseBytes = Encoding.UTF8.GetBytes(resp);


            // HttpStatusCode doesn't contain WebDav status codes, but HttpWorkerRequest can handle these WebDav status codes
            context.Response.StatusCode = (int) WebDavStatusCode.MultiStatus;
            context.Response.StatusDescription = HttpWorkerRequest.GetStatusDescription((int) WebDavStatusCode.MultiStatus);

            // set the headers of the response
            context.Response.ContentLength64 = responseBytes.Length;
            context.Response.AdaptedInstance.ContentType = "text/xml";

            // the body
            context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

            context.Response.Close();
        }

        #endregion
    }
}