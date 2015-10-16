using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using WebDAVSharp.Server.Adapters;
using WebDAVSharp.Server.Exceptions;
using WebDAVSharp.Server.MethodHandlers;
using WebDAVSharp.Server.Stores;

#if DEBUG
using Common.Logging;
#endif

namespace WebDAVSharp.Server.Utilities
{
    /// <summary>
    /// </summary>
    public class HttpReciever
    {
#if DEBUG
        internal static readonly ILog _log = LogManager.GetLogger<HttpReciever>();
#endif
        private readonly string _name;

        private readonly IHttpListener _listener;
        private readonly Dictionary<string, IWebDavMethodHandler> _methodHandlers;
        private readonly WebDavServer _server;
        private readonly IWebDavStore _store;

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="listener"></param>
        /// <param name="handlers"></param>
        /// <param name="store"></param>
        /// <param name="server"></param>
        public HttpReciever(string name, ref IHttpListener listener, ref Dictionary<string, IWebDavMethodHandler> handlers, ref IWebDavStore store, WebDavServer server)
        {
            _listener = listener;
            _methodHandlers = handlers;
            _store = store;
            _server = server;
            _name = name;
        }

        private void ProcessRequest(object state)
        {
            DateTime startTime = DateTime.Now;

            IHttpListenerContext context = (IHttpListenerContext) state;
            string url = context.Request.Url.ToString().ToLower();

            if (url.EndsWith("desktop.ini") ||
                url.EndsWith("folder.gif") ||
                url.EndsWith("folder.jpg") ||
                url.EndsWith("thumbs.db"))
            {
                WebDavNotFoundException ex = new WebDavNotFoundException();
                context.Response.StatusCode = ex.StatusCode;
                context.Response.StatusDescription = ex.StatusDescription;
                if (ex.Message != context.Response.StatusDescription)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(ex.Message);
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Flush();
                }

                context.Response.Close();
                return;
            }

            var ident = _listener.GetIdentity(context);
            Thread.SetData(Thread.GetNamedDataSlot(WebDavServer.HttpUser), ident);
            _store.UserAuthenticated(ident);

            string method = context.Request.HttpMethod;

#if DEBUG
            
            _log.Warn("");
            _log.Warn("!!!!!!!!!!!!!!!!!!!!!! BEGIN Method Handler: " + method + "!!!!!!!!!!!!!!!!!!!!!!");
#endif
            try
            {
                try
                {
                    IWebDavMethodHandler methodHandler;

                    if (!_methodHandlers.TryGetValue(method, out methodHandler))
                        throw new WebDavMethodNotAllowedException(string.Format(CultureInfo.InvariantCulture, "%s ({0})", context.Request.HttpMethod));

                    context.Response.AppendHeader("DAV", "1,2,1#extend");

                    methodHandler.ProcessRequest(_server, context, _store);
                }
                catch (WebDavException)
                {
                    throw;
                }
                catch (UnauthorizedAccessException)
                {
                    throw new WebDavUnauthorizedException();
                }
                catch (FileNotFoundException ex)
                {
#if DEBUG
                    _log.Warn(ex.Message);
#endif
                    throw new WebDavNotFoundException(innerException: ex);
                }
                catch (DirectoryNotFoundException ex)
                {
#if DEBUG
                    _log.Warn(ex.Message);
#endif
                    throw new WebDavNotFoundException(innerException: ex);
                }
                catch (NotImplementedException ex)
                {
#if DEBUG
                    _log.Warn(ex.Message);
#endif
                    throw new WebDavNotImplementedException(innerException: ex);
                }
                catch (Exception ex)
                {
#if DEBUG
                    _log.Warn(ex.Message);
#endif
                    throw new WebDavInternalServerException(innerException: ex);
                }
            }
            catch (WebDavException ex)
            {
                if (ex.Message != "Not Found")
                {
                    Console.WriteLine(ex.Message + ex.StackTrace);
                    if (ex.InnerException != null)
                        Console.WriteLine(ex.InnerException.Message + ex.InnerException.StackTrace);
                }
#if DEBUG
                _log.Warn(ex.StatusCode + " " + ex.Message);
#else
                //Console.WriteLine("Method: " + method + " Processing URL: " + context.Request.Url + " Threw Exception: " + ex.Message);
#endif
                context.Response.StatusCode = ex.StatusCode;
                context.Response.StatusDescription = ex.StatusDescription;
                if (ex.Message != context.Response.StatusDescription)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(ex.Message);
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Flush();
                }

                context.Response.Close();
            }

            finally
            {
#if DEBUG
                _log.Warn("!!!!!!!!!!!!!!!!!!!!!! END Method Handler: " + method + "!!!!!!!!!!!!!!!!!!!!!! ------------->" + ((DateTime.Now - startTime).Milliseconds / 1000.00) + " seconds.");
#endif
            }
        }

        // ReSharper disable once InconsistentNaming
        internal bool _stop;

        /// <summary>
        /// </summary>
        public void Start()
        {
            _stop = false;
            _listener.AdaptedInstance.BeginGetContext(OnContext, this);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public void Stop()
        {
            _stop = true;
        }

        private static void OnContext(IAsyncResult ar)
        {
            HttpReciever server = (HttpReciever) ar.AsyncState;
            if (!server._stop)
                server._listener.AdaptedInstance.BeginGetContext(OnContext, server);
            IHttpListenerContext context = new HttpListenerContextAdapter(server._listener.AdaptedInstance.EndGetContext(ar));
            server.ProcessRequest(context);
        }
    }
}