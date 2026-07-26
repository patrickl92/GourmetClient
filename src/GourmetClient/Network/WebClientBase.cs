using GourmetClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GourmetClient.Network;

public abstract class WebClientBase
{
    private readonly object _loginLogoutLockObject;
    private readonly SemaphoreSlim _clientCreationSemaphore;
    private readonly CookieContainer _cookieContainer;

    private HttpClient? _client;
    private Task<bool>? _loginTask;
    private Task? _logoutTask;
    private int _loginCounter;

    protected WebClientBase()
    {
        _loginLogoutLockObject = new object();
        _clientCreationSemaphore = new SemaphoreSlim(1, 1);
        _cookieContainer = new CookieContainer();
    }

    public async Task<LoginHandle> Login(string userName, string password)
    {
        bool loginSuccessful = await RequestLogin(userName, password);

        return new LoginHandle(loginSuccessful, OnLoginHandleReturned);
    }

    private async Task<bool> RequestLogin(string userName, string password)
    {
        Task? activeLogoutTask;

        lock (_loginLogoutLockObject)
        {
            activeLogoutTask = _logoutTask;
        }

        if (activeLogoutTask is not null)
        {
            await activeLogoutTask;
        }

        Task<bool> loginTask;

        lock (_loginLogoutLockObject)
        {
            _loginCounter++;
            _loginTask ??= LoginImpl(userName, password);
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
        Task? logoutTask;

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

        if (logoutTask is not null)
        {
            return new ValueTask(logoutTask);
        }

        return ValueTask.CompletedTask;
    }

    protected abstract Task<bool> LoginImpl(string username, string password);

    protected abstract Task LogoutImpl();

    protected async Task<HttpResponseMessage> ExecuteGetRequest(string url, IReadOnlyDictionary<string, string>? urlParameters = null)
    {
        string requestUrl = AppendParametersToUrl(url, urlParameters);

        HttpResponseMessage response;
        try
        {
            response = await ExecuteRequest(requestUrl, client => client.GetAsync(requestUrl));
        }
        catch (HttpRequestException exception)
        {
            throw new GourmetRequestException("GET request failed", requestUrl, exception);
        }
        catch (OperationCanceledException exception)
        {
            throw new GourmetRequestException("GET request was cancelled", requestUrl, exception);
        }

        EnsureSuccessStatusCode(response);

        return response;
    }

    protected async Task<HttpResponseMessage> ExecuteFormPostRequest(string url, IReadOnlyDictionary<string, string> formParameters)
    {
        var content = new FormUrlEncodedContent(formParameters);

        HttpResponseMessage response;
        try
        {
            response = await ExecuteRequest(url, client => client.PostAsync(url, content));
        }
        catch (HttpRequestException exception)
        {
            throw new GourmetRequestException("POST request failed", url, exception);
        }
        catch (OperationCanceledException exception)
        {
            throw new GourmetRequestException("POST request was cancelled", url, exception);
        }

        EnsureSuccessStatusCode(response);

        return response;
    }

    protected async Task<HttpResponseMessage> ExecuteJsonPostRequest(string url, object payload)
    {
        HttpResponseMessage response;
        try
        {
            response = await ExecuteRequest(url, client => client.PostAsJsonAsync(url, payload));
        }
        catch (HttpRequestException exception)
        {
            throw new GourmetRequestException("POST request failed", url, exception);
        }
        catch (OperationCanceledException exception)
        {
            throw new GourmetRequestException("POST request was cancelled", url, exception);
        }

        EnsureSuccessStatusCode(response);

        return response;
    }

    protected static async Task<string> ReadResponseContent(HttpResponseMessage response)
    {
        try
        {
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException exception)
        {
            throw new GourmetRequestException("Error reading response content", GetRequestUriString(response), exception);
        }
    }

    protected static async Task<T> ParseJsonResponseObject<T>(HttpResponseMessage response)
    {
        string jsonResponseContent = await ReadResponseContent(response);

        T? result;
        try
        {
            result = JsonSerializer.Deserialize<T>(jsonResponseContent);
        }
        catch (JsonException exception)
        {
            throw new GourmetParseException("Error parsing response content as JSON", GetRequestUriString(response), jsonResponseContent, exception);
        }

        if (result is null)
        {
            throw new GourmetParseException("Invalid JSON content", GetRequestUriString(response), jsonResponseContent);
        }

        return result;
    }

    protected static string GetRequestUriString(HttpResponseMessage response)
    {
        HttpRequestMessage? requestMessage = response.RequestMessage;
        if (requestMessage is null)
        {
            return string.Empty;
        }

        return $"{requestMessage.Method} {requestMessage.RequestUri}";
    }

    private async Task<HttpResponseMessage> ExecuteRequest(string requestUrl, Func<HttpClient, Task<HttpResponseMessage>> requestFunc)
    {
        HttpClientResult<HttpResponseMessage?> clientResult = await GetOrCreateClient();

        if (clientResult.ResponseResult is not null)
        {
            // Request was executed while creating the HttpClient
            return clientResult.ResponseResult;
        }

        try
        {
            return await requestFunc(clientResult.Client);
        }
        catch (HttpRequestException exception)
        {
            bool isNetworkError = exception.StatusCode is null && exception.InnerException is IOException;

            if (isNetworkError || HttpClientHelper.IsProxyRelatedException(requestUrl, exception))
            {
                // A network error occurred, or the connection with the proxy no longer works
                // Try to recreate the HttpClient and execute the request again
                clientResult = await GetOrCreateClient(true);

                if (clientResult.ResponseResult is not null)
                {
                    // Client was recreated successfully
                    return clientResult.ResponseResult;
                }
            }

            throw;
        }

        async Task<HttpClientResult<HttpResponseMessage?>> GetOrCreateClient(bool resetClient = false)
        {
            await _clientCreationSemaphore.WaitAsync();

            try
            {
                if (_client is null || resetClient)
                {
                    _client?.Dispose();
                    _client = null;

                    HttpClientResult<HttpResponseMessage> result =
                        await HttpClientHelper.CreateHttpClient(requestUrl, requestFunc, _cookieContainer);

                    _client = result.Client;

                    return new HttpClientResult<HttpResponseMessage?>(result.Client, result.ResponseResult);
                }

                return new HttpClientResult<HttpResponseMessage?>(_client, null);
            }
            finally
            {
                _clientCreationSemaphore.Release();
            }
        }
    }

    private static string AppendParametersToUrl(string url, IReadOnlyDictionary<string, string>? parameters)
    {
        if (parameters is null)
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
            throw new GourmetRequestException(
                $"Server returned unexpected status code: {response.StatusCode}", GetRequestUriString(response));
        }
    }
}