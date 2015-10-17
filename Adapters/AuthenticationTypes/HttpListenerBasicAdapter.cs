using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace WebDAVSharp.Server.Adapters.AuthenticationTypes
{
    internal class HttpListenerBasicAdapter : WebDavDisposableBase, IHttpListener
    {
        public HttpListenerBasicAdapter()
        {
            AdaptedInstance = new HttpListener
            {
                AuthenticationSchemes = AuthenticationSchemes.Basic,
                UnsafeConnectionNtlmAuthentication = false,
                TimeoutManager =
                {
                    RequestQueue = TimeSpan.FromMinutes(5),
                    DrainEntityBody = TimeSpan.FromMinutes(5),
                    EntityBody = TimeSpan.FromMinutes(5),
                    HeaderWait = TimeSpan.FromMinutes(5),
                    IdleConnection = TimeSpan.FromMinutes(5),
                    MinSendBytesPerSecond = 300
                },
                IgnoreWriteExceptions = true
            };
        }

        public HttpListener AdaptedInstance { get; }
        public HttpListenerPrefixCollection Prefixes => AdaptedInstance.Prefixes;

        public void Start()
        {
            AdaptedInstance.Start();
        }

        public void Stop()
        {
            AdaptedInstance.Stop();
        }

        public IIdentity GetIdentity(IHttpListenerContext context)
        {
            HttpListenerBasicIdentity ident = (HttpListenerBasicIdentity) context.AdaptedInstance.User.Identity;
            string domain = ident.Name.Split('\\')[0];
            string username = ident.Name.Split('\\')[1];
            SafeTokenHandle token = GetToken(domain, username, ident.Password);
            return new WindowsIdentity(token.DangerousGetHandle());
        }

        protected override void Dispose(bool disposing)
        {
            if (AdaptedInstance.IsListening)
                AdaptedInstance.Close();
        }

        internal static SafeTokenHandle GetToken(string domainName,
            string userName, string password)
        {
            SafeTokenHandle safeTokenHandle;

            const int logon32ProviderDefault = 0;
            //This parameter causes LogonUser to create a primary token.
            const int logon32LogonInteractive = 2;

            // Call LogonUser to obtain a handle to an access token.
            bool returnValue = LogonUser(userName, domainName, password,
                logon32LogonInteractive, logon32ProviderDefault,
                out safeTokenHandle);

            if (returnValue) return safeTokenHandle;
            int ret = Marshal.GetLastWin32Error();
            throw new Win32Exception(ret);
        }

        public sealed class SafeTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeTokenHandle()
                : base(true)
            {
            }

            [DllImport("kernel32.dll")]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            // ReSharper disable once MemberHidesStaticFromOuterClass
            private static extern bool CloseHandle(IntPtr handle);

            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }
        }

        #region Imports

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword, int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);

        #endregion
    }
}