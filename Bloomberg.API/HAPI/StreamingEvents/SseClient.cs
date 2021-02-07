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
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Bloomberg.API.HAPI.Model;

namespace Bloomberg.API.HAPI.StreamingEvents
{
    /// <summary>
    /// This class serves as a HTTP client capable of receiving events from a server.
    /// </summary>
    public class SseClient : IDisposable
    {
        private const string DHeaderNameAccept = "Accept";
        private const string DHeaderNameCacheControl = "Cache-Control";
        private const string DHeaderNameContentType = "Content-Type";
        private const string DHeaderNameLastEventId = "Last-Event-ID";
        private const string DHeaderValueCacheControl = "no-cache";
        private const string DHeaderValueContentType = "text/event-stream";
        private const int DMaxConnectAttempts = 3;

        private readonly Uri dUri;
        private readonly HttpClient dSession;
        private int dRetryTimeoutMs = 3000;
        private HttpResponseMessage dResponse;
        private SseStreamParser dParser;
        private bool isConnected = false;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="uri">A URI to send the initial request.</param>
        /// <param name="session">An HTTP client session.</param>
        public SseClient(Uri uri, HttpClient session)
        {
            dUri = uri;
            dSession = session;

            dSession.DefaultRequestHeaders.Add(DHeaderNameAccept, DHeaderValueContentType);
            dSession.DefaultRequestHeaders.Add(DHeaderNameCacheControl, DHeaderValueCacheControl);
        }

        /// <summary>
        /// Tries to establish a connection by sending a HTTP GET request.
        /// </summary>
        public async Task Connect()
        {
            if (isConnected) return;

            for (var i = 0; i < DMaxConnectAttempts; ++i)
            {
                try
                {
                    dResponse?.Dispose();
                    dResponse = await dSession.GetAsync(dUri, HttpCompletionOption.ResponseHeadersRead);
                    dResponse.EnsureSuccessStatusCode();
                    var data = await dResponse.Content.ReadAsStreamAsync();
                    var reader = new StreamReader(data);
                    dParser?.Dispose();
                    dParser = new SseStreamParser(reader, dUri.ToString());
                    isConnected = true;
                    return;
                }
                catch (HttpRequestException error)
                {
                    if (i + 1 == DMaxConnectAttempts)
                    {
                        throw;
                    }

                    Console.Error.WriteLine(error.Message);
                }
            }
        }

        public bool IsConnected => isConnected;

        /// <summary>
        /// Reads an event from the event stream.
        /// </summary>
        /// <returns>The next event.</returns>
        public async Task<SseEvent> ReadEvent()
        {
            for (var i = 0; i < DMaxConnectAttempts; ++i)
            {
                try
                {
                    var nextEvent = await dParser.ReadEvent();
                    if (!string.IsNullOrEmpty(nextEvent.Id))
                    {
                        dSession.DefaultRequestHeaders.Remove(DHeaderNameLastEventId);
                        dSession.DefaultRequestHeaders.Add(DHeaderNameLastEventId, nextEvent.Id);
                    }
                    dRetryTimeoutMs = nextEvent.Retry ?? dRetryTimeoutMs;
                    return nextEvent;
                }
                catch (Exception error)
                {
                    if (i + 1 == DMaxConnectAttempts)
                    {
                        throw;
                    }

                    Console.Error.WriteLine($"Error when reading event from the SSE server: {error.Message}");
                    dSession.CancelPendingRequests();
                    isConnected = false;
                    await Task.Delay(dRetryTimeoutMs);
                    await Connect();
                }
            }
            throw new Exception("too many failed attempts to read an event");
        }

        /// <summary>
        /// Releases all the underlying resources.
        /// </summary>
        public void Dispose()
        {
            dParser?.Dispose();
            dResponse?.Dispose();
            dSession.Dispose();
        }
    }
}
