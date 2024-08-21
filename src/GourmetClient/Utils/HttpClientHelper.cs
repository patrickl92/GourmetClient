using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System;
using System.Threading.Tasks;

namespace GourmetClient.Utils
{
    internal static class HttpClientHelper
    {
        public static async Task<(HttpClient Client, HttpResponseMessage Response)> CreateHttpClient(string requestUrl, Func<HttpClient, Task<HttpResponseMessage>> requestFunc, CookieContainer cookieContainer)
        {
            HttpClient client;
            HttpResponseMessage response;

            var proxy = GetProxy(requestUrl);
            if (proxy is null)
            {
                // No proxy required
                client = new HttpClient(new HttpClientHandler { UseProxy = false, CookieContainer = cookieContainer });
                response = await requestFunc(client);
                return (client, response);
            }

            // Try executing request with default proxy (no authentication)
            client = new HttpClient(new HttpClientHandler { Proxy = proxy, CookieContainer = cookieContainer });

            try
            {
                response = await requestFunc(client);
                return (client, response);
            }
            catch (HttpRequestException exception)
            {
                client.Dispose();
                client = null;

                if (IsProxyAuthenticationRequiredException(proxy, exception))
                {
                    // Try executing request with proxy and default credentials
                    proxy.Credentials = CredentialCache.DefaultCredentials;
                    client = new HttpClient(new HttpClientHandler { Proxy = proxy, CookieContainer = cookieContainer });
                }
                else if (IsProxyConnectionErrorException(proxy, exception))
                {
                    // Connection to proxy cannot be established. Try executing request without proxy
                    client = new HttpClient(new HttpClientHandler { UseProxy = false, CookieContainer = cookieContainer });
                }

                if (client is null)
                {
                    throw;
                }
            }

            try
            {
                response = await requestFunc(client);
                return (client, response);
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        public static WebProxy GetProxy(string requestUrl)
        {
            var requestUri = new Uri(requestUrl);
            var proxyUri = WebRequest.DefaultWebProxy?.GetProxy(requestUri);

            if (proxyUri == null || proxyUri.Authority == requestUri.Authority)
            {
                // No proxy required
                return null;
            }

            return new WebProxy(proxyUri, true);
        }

        public static bool IsProxyRelatedException(string requestUrl, HttpRequestException exception)
        {
            var proxy = GetProxy(requestUrl);
            if (proxy is null)
            {
                // No proxy required for request url
                return false;
            }

            return IsProxyAuthenticationRequiredException(proxy, exception) || IsProxyConnectionErrorException(proxy, exception);
        }

        public static bool IsProxyAuthenticationRequiredException(WebProxy proxy, HttpRequestException exception)
        {
            // Exception message is like "The proxy tunnel request to proxy '<proxyUri>' failed with status code '407'."
            return exception.StatusCode is null && exception.Message.Contains($"'{proxy.Address!.AbsoluteUri}'") && exception.Message.Contains("'407'");
        }

        public static bool IsProxyConnectionErrorException(WebProxy proxy, HttpRequestException exception)
        {
            // Exception message is like "The remote host (<proxy uri>) is unknown"
            return exception.StatusCode is null && exception.InnerException is SocketException && exception.Message.Contains(proxy.Address!.Authority);
        }
    }
}
