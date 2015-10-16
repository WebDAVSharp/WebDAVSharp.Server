using System;
using System.Net;
using System.Security.Principal;

namespace WebDAVSharp.Server.Adapters
{
    /// <summary>
    ///     This is an interface-version of the parts of
    ///     <see cref="HttpListener" /> that
    ///     the
    ///     <see cref="WebDavServer" /> requires to operator.
    /// </summary>
    /// <remarks>
    ///     The main purpose of this interface is to facilitate unit-testing.
    /// </remarks>
    public interface IHttpListener : IAdapter<HttpListener>, IDisposable
    {
        /// <summary>
        ///     Gets the Uniform Resource Identifier (
        ///     <see cref="Uri" />) prefixes handled by the
        ///     adapted
        ///     <see cref="HttpListener" /> object.
        /// </summary>
        /// <value>
        ///     The prefixes.
        /// </value>
        HttpListenerPrefixCollection Prefixes { get; }

        /// <summary>
        ///     Allows the adapted <see cref="HttpListener" /> to receive incoming requests.
        /// </summary>
        void Start();

        /// <summary>
        ///     Causes the adapted <see cref="HttpListener" /> to stop receiving incoming requests.
        /// </summary>
        void Stop();

        /// <summary>
        ///     Returns the windows Idenity to use for the request.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        IIdentity GetIdentity(IHttpListenerContext context);
    }
}