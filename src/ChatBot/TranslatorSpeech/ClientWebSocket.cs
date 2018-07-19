////------------------------------------------------------------------------------
//// <copyright file="ClientWebSocket.cs" company="Microsoft">
////     Copyright (c) Microsoft Corporation.  All rights reserved.
//// </copyright>
////
//// ClientWebSocket modified to extract "RequestId" header during protocol handshake.
//// Most modifications in the "Helpers" regions mostly re-define constants to make them
//// accessible or use reflection to call non-public methods.
//// Changes in ClientWebSocket are limited to:
//// 1) adding code to fetch the requestId: property which is set in ConnectAsyncCore
//// 2) changing Uri.SchemeWs and Uri.SchemeWss to string literals.
//// 3) in the options class calling a helper in Ext to create WebHeaderCollection instance.
////------------------------------------------------------------------------------

namespace ChatBot.TranslatorSpeech
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using System.Net.WebSockets;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    #region Helpers

    internal class Logging
    {
        internal static bool On { get { return false; } }
        internal const string WebSockets = "";
        internal static void Enter(string key, params object[] args) { }
        internal static void Exit(string key, params object[] args) { }
        internal static void Associate(string key, params object[] args) { }
        internal static void Exception(string key, params object[] args) { }
    }

    internal static class Ext
    {
        public static Type FindType(string name)
        {
            return (from a in AppDomain.CurrentDomain.GetAssemblies()
                    let t = a.GetType(name)
                    where t != null
                    select t).First();
        }

        public static ConfiguredTaskAwaitable<T> SuppressContextFlow<T>(this Task<T> task)
        {
            // We don't flow the synchronization context within WebSocket.xxxAsync - but the calling application
            // can decide whether the completion callback for the task returned from WebSocket.xxxAsync runs
            // under the caller's synchronization context.
            return task.ConfigureAwait(false);
        }

        internal static WebHeaderCollection CreateWebHeaderCollection()
        {
            //return new WebHeaderCollection(ReflectionHelpers.WebHeaderCollectionType.HttpWebRequest);
            var ty = FindType("System.Net.WebHeaderCollection");
            var ctor = ty
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(ci => (ci.GetParameters().Count() == 1) &&
                                (ci.GetParameters()[0].ParameterType.Name == "WebHeaderCollectionType"))
                .First();
            var tye = FindType("System.Net.WebHeaderCollectionType");
            return (WebHeaderCollection)ctor.Invoke(new object[] { tye.GetField("HttpWebRequest").GetValue(null) });
        }
    }

    internal class WebSocketProtocolComponent
    {
        private static PropertyInfo isSupportedPropInfo;

        static WebSocketProtocolComponent()
        {
            isSupportedPropInfo = Ext.FindType("System.Net.WebSockets.WebSocketProtocolComponent")
                .GetProperty("IsSupported", BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static bool IsSupported { get { return (bool)isSupportedPropInfo.GetValue(null); } }
    }

    internal class WebSocketHelpers
    {
        private static MethodInfo throwPlatformNotSupportedExceptionMethInfo;
        private static MethodInfo validateBufferSizesMethInfo;
        private static MethodInfo validateArraySegmentMethInfo;
        private static MethodInfo getSecWebSocketAcceptStringMethInfo;
        private static MethodInfo validateSubprotocolMethInfo;

        static WebSocketHelpers()
        {
            var ty = Ext.FindType("System.Net.WebSockets.WebSocketHelpers");
            getSecWebSocketAcceptStringMethInfo =
                ty.GetMethod("GetSecWebSocketAcceptString", BindingFlags.Static | BindingFlags.NonPublic);
            throwPlatformNotSupportedExceptionMethInfo =
                ty.GetMethod("ThrowPlatformNotSupportedException_WSPC", BindingFlags.Static | BindingFlags.NonPublic);
            validateBufferSizesMethInfo =
                ty.GetMethod("ValidateBufferSizes", BindingFlags.Static | BindingFlags.NonPublic);
            validateArraySegmentMethInfo =
                ty.GetMethod("ValidateArraySegment", BindingFlags.Static | BindingFlags.NonPublic);
            validateSubprotocolMethInfo =
                ty.GetMethod("ValidateSubprotocol", BindingFlags.Static | BindingFlags.NonPublic);
        }

        public const string WebSocketUpgradeToken = "websocket";
        public const int DefaultReceiveBufferSize = 16 * 1024;
        public const int DefaultClientSendBufferSize = 16 * 1024;

        internal static string GetSecWebSocketAcceptString(string p)
        {
            return (string)getSecWebSocketAcceptStringMethInfo.Invoke(null, new object[] { p });
        }

        internal static void ThrowPlatformNotSupportedException_WSPC()
        {
            throwPlatformNotSupportedExceptionMethInfo.Invoke(null, null);
        }

        internal static void ValidateBufferSizes(int receiveBufferSize, int sendBufferSize)
        {
            validateBufferSizesMethInfo.Invoke(null, new object[] { receiveBufferSize, sendBufferSize });
        }

        internal static void ValidateArraySegment<T>(ArraySegment<T> arraySegment, string parameterName)
        {
            validateArraySegmentMethInfo.Invoke(null, new object[] { arraySegment, parameterName });
        }

        internal static void ValidateSubprotocol(string subProtocol)
        {
            validateSubprotocolMethInfo.Invoke(null, new object[] { subProtocol });
        }
    }

    internal class WebSocketBuffer
    {
        private static MethodInfo validateMethInfo;

        static WebSocketBuffer()
        {
            var ty = Ext.FindType("System.Net.WebSockets.WebSocketHelpers");
            validateMethInfo = ty.GetMethod("Validate", BindingFlags.Static | BindingFlags.NonPublic);
        }

        internal static void Validate(int p1, int receiveBufferSize, int sendBufferSize, bool p2)
        {
            validateMethInfo.Invoke(null, new object[] { p1, receiveBufferSize, sendBufferSize, p2 });
        }
    }

    internal class SR
    {
        private static Type ty;
        private static MethodInfo getString1MethInfo;
        private static MethodInfo getString2MethInfo;

        public static string net_webstatus_ConnectFailure { get; private set; }
        public static string net_WebSockets_InvalidResponseHeader { get; private set; }
        public static string net_uri_NotAbsolute { get; private set; }
        public static string net_WebSockets_Scheme { get; private set; }
        public static string net_WebSockets_AlreadyStarted { get; private set; }
        public static string net_WebSockets_InvalidRegistration { get; private set; }
        public static string net_WebSockets_Connect101Expected { get; private set; }
        public static string net_WebSockets_AcceptUnsupportedProtocol { get; private set; }
        public static string net_WebSockets_NotConnected { get; private set; }
        public static string net_WebSockets_NoDuplicateProtocol { get; private set; }
        public static string net_WebSockets_ArgumentOutOfRange_TooSmall { get; private set; }

        static SR()
        {
            ty = Ext.FindType("System.SR");
            getString1MethInfo = ty.GetMethods()
                .Where(m => m.ToString() == "System.String GetString(System.String)").First();
            getString2MethInfo = ty.GetMethods()
                .Where(m => m.ToString() == "System.String GetString(System.String, System.Object[])").First();
            net_webstatus_ConnectFailure = GetFieldConst("net_webstatus_ConnectFailure");
            net_WebSockets_InvalidResponseHeader = GetFieldConst("net_WebSockets_InvalidResponseHeader");
            net_uri_NotAbsolute = GetFieldConst("net_uri_NotAbsolute");
            net_WebSockets_Scheme = GetFieldConst("net_WebSockets_Scheme");
            net_WebSockets_AlreadyStarted = GetFieldConst("net_WebSockets_AlreadyStarted");
            net_WebSockets_InvalidRegistration = GetFieldConst("net_WebSockets_InvalidRegistration");
            net_WebSockets_Connect101Expected = GetFieldConst("net_WebSockets_Connect101Expected");
            net_WebSockets_AcceptUnsupportedProtocol = GetFieldConst("net_WebSockets_AcceptUnsupportedProtocol");
            net_WebSockets_NotConnected = GetFieldConst("net_WebSockets_NotConnected");
            net_WebSockets_NoDuplicateProtocol = GetFieldConst("net_WebSockets_NoDuplicateProtocol");
            net_WebSockets_ArgumentOutOfRange_TooSmall = GetFieldConst("net_WebSockets_ArgumentOutOfRange_TooSmall");
        }

        private static string GetFieldConst(string key)
        {
            return (string)ty.GetField(key, BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
        }

        public static string GetString(string key)
        {
            return (string)getString1MethInfo.Invoke(null, new object[] { key });
        }

        public static string GetString(string key, params object[] args)
        {
            return (string)getString2MethInfo.Invoke(null, new object[] { key, args });
        }
    }

    internal static class HttpKnownHeaderNames
    {
        public const string CacheControl = "Cache-Control";
        public const string Connection = "Connection";
        public const string Date = "Date";
        public const string KeepAlive = "Keep-Alive";
        public const string Pragma = "Pragma";
        public const string ProxyConnection = "Proxy-Connection";
        public const string Trailer = "Trailer";
        public const string TransferEncoding = "Transfer-Encoding";
        public const string Upgrade = "Upgrade";
        public const string Via = "Via";
        public const string Warning = "Warning";
        public const string ContentLength = "Content-Length";
        public const string ContentType = "Content-Type";
        public const string ContentDisposition = "Content-Disposition";
        public const string ContentEncoding = "Content-Encoding";
        public const string ContentLanguage = "Content-Language";
        public const string ContentLocation = "Content-Location";
        public const string ContentRange = "Content-Range";
        public const string Expires = "Expires";
        public const string LastModified = "Last-Modified";
        public const string Age = "Age";
        public const string Location = "Location";
        public const string ProxyAuthenticate = "Proxy-Authenticate";
        public const string RetryAfter = "Retry-After";
        public const string Server = "Server";
        public const string SetCookie = "Set-Cookie";
        public const string SetCookie2 = "Set-Cookie2";
        public const string Vary = "Vary";
        public const string WWWAuthenticate = "WWW-Authenticate";
        public const string Accept = "Accept";
        public const string AcceptCharset = "Accept-Charset";
        public const string AcceptEncoding = "Accept-Encoding";
        public const string AcceptLanguage = "Accept-Language";
        public const string Authorization = "Authorization";
        public const string Cookie = "Cookie";
        public const string Cookie2 = "Cookie2";
        public const string Expect = "Expect";
        public const string From = "From";
        public const string Host = "Host";
        public const string IfMatch = "If-Match";
        public const string IfModifiedSince = "If-Modified-Since";
        public const string IfNoneMatch = "If-None-Match";
        public const string IfRange = "If-Range";
        public const string IfUnmodifiedSince = "If-Unmodified-Since";
        public const string MaxForwards = "Max-Forwards";
        public const string ProxyAuthorization = "Proxy-Authorization";
        public const string Referer = "Referer";
        public const string Range = "Range";
        public const string UserAgent = "User-Agent";
        public const string ContentMD5 = "Content-MD5";
        public const string ETag = "ETag";
        public const string TE = "TE";
        public const string Allow = "Allow";
        public const string AcceptRanges = "Accept-Ranges";
        public const string P3P = "P3P";
        public const string XPoweredBy = "X-Powered-By";
        public const string XAspNetVersion = "X-AspNet-Version";
        public const string SecWebSocketKey = "Sec-WebSocket-Key";
        public const string SecWebSocketExtensions = "Sec-WebSocket-Extensions";
        public const string SecWebSocketAccept = "Sec-WebSocket-Accept";
        public const string Origin = "Origin";
        public const string SecWebSocketProtocol = "Sec-WebSocket-Protocol";
        public const string SecWebSocketVersion = "Sec-WebSocket-Version";
    }

    #endregion

#if false
    //TODO:remove and use standard ClientWebSocket.
    public sealed class ClientWebSocket : WebSocket
    {
        private readonly ClientWebSocketOptions options;
        private WebSocket innerWebSocket;
        private readonly CancellationTokenSource cts;

        // Stages of this class. Interlocked doesn't support enums.
        private int state;
        private const int created = 0;
        private const int connecting = 1;
        private const int connected = 2;
        private const int disposed = 3;

        static ClientWebSocket()
        {
            // Register ws: and wss: with WebRequest.Register so that WebRequest.Create returns a 
            // WebSocket capable HttpWebRequest instance.
            WebSocket.RegisterPrefixes();
        }

        public ClientWebSocket()
        {
            if (Logging.On) Logging.Enter(Logging.WebSockets, this, ".ctor", null);

            if (!WebSocketProtocolComponent.IsSupported)
            {
                WebSocketHelpers.ThrowPlatformNotSupportedException_WSPC();
            }

            state = created;
            options = new ClientWebSocketOptions();
            cts = new CancellationTokenSource();

            if (Logging.On) Logging.Exit(Logging.WebSockets, this, ".ctor", null);
        }

#region Properties

        public ClientWebSocketOptions Options { get { return options; } }

        public override WebSocketCloseStatus? CloseStatus
        {
            get
            {
                if (innerWebSocket != null)
                {
                    return innerWebSocket.CloseStatus;
                }
                return null;
            }
        }

        public override string CloseStatusDescription
        {
            get
            {
                if (innerWebSocket != null)
                {
                    return innerWebSocket.CloseStatusDescription;
                }
                return null;
            }
        }

        public override string SubProtocol
        {
            get
            {
                if (innerWebSocket != null)
                {
                    return innerWebSocket.SubProtocol;
                }
                return null;
            }
        }

        public override WebSocketState State
        {
            get
            {
                // state == Connected or Disposed
                if (innerWebSocket != null)
                {
                    return innerWebSocket.State;
                }
                switch (state)
                {
                    case created:
                        return WebSocketState.None;
                    case connecting:
                        return WebSocketState.Connecting;
                    case disposed: // We only get here if disposed before connecting
                        return WebSocketState.Closed;
                    default:
                        Contract.Assert(false, "NotImplemented: " + state);
                        return WebSocketState.Closed;
                }
            }
        }

        /// <summary>
        /// Gets the RequestId returned by speech translation API.
        /// </summary>
        public string RequestId { get; private set; }

#endregion Properties

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException(SR.GetString(SR.net_uri_NotAbsolute), "uri");
            }
            //if (uri.Scheme != Uri.UriSchemeWs && uri.Scheme != Uri.UriSchemeWss)
            if (uri.Scheme != "ws" && uri.Scheme != "wss")
            {
                throw new ArgumentException(SR.GetString(SR.net_WebSockets_Scheme), "uri");
            }

            // Check that we have not started already
            int priorState = Interlocked.CompareExchange(ref state, connecting, created);
            if (priorState == disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if (priorState != created)
            {
                throw new InvalidOperationException(SR.GetString(SR.net_WebSockets_AlreadyStarted));
            }
            options.SetToReadOnly();

            return ConnectAsyncCore(uri, cancellationToken);
        }

        private async Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken)
        {
            HttpWebResponse response = null;
            CancellationTokenRegistration connectCancellation = new CancellationTokenRegistration();
            // Any errors from here on out are fatal and this instance will be disposed.
            try
            {
                HttpWebRequest request = CreateAndConfigureRequest(uri);
                if (Logging.On) Logging.Associate(Logging.WebSockets, this, request);

                connectCancellation = cancellationToken.Register(AbortRequest, request, false);

                response = await request.GetResponseAsync().SuppressContextFlow() as HttpWebResponse;
                Contract.Assert(response != null, "Not an HttpWebResponse");

                this.RequestId = response.Headers.Get("X-RequestId");

                if (Logging.On) Logging.Associate(Logging.WebSockets, this, response);

                string subprotocol = ValidateResponse(request, response);

                innerWebSocket = WebSocket.CreateClientWebSocket(response.GetResponseStream(), subprotocol,
                    options.ReceiveBufferSize, options.SendBufferSize, options.KeepAliveInterval, false,
                    options.GetOrCreateBuffer());

                if (Logging.On) Logging.Associate(Logging.WebSockets, this, innerWebSocket);

                // Change internal state to 'connected' to enable the other methods
                if (Interlocked.CompareExchange(ref state, connected, connecting) != connecting)
                {
                    // Aborted/Disposed during connect.
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }
            catch (WebException ex)
            {
                ConnectExceptionCleanup(response);
                WebSocketException wex = new WebSocketException(SR.GetString(SR.net_webstatus_ConnectFailure), ex);
                if (Logging.On) Logging.Exception(Logging.WebSockets, this, "ConnectAsync", wex);
                throw wex;
            }
            catch (Exception ex)
            {
                ConnectExceptionCleanup(response);
                if (Logging.On) Logging.Exception(Logging.WebSockets, this, "ConnectAsync", ex);
                throw;
            }
            finally
            {
                // We successfully connected (or failed trying), disengage from this token.  
                // Otherwise any timeout/cancellation would apply to the full session.
                // In the failure case we need to release the reference to HWR.
                connectCancellation.Dispose();
            }
        }

        private void ConnectExceptionCleanup(HttpWebResponse response)
        {
            Dispose();
            if (response != null)
            {
                response.Dispose();
            }
        }

        private HttpWebRequest CreateAndConfigureRequest(Uri uri)
        {
            HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
            if (request == null)
            {
                throw new InvalidOperationException(SR.GetString(SR.net_WebSockets_InvalidRegistration));
            }

            // Request Headers
            foreach (string key in options.RequestHeaders.Keys)
            {
                request.Headers.Add(key, options.RequestHeaders[key]);
            }

            // SubProtocols
            if (options.RequestedSubProtocols.Count > 0)
            {
                request.Headers.Add(HttpKnownHeaderNames.SecWebSocketProtocol,
                    string.Join(", ", options.RequestedSubProtocols));
            }

            // Creds
            if (options.UseDefaultCredentials)
            {
                request.UseDefaultCredentials = true;
            }
            else if (options.Credentials != null)
            {
                request.Credentials = options.Credentials;
            }

            // Certs
            if (options.InternalClientCertificates != null)
            {
                request.ClientCertificates = options.InternalClientCertificates;
            }

            request.Proxy = options.Proxy;
            request.CookieContainer = options.Cookies;

            // For Abort/Dispose.  Calling Abort on the request at any point will close the connection.
            cts.Token.Register(AbortRequest, request, false);

            return request;
        }

        // Validate the response headers and return the sub-protocol.
        private string ValidateResponse(HttpWebRequest request, HttpWebResponse response)
        {
            // 101
            if (response.StatusCode != HttpStatusCode.SwitchingProtocols)
            {
                throw new WebSocketException(SR.GetString(SR.net_WebSockets_Connect101Expected,
                    (int)response.StatusCode));
            }

            // Upgrade: websocket
            string upgradeHeader = response.Headers[HttpKnownHeaderNames.Upgrade];
            if (!string.Equals(upgradeHeader, WebSocketHelpers.WebSocketUpgradeToken,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new WebSocketException(SR.GetString(SR.net_WebSockets_InvalidResponseHeader,
                    HttpKnownHeaderNames.Upgrade, upgradeHeader));
            }

            // Connection: Upgrade
            string connectionHeader = response.Headers[HttpKnownHeaderNames.Connection];
            if (!string.Equals(connectionHeader, HttpKnownHeaderNames.Upgrade,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new WebSocketException(SR.GetString(SR.net_WebSockets_InvalidResponseHeader,
                    HttpKnownHeaderNames.Connection, connectionHeader));
            }

            // Sec-WebSocket-Accept derived from request Sec-WebSocket-Key
            string websocketAcceptHeader = response.Headers[HttpKnownHeaderNames.SecWebSocketAccept];
            string expectedAcceptHeader = WebSocketHelpers.GetSecWebSocketAcceptString(
                request.Headers[HttpKnownHeaderNames.SecWebSocketKey]);
            if (!string.Equals(websocketAcceptHeader, expectedAcceptHeader, StringComparison.OrdinalIgnoreCase))
            {
                throw new WebSocketException(SR.GetString(SR.net_WebSockets_InvalidResponseHeader,
                    HttpKnownHeaderNames.SecWebSocketAccept, websocketAcceptHeader));
            }

            // Sec-WebSocket-Protocol matches one from request
            // A missing header is ok.  It's also ok if the client didn't specify any.
            string subProtocol = response.Headers[HttpKnownHeaderNames.SecWebSocketProtocol];
            if (!string.IsNullOrWhiteSpace(subProtocol) && options.RequestedSubProtocols.Count > 0)
            {
                bool foundMatch = false;
                foreach (string requestedSubProtocol in options.RequestedSubProtocols)
                {
                    if (string.Equals(requestedSubProtocol, subProtocol, StringComparison.OrdinalIgnoreCase))
                    {
                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch)
                {
                    throw new WebSocketException(SR.GetString(SR.net_WebSockets_AcceptUnsupportedProtocol,
                        string.Join(", ", options.RequestedSubProtocols), subProtocol));
                }
            }

            return string.IsNullOrWhiteSpace(subProtocol) ? null : subProtocol; // May be null or valid.
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            return innerWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            return innerWebSocket.ReceiveAsync(buffer, cancellationToken);
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            return innerWebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription,
            CancellationToken cancellationToken)
        {
            ThrowIfNotConnected();
            return innerWebSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
        }

        public override void Abort()
        {
            if (state == disposed)
            {
                return;
            }
            if (innerWebSocket != null)
            {
                innerWebSocket.Abort();
            }
            Dispose();
        }

        private void AbortRequest(object obj)
        {
            HttpWebRequest request = (HttpWebRequest)obj;
            request.Abort();
        }

        public override void Dispose()
        {
            int priorState = Interlocked.Exchange(ref state, disposed);
            if (priorState == disposed)
            {
                // No cleanup required.
                return;
            }
            cts.Cancel(false);
            cts.Dispose();
            if (innerWebSocket != null)
            {
                innerWebSocket.Dispose();
            }
        }

        private void ThrowIfNotConnected()
        {
            if (state == disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            else if (state != connected)
            {
                throw new InvalidOperationException(SR.GetString(SR.net_WebSockets_NotConnected));
            }
        }
    }

    public sealed class ClientWebSocketOptions
    {
        private bool isReadOnly; // After ConnectAsync is called the options cannot be modified.
        private readonly IList<string> requestedSubProtocols;
        private readonly WebHeaderCollection requestHeaders;
        private TimeSpan keepAliveInterval;
        private int receiveBufferSize;
        private int sendBufferSize;
        private ArraySegment<byte>? buffer;
        private bool useDefaultCredentials;
        private ICredentials credentials;
        private IWebProxy proxy;
        private X509CertificateCollection clientCertificates;
        private CookieContainer cookies;

        internal ClientWebSocketOptions()
        {
            requestedSubProtocols = new List<string>();
            //requestHeaders = new WebHeaderCollection(WebHeaderCollectionType.HttpWebRequest);
            requestHeaders = Ext.CreateWebHeaderCollection();
            Proxy = WebRequest.DefaultWebProxy;
            receiveBufferSize = WebSocketHelpers.DefaultReceiveBufferSize;
            sendBufferSize = WebSocketHelpers.DefaultClientSendBufferSize;
            keepAliveInterval = WebSocket.DefaultKeepAliveInterval;
        }

#region HTTP Settings

        // Note that some headers are restricted like Host.
        public void SetRequestHeader(string headerName, string headerValue)
        {
            ThrowIfReadOnly();
            // WebHeadersColection performs the validation
            requestHeaders.Set(headerName, headerValue);
        }

        internal WebHeaderCollection RequestHeaders { get { return requestHeaders; } }

        public bool UseDefaultCredentials
        {
            get
            {
                return useDefaultCredentials;
            }
            set
            {
                ThrowIfReadOnly();
                useDefaultCredentials = value;
            }
        }

        public ICredentials Credentials
        {
            get
            {
                return credentials;
            }
            set
            {
                ThrowIfReadOnly();
                credentials = value;
            }
        }

        public IWebProxy Proxy
        {
            get
            {
                return proxy;
            }
            set
            {
                ThrowIfReadOnly();
                proxy = value;
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This collectin will be handed off directly to HttpWebRequest.")]
        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (clientCertificates == null)
                {
                    clientCertificates = new X509CertificateCollection();
                }
                return clientCertificates;
            }
            set
            {
                ThrowIfReadOnly();
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                clientCertificates = value;
            }
        }

        internal X509CertificateCollection InternalClientCertificates { get { return clientCertificates; } }

        public CookieContainer Cookies
        {
            get
            {
                return cookies;
            }
            set
            {
                ThrowIfReadOnly();
                cookies = value;
            }
        }

#endregion HTTP Settings

#region WebSocket Settings

        public void SetBuffer(int receiveBufferSize, int sendBufferSize)
        {
            ThrowIfReadOnly();
            WebSocketHelpers.ValidateBufferSizes(receiveBufferSize, sendBufferSize);

            this.buffer = null;
            this.receiveBufferSize = receiveBufferSize;
            this.sendBufferSize = sendBufferSize;
        }

        public void SetBuffer(int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer)
        {
            ThrowIfReadOnly();
            WebSocketHelpers.ValidateBufferSizes(receiveBufferSize, sendBufferSize);
            WebSocketHelpers.ValidateArraySegment(buffer, "buffer");
            WebSocketBuffer.Validate(buffer.Count, receiveBufferSize, sendBufferSize, false);

            this.receiveBufferSize = receiveBufferSize;
            this.sendBufferSize = sendBufferSize;
            this.buffer = buffer;
        }

        internal int ReceiveBufferSize { get { return receiveBufferSize; } }

        internal int SendBufferSize { get { return sendBufferSize; } }

        internal ArraySegment<byte> GetOrCreateBuffer()
        {
            if (!buffer.HasValue)
            {
                buffer = WebSocket.CreateClientBuffer(receiveBufferSize, sendBufferSize);
            }
            return buffer.Value;
        }

        public void AddSubProtocol(string subProtocol)
        {
            ThrowIfReadOnly();
            WebSocketHelpers.ValidateSubprotocol(subProtocol);
            // Duplicates not allowed.
            foreach (string item in requestedSubProtocols)
            {
                if (string.Equals(item, subProtocol, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(SR.GetString(SR.net_WebSockets_NoDuplicateProtocol, subProtocol),
                        "subProtocol");
                }
            }
            requestedSubProtocols.Add(subProtocol);
        }

        internal IList<string> RequestedSubProtocols { get { return requestedSubProtocols; } }

        public TimeSpan KeepAliveInterval
        {
            get
            {
                return keepAliveInterval;
            }
            set
            {
                ThrowIfReadOnly();
                if (value < Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentOutOfRangeException("value", value,
                        SR.GetString(SR.net_WebSockets_ArgumentOutOfRange_TooSmall,
                        Timeout.InfiniteTimeSpan.ToString()));
                }
                keepAliveInterval = value;
            }
        }

#endregion WebSocket settings

#region Helpers

        internal void SetToReadOnly()
        {
            Contract.Assert(!isReadOnly, "Already set");
            isReadOnly = true;
        }

        private void ThrowIfReadOnly()
        {
            if (isReadOnly)
            {
                throw new InvalidOperationException(SR.GetString(SR.net_WebSockets_AlreadyStarted));
            }
        }

#endregion Helpers
    }

#endif
}
