using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using GourmetClient.Utils;

namespace GourmetClient.Network
{
    using System.Security;

    public abstract class WebClientBase
    {
        private readonly object _loginLogoutLockObject = new object();
        private readonly SemaphoreSlim _clientCreationSemaphore = new SemaphoreSlim(1, 1);

        private readonly CookieContainer _cookieContainer = new CookieContainer();

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

            try
            {
                return await loginTask;
            }
            catch (Exception)
            {
                lock (_loginLogoutLockObject)
                {
                    _loginCounter--;

                    if (_loginCounter == 0)
                    {
                        _loginTask = null;
                    }
                }

                throw;
            }
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
            var (client, response) = await GetClient();

            if (response is not null)
            {
                // Request was executed while creating the HttpClient
                return response;
            }

            try
            {
                return await requestFunc(client);
            }
            catch (HttpRequestException exception)
            {
                var isNetworkError = exception.StatusCode is null && exception.InnerException is IOException;

                if (isNetworkError || HttpClientHelper.IsProxyRelatedException(requestUrl, exception))
                {
                    // A network error occurred, or the connection with the proxy no longer works
                    // Try to recreate the HttpClient and execute the request again
                    var (_, responseAfterReset) = await GetClient(true);

                    if (responseAfterReset is not null)
                    {
                        return responseAfterReset;
                    }
                }

                throw;
            }

            async Task<(HttpClient Client, HttpResponseMessage Response)> GetClient(bool resetClient = false)
            {
                await _clientCreationSemaphore.WaitAsync();

                try
                {
                    if (_client == null || resetClient)
                    {
                        _client?.Dispose();
                        _client = null;

                        var result = await HttpClientHelper.CreateHttpClient(requestUrl, requestFunc, _cookieContainer);
                        _client = result.Client;

                        return (_client, result.RequestResult);
                    }

                    return (_client, null);
                }
                finally
                {
                    _clientCreationSemaphore.Release();
                }
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
