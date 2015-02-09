using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.Stores;
using WebDAVSharp.Server.Stores.Locks;

namespace WebDAVSharp.Server.MethodHandlers
{
    /// <summary>
    /// This class implements the <c>LOCK</c> HTTP method for WebDAV#.
    /// </summary>
    internal class WebDavLockMethodHandler : WebDavMethodHandlerBase, IWebDavMethodHandler
    {
        
        #region Properties

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
                    "LOCK"
                };
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="server">The <see cref="WebDavServer" /> through which the request came in from the client.</param>
        /// <param name="context">The
        /// <see cref="IHttpListenerContext" /> object containing both the request and response
        /// objects to use.</param>
        /// <param name="store">The <see cref="IWebDavStore" /> that the <see cref="WebDavServer" /> is hosting.</param>
        /// <exception cref="WebDAVSharp.Server.Exceptions.WebDavPreconditionFailedException"></exception>
        public void ProcessRequest(WebDavServer server, IHttpListenerContext context, IWebDavStore store)
        {


            /***************************************************************************************************
             * Retreive al the information from the request
             ***************************************************************************************************/

            // read the headers
            int depth = GetDepthHeader(context.Request);
            string timeout = GetTimeoutHeader(context.Request);
            string locktoken = GetLockTokenIfHeader(context.Request);
            int lockResult;
            // Initiate the XmlNamespaceManager and the XmlNodes
            XmlNamespaceManager manager;
            XmlNode lockscopeNode, locktypeNode, ownerNode;
            XmlDocument requestDocument = new XmlDocument();

            if (string.IsNullOrEmpty(locktoken))
            {
                #region New Lock
                // try to read the body
                try
                {
                    StreamReader reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                    string requestBody = reader.ReadToEnd();

                    if (!requestBody.Equals("") && requestBody.Length != 0)
                    {

                        requestDocument.LoadXml(requestBody);

                        if (requestDocument.DocumentElement != null &&
                            requestDocument.DocumentElement.LocalName != "prop" &&
                            requestDocument.DocumentElement.LocalName != "lockinfo")
                        {
                            WebDavServer.Log.Debug("LOCK method without prop or lockinfo element in xml document");
                        }

                        manager = new XmlNamespaceManager(requestDocument.NameTable);
                        manager.AddNamespace("D", "DAV:");
                        manager.AddNamespace("Office", "schemas-microsoft-com:office:office");
                        manager.AddNamespace("Repl", "http://schemas.microsoft.com/repl/");
                        manager.AddNamespace("Z", "urn:schemas-microsoft-com:");

                        // Get the lockscope, locktype and owner as XmlNodes from the XML document
                        lockscopeNode = requestDocument.DocumentElement.SelectSingleNode("D:lockscope", manager);
                        locktypeNode = requestDocument.DocumentElement.SelectSingleNode("D:locktype", manager);
                        ownerNode = requestDocument.DocumentElement.SelectSingleNode("D:owner", manager);
                    }
                    else
                    {
                        throw new WebDavPreconditionFailedException();
                    }
                }
                catch (Exception ex)
                {
                    WebDavServer.Log.Warn(ex.Message);
                    throw;
                }


                /***************************************************************************************************
                * Lock the file or folder
                ***************************************************************************************************/


                // Get the parent collection of the item
                IWebDavStoreCollection collection = GetParentCollection(server, store, context.Request.Url);

                WebDavLockScope lockscope = (lockscopeNode.InnerXml.StartsWith("<D:exclusive"))
                    ? WebDavLockScope.Exclusive
                    : WebDavLockScope.Shared;

                //Only lock available at this time is a Write Lock according to RFC
                WebDavLockType locktype = (locktypeNode.InnerXml.StartsWith("<D:write")) ? WebDavLockType.Write : WebDavLockType.Write;

                string lockuser = ownerNode.InnerText;

                WindowsIdentity Identity = (WindowsIdentity)Thread.GetData(Thread.GetNamedDataSlot(WebDavServer.HttpUser));

                lockResult = WebDavStoreItemLock.Lock(context.Request.Url, lockscope, locktype, Identity.Name, ref timeout,
                    out locktoken, requestDocument, depth);

                // Get the item from the collection
                try
                {
                    GetItemFromCollection(collection, context.Request.Url);
                }
                catch (Exception)
                {
                    lockResult = (int)HttpStatusCode.Created;
                }
                #endregion
            }
            else
            {
                #region Refreshing a lock
                //Refresh lock will ref us back the original XML document which was used to request this lock, from
                //this we will grab the data we need to build the response to the lock refresh request.
                lockResult = WebDavStoreItemLock.RefreshLock(context.Request.Url, locktoken, ref timeout, out requestDocument);
                if (requestDocument == null)
                {
                    context.SendSimpleResponse(409);
                    return;
                }

                try
                {
                    if (requestDocument.DocumentElement != null &&
                        requestDocument.DocumentElement.LocalName != "prop" &&
                        requestDocument.DocumentElement.LocalName != "lockinfo")
                    {
                        WebDavServer.Log.Debug("LOCK method without prop or lockinfo element in xml document");
                    }

                    manager = new XmlNamespaceManager(requestDocument.NameTable);
                    manager.AddNamespace("D", "DAV:");
                    manager.AddNamespace("Office", "schemas-microsoft-com:office:office");
                    manager.AddNamespace("Repl", "http://schemas.microsoft.com/repl/");
                    manager.AddNamespace("Z", "urn:schemas-microsoft-com:");

                    // Get the lockscope, locktype and owner as XmlNodes from the XML document
                    lockscopeNode = requestDocument.DocumentElement.SelectSingleNode("D:lockscope", manager);
                    locktypeNode = requestDocument.DocumentElement.SelectSingleNode("D:locktype", manager);
                    ownerNode = requestDocument.DocumentElement.SelectSingleNode("D:owner", manager);
                }
                catch (Exception ex)
                {
                    WebDavServer.Log.Warn(ex.Message);
                    throw;
                }

                #endregion
            }

            /***************************************************************************************************
             * Create the body for the response
             ***************************************************************************************************/

            // Create the basic response XmlDocument
            XmlDocument responseDoc = new XmlDocument();
            const string responseXml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><D:prop xmlns:D=\"DAV:\"><D:lockdiscovery><D:activelock/></D:lockdiscovery></D:prop>";
            responseDoc.LoadXml(responseXml);

            // Select the activelock XmlNode
            XmlNode activelock = responseDoc.DocumentElement.SelectSingleNode("D:lockdiscovery/D:activelock", manager);

            // Import the given nodes
            activelock.AppendChild(responseDoc.ImportNode(lockscopeNode, true));
            activelock.AppendChild(responseDoc.ImportNode(locktypeNode, true));
            activelock.AppendChild(responseDoc.ImportNode(ownerNode, true));

            // Add the additional elements, e.g. the header elements

            // The timeout element
            WebDavProperty timeoutProperty = new WebDavProperty("timeout", timeout);// timeout);
            activelock.AppendChild(timeoutProperty.ToXmlElement(responseDoc));

            // The depth element
            WebDavProperty depthProperty = new WebDavProperty("depth", (depth == 0 ? "0" : "Infinity"));
            activelock.AppendChild(depthProperty.ToXmlElement(responseDoc));

            // The locktoken element
            WebDavProperty locktokenProperty = new WebDavProperty("locktoken", string.Empty);
            XmlElement locktokenElement = locktokenProperty.ToXmlElement(responseDoc);
            WebDavProperty hrefProperty = new WebDavProperty("href", locktoken);//"opaquelocktoken:e71d4fae-5dec-22df-fea5-00a0c93bd5eb1");
            locktokenElement.AppendChild(hrefProperty.ToXmlElement(responseDoc));


            activelock.AppendChild(locktokenElement);

            /***************************************************************************************************
             * Send the response
             ***************************************************************************************************/

            // convert the StringBuilder
            string resp = responseDoc.InnerXml;
            byte[] responseBytes = Encoding.UTF8.GetBytes(resp);


            context.Response.StatusCode = lockResult;
            context.Response.StatusDescription = HttpWorkerRequest.GetStatusDescription(lockResult);

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