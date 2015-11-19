using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI.Common.Utils;
using EventStore.ClientAPI.SystemData;

namespace EventStore.ClientAPI.Transport.Http
{
    internal class HttpAsyncClient
    {
        private static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        static HttpAsyncClient()
        {
            // TODO does this have an equivalent for HTTPClient ??
            // see http://blogs.msdn.com/b/jpsanders/archive/2009/05/20/understanding-maxservicepointidletime-and-defaultconnectionlimit.aspx
            // According to the .NET Portability analyzer as of 2015-11-19, this functionality does not exist in .NET Core.
            // However, according to a StackOverflow User, the default value for this limit was recently increased to Max Int.
            // http://stackoverflow.com/questions/5488235/system-net-servicepointmanager-defaultconnectionlimit-24-bug 
            //ServicePointManager.MaxServicePointIdleTime = 10000;
            //ServicePointManager.DefaultConnectionLimit = 800;
        }

        private readonly ILogger _log;
        private readonly HttpClient _httpClient;

        public HttpAsyncClient(ILogger log, TimeSpan timeout)
        {
            Ensure.NotNull(log, "log");
            _log = log;

            _httpClient = new HttpClient();
            _httpClient.Timeout = timeout;
            // TODO add Accept header config??
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void Get(string url, UserCredentials userCredentials, 
                        Action<HttpResponse> onSuccess, Action<Exception> onException,
                        string hostHeader = "")
        {
            Ensure.NotNull(url, "url");
            Ensure.NotNull(onSuccess, "onSuccess");
            Ensure.NotNull(onException, "onException");

            Receive(System.Net.Http.HttpMethod.Get, url, userCredentials, onSuccess, onException, hostHeader);
        }

        public void Post(string url, string body, string contentType, UserCredentials userCredentials,
                         Action<HttpResponse> onSuccess, Action<Exception> onException)
        {
            Ensure.NotNull(url, "url");
            Ensure.NotNull(body, "body");
            Ensure.NotNull(contentType, "contentType");
            Ensure.NotNull(onSuccess, "onSuccess");
            Ensure.NotNull(onException, "onException");

            Send(System.Net.Http.HttpMethod.Post, url, body, contentType, userCredentials, onSuccess, onException);
        }

        public void Delete(string url, UserCredentials userCredentials, 
                           Action<HttpResponse> onSuccess, Action<Exception> onException)
        {
            Ensure.NotNull(url, "url");
            Ensure.NotNull(onSuccess, "onSuccess");
            Ensure.NotNull(onException, "onException");

            Receive(System.Net.Http.HttpMethod.Delete, url, userCredentials, onSuccess, onException);
        }

        public void Put(string url, string body, string contentType, UserCredentials userCredentials,
                        Action<HttpResponse> onSuccess, Action<Exception> onException)
        {
            Ensure.NotNull(url, "url");
            Ensure.NotNull(body, "body");
            Ensure.NotNull(contentType, "contentType");
            Ensure.NotNull(onSuccess, "onSuccess");
            Ensure.NotNull(onException, "onException");

            Send(System.Net.Http.HttpMethod.Put, url, body, contentType, userCredentials, onSuccess, onException);
        }

        private async void Receive(System.Net.Http.HttpMethod method, string url, UserCredentials userCredentials,
                             Action<HttpResponse> onSuccess, Action<Exception> onException, string hostHeader = "")
        {
            var requestMessage = new HttpRequestMessage()
            {
                RequestUri = new Uri(url),
                Method = method,
            };

            if (userCredentials != null)
                AddAuthenticationHeader(requestMessage, userCredentials);

            if (!string.IsNullOrWhiteSpace(hostHeader))
                requestMessage.Headers.Add("Host", hostHeader);

            HttpResponseMessage httpResponseMessage;
            try
            {
                httpResponseMessage = await _httpClient.SendAsync(requestMessage);
            }
            catch (Exception ex)
            {
                _log.Debug("Error inside httpClient.SendAsync(...)", ex);
                onException(ex);
                return;
            }

            HttpResponse httpResponse = new HttpResponse(httpResponseMessage);
            httpResponse.Body = await DecodeResponseString(httpResponseMessage, UTF8NoBom);

            onSuccess(httpResponse);
        }

        private async void Send(System.Net.Http.HttpMethod method, string url, string body, string contentType, UserCredentials userCredentials,
                          Action<HttpResponse> onSuccess, Action<Exception> onException)
        {
            HttpResponseMessage httpResponseMessage;

            using (var streamContent = new StreamContent(EncodeStringToStream(body, UTF8NoBom)))
            {
                var requestMessage = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = method,
                    Content = streamContent
                };

                if (userCredentials != null)
                    AddAuthenticationHeader(requestMessage, userCredentials);

                if (!string.IsNullOrWhiteSpace(contentType))
                    requestMessage.Headers.Add("Content-Type", contentType);

                try
                {
                    httpResponseMessage = await _httpClient.SendAsync(requestMessage);
                }
                catch (Exception ex)
                {
                    _log.Debug("Error inside httpClient.SendAsync(...)", ex);
                    onException(ex);
                    return;
                }
            }

            HttpResponse httpResponse = new HttpResponse(httpResponseMessage);
            httpResponse.Body = await DecodeResponseString(httpResponseMessage, UTF8NoBom);

            onSuccess(httpResponse);
        }

        private void AddAuthenticationHeader(HttpRequestMessage request, UserCredentials userCredentials)
        {
            Ensure.NotNull(userCredentials, "userCredentials");
            var httpAuthentication = string.Format("{0}:{1}", userCredentials.Username, userCredentials.Password);
            var encodedCredentials = Convert.ToBase64String(Helper.UTF8NoBom.GetBytes(httpAuthentication));
            request.Headers.Add("Authorization", string.Format("Basic {0}", encodedCredentials));
        }

        //private Stream 
        private Stream EncodeStringToStream(string content, Encoding encoding)
        {
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, encoding))
            {
                writer.Write(content);
                writer.Flush();
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private async Task<string> DecodeResponseString(HttpResponseMessage message, Encoding encoding)
        {

            using (var responseStream = await message.Content.ReadAsStreamAsync())
            {
                using (var responseStreamReader = new StreamReader(responseStream, encoding))
                {
                    return await responseStreamReader.ReadToEndAsync();
                }
            }
        }
    }
}