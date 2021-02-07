/// <summary>
/// Copyright 2019. Bloomberg Finance L.P. Permission is hereby granted, free of
/// charge, to any person obtaining a copy of this software and associated
/// documentation files (the "Software"), to deal in the Software without
/// restriction, including without limitation the rights to use, copy, modify,
/// merge, publish, distribute, sublicense, and/or sell copies of the Software,
/// and to permit persons to whom the Software is furnished to do so, subject to
/// the following conditions: The above copyright notice and this permission
/// notice shall be included in all copies or substantial portions of the
/// Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE.
/// </summary>

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// This file was originally copied from the Bloomberg HAPI Samples
// S:\Technology\Development\Bloomberg\HAPI - SampleConnect\BeapAuth.cs

namespace Bloomberg.API.HAPI.Authentication
{
    /// <summary>
    /// This class serves to inject JWT tokens in each outgoing HTTP request.
    /// It uses standard "HttpClient Message Handlers" engine provided by 
    /// HTTPClient component to embed into HTTPClient HTTP handling subsystem. 
    /// (https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/httpclient-message-handlers)
    /// </summary>
    public class BloombergAuthenticationHandler : DelegatingHandler
    {
        private readonly string dApiVersion;
        private readonly Credential dTokenMaker;
        private const uint DMaxRedirects = 3;
        private const string DMediaTypeSseEvents = "text/event-stream";

        private readonly ILogger logger;

        /// <summary>
        /// Constructs new JWT signing handler and initializes default HttpClientHandler inner HTTP handler 
        /// which is used to delegate the rest of standard HTTP messaging behaviour to the regular
        /// component. 
        /// Note: The only unsutisfactory thing in "HttpClientHandler" behaviour is handling redirecitons 
        /// because in case of HTTP redirection it will follow redirection without generating new JWT token 
        /// for each redirected request and receive authorization faulure errors from BEAP service for that 
        /// reason.
        /// So this class disables the HttpClientHandler's redirection handling behaviour in the following 
        /// consructor and handles HTTP redirections by itself in SendAsync method.
        /// This constructor will enable all logging of this unit to console.
        /// To customize the behaviour of logging an alternate constructor should be used(using
        /// prepared ILogger<BEAPHandler> instance, customized with LoggerFactory).
        /// For example, to disable logging the following code can be used:
        /// var logging = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.None));
        /// var httpHandler = new BEAPHandler(logger: logging.CreateLogger<BEAPHandler>());
        /// </summary>
        /// <param name="apiVersion"> The Hypermedia API version. </param>
        /// <param name="tokenMaker"> JWT token maker instance responsible for JWT generation. </param>
        /// <param name="logger"> An object to be used to log messages. </param>
        public BloombergAuthenticationHandler(ILogger logger, string apiVersion = "2", Credential tokenMaker = null)
        {
            var defaultHandler = new HttpClientHandler();
            // Important: the following line disables default redirection 
            // hadler because the default handler will not update JWT on 
            // each redirection, JWTSigningHandler will do it in 
            // 'SendAsync' method.
            defaultHandler.AllowAutoRedirect = false;
            InnerHandler = defaultHandler;
            this.logger = logger;
            dApiVersion = apiVersion;
            dTokenMaker = tokenMaker ?? Credential.LoadCredential();
        }

        /// <summary>
        /// Determine whether provided response has one of the redirection status code.
        /// </summary>
        /// <param name="response"> HTTP response to determine redirection status for. </param>
        /// <returns> True if this is redirect response, false otherwise </returns>
        private static bool IsRedirect(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.MultipleChoices:
                case HttpStatusCode.Moved:
                case HttpStatusCode.Found:
                case HttpStatusCode.SeeOther:
                case HttpStatusCode.TemporaryRedirect: 
                // case HttpStatusCode.PermanentRedirect: NOT IN Standard2.0
                    return true;
                default:
                    if (((int) response.StatusCode) == 308) return true;
                    return false;
            }
        }
        /// <summary>
        /// Convert HTTP method of redirected response in accord with HTTP standard.
        /// </summary>
        /// <param name="response"> Response containing redirection status. </param>
        /// <param name="request"> Original HTTP request. </param>
        private static void ConvertForRedirect(HttpResponseMessage response, HttpRequestMessage request)
        {
            if (request.Method == HttpMethod.Post)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.MultipleChoices:
                    case HttpStatusCode.Moved:
                    case HttpStatusCode.Found:
                    case HttpStatusCode.SeeOther:
                        request.Content = null;
                        request.Headers.TransferEncodingChunked = false;
                        request.Method = HttpMethod.Get;
                        break;
                }
            }
        }
        /// <summary>
        /// Set new JWT token to outgoing request.
        /// </summary>
        /// <param name="request"> The HTTP request message to send to the server. </param>
        private void AssignBeapToken(HttpRequestMessage request)
        {
            var accessToken = dTokenMaker.CreateToken(
                request.RequestUri.Host,
                request.RequestUri.LocalPath,
                request.Method.ToString()
                );

            request.Headers.Remove("JWT");
            request.Headers.Add("JWT", accessToken);
        }
        /// <summary>
        /// Add JWT token to request and reflect HTTP redirection responses, if any.
        /// </summary>
        /// <param name="request"> The HTTP request message to send to the server. </param>
        /// <param name="cancellationToken"> A cancellation token to cancel operation. </param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            System.Threading.CancellationToken cancellationToken)
        {
            AssignBeapToken(request);
            request.Headers.Add("api-version", dApiVersion);

            logger.LogDebug("Request being sent to HTTP server:\n{request}.\n\n", request);

            var response = await base.SendAsync(request, cancellationToken);
            uint redirectCount = 0;
            try
            {
                while (IsRedirect(response) && redirectCount++ < DMaxRedirects)
                {
                    Uri redirectUri;
                    redirectUri = response.Headers.Location;
                    if (!redirectUri.IsAbsoluteUri)
                    {
                        redirectUri = new Uri(request.RequestUri, response.Headers.Location);
                    }

                    logger.LogDebug("Redirecting to {redirectUri}.", redirectUri);

                    request.RequestUri = redirectUri;
                    ConvertForRedirect(response, request);
                    response.Dispose();

                    AssignBeapToken(request);
                    response = await base.SendAsync(request, cancellationToken);
                }
            }
            catch (Exception)
            {
                response.Dispose();
                throw;
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.Forbidden:
                    logger.LogCritical("Either supplied credentials are invalid or expired, or the requesting IP is address is not whitelisted");
                    break;
            }

            logger.LogDebug("{request.Method} {request.RequestUri} [{response.StatusCode}]", request.Method, request.RequestUri, response.StatusCode);
            // If it's not a file download request - print the response contents.
            if (response.Content.Headers.ContentDisposition == null &&
                response.Content.Headers.ContentType.MediaType != DMediaTypeSseEvents)
            {
                var content = await response.Content.ReadAsStringAsync();
                logger.LogDebug("Response content: {content}", content);
            }
            return response;
        }
    }
}
