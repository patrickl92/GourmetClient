using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Net;
using System.Text;
using System.Threading;

namespace GourmetClient.Network
{
    using System.Security;

    public abstract class WebClientBase
    {
        private readonly object _loginLogoutLockObject = new object();
        private readonly SemaphoreSlim _clientCreationSemaphore = new SemaphoreSlim(1, 1);

        private HttpClient _client;

        private Task<bool> _loginTask;

        private Task _logoutTask;

        private int _loginCounter;

        public async Task<LoginHandle> Login(string userName, SecureString password)
        {
            userName = userName ?? throw new ArgumentNullException(nameof(userName));
            password = password ?? throw new ArgumentNullException(nameof(password));

            var loginSuccessful = await RequestLogin(userName, password);

            return new LoginHandle(loginSuccessful, OnLoginHandleReturned);
        }

        private async Task<bool> RequestLogin(string userName, SecureString password)
        {
            Task activeLogoutTask;

            lock (_loginLogoutLockObject)
            {
                activeLogoutTask = _logoutTask;
            }

            if (activeLogoutTask != null)
            {
                await activeLogoutTask;
            }

            Task<bool> loginTask;

            lock (_loginLogoutLockObject)
            {
                _loginCounter++;

                if (_loginTask == null)
                {
                    _loginTask = LoginImpl(userName, password);
                }

                loginTask = _loginTask;
            }

            return await loginTask;
        }

        private ValueTask OnLoginHandleReturned()
        {
            Task logoutTask;

            lock (_loginLogoutLockObject)
            {
                _loginCounter--;

                if (_loginCounter == 0)
                {
                    _loginTask = null;
                    _logoutTask = LogoutImpl();
                }

                logoutTask = _logoutTask;
            }

            if (logoutTask != null)
            {
                return new ValueTask(logoutTask);
            }

            return ValueTask.CompletedTask;
        }

        protected abstract Task<bool> LoginImpl(string username, SecureString password);

        protected abstract Task LogoutImpl();

        protected async Task<HttpResponseMessage> ExecuteGetRequest(string url, IReadOnlyDictionary<string, string> urlParameters = null)
        {
            var requestUrl = AppendParametersToUrl(url, urlParameters);
            HttpResponseMessage response;

            try
            {
                response = await ExecuteRequest(requestUrl, client => client.GetAsync(requestUrl));
            }
            catch (Exception exception)
            {
                throw new GourmetRequestException($"Error executing GET request", requestUrl, exception);
            }

            EnsureSuccessStatusCode(response);

            return response;
        }

        protected async Task<HttpResponseMessage> ExecutePostRequest(string url, IReadOnlyDictionary<string, string> urlParameters, IReadOnlyDictionary<string, string> formParameters)
        {
            var requestUrl = AppendParametersToUrl(url, urlParameters);
            var content = new FormUrlEncodedContent(formParameters);

            HttpResponseMessage response;

            try
            {
                response = await ExecuteRequest(requestUrl, client => client.PostAsync(requestUrl, content));
            }
            catch (Exception exception)
            {
                throw new GourmetRequestException("Error executing POST request", requestUrl, exception);
            }

            EnsureSuccessStatusCode(response);

            return response;
        }

        protected static async Task<string> GetResponseContent(HttpResponseMessage response)
        {
            try
            {
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception exception)
            {
                throw new GourmetRequestException("Error reading response content", GetRequestUriString(response), exception);
            }
        }

        protected static string GetRequestUriString(HttpResponseMessage response)
        {
            var requestMessage = response.RequestMessage;
            if (requestMessage == null)
            {
                return string.Empty;
            }

            return $"{requestMessage.Method} {requestMessage.RequestUri}";
        }

        private async Task<HttpResponseMessage> ExecuteRequest(string requestUrl, Func<HttpClient, Task<HttpResponseMessage>> requestFunc)
        {
            await _clientCreationSemaphore.WaitAsync();
            HttpClient client;

            try
            {
                if (_client == null)
                {
                    var result = await CreateHttpClient(requestUrl, requestFunc);
                    _client = result.Client;

                    return result.Response;
                }

                client = _client;
            }
            finally
            {
                _clientCreationSemaphore.Release();
            }

            return await requestFunc(client);
        }

        private static async Task<(HttpClient Client, HttpResponseMessage Response)> CreateHttpClient(string requestUrl, Func<HttpClient, Task<HttpResponseMessage>> requestFunc)
        {
            HttpClient client;
            HttpResponseMessage response;

            var requestUri = new Uri(requestUrl);
            var proxyUri = WebRequest.DefaultWebProxy?.GetProxy(requestUri);

            if (proxyUri == null || proxyUri.Authority == requestUri.Authority)
            {
                // No proxy required -> use default HttpClient
                client = new HttpClient();
                response = await requestFunc(client);
                return (client, response);
            }

            // Try executing request with default proxy (no authentication)
            var proxy = new WebProxy(proxyUri, true);
            client = new HttpClient(new HttpClientHandler { Proxy = proxy });

            try
            {
                response = await requestFunc(client);
                return (client, response);
            }
            catch (HttpRequestException exception)
            {
                client.Dispose();

                if (exception.StatusCode == HttpStatusCode.ProxyAuthenticationRequired)
                {
                    // Try executing request with default proxy and default credentials
                    client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true });
                }
                else
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

        private static string AppendParametersToUrl(string url, IReadOnlyDictionary<string, string> parameters)
        {
            if (parameters == null)
            {
                return url;
            }

            var stringBuilder = new StringBuilder($"{url}?");

            foreach (var parameter in parameters)
            {
                stringBuilder.Append($"{parameter.Key}={WebUtility.HtmlEncode(parameter.Value)}&");
            }

            stringBuilder.Length--;

            return stringBuilder.ToString();
        }

        private static void EnsureSuccessStatusCode(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new GourmetRequestException($"Server returned unexpected status code: {response.StatusCode}", GetRequestUriString(response));
            }
        }
    }
}
