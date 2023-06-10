using System.Collections;
using System.Net;
using System.Reflection;
namespace Ribbit;
public class HttpBuddy : IDisposable {
    private static readonly string _defaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.88 Safari/537.36 Edg/79.0.309.54";

    private readonly HttpClient _httpClient;
    private readonly HttpClientHandler _httpHandler;

    public HttpBuddy() {
        _httpHandler = new HttpClientHandler {
            UseCookies = true,
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(_httpHandler);
    }

    public void Dispose() {
        _httpHandler?.Dispose();
        _httpClient?.Dispose();
    }

    /// <summary>
    /// Get's all the cookies from an HTTP client
    /// </summary>
    /// <returns></returns>
    public List<Cookie> GetAllCookies() {
        var domainTableField = _httpHandler.CookieContainer.GetType().GetRuntimeFields().FirstOrDefault(x => x.Name == "m_domainTable");
        if (domainTableField == null) return new List<Cookie>();
        {
            var domains = (IDictionary)domainTableField.GetValue(_httpHandler.CookieContainer);

            return (from object val in domains.Values
                    let type = val.GetType().GetRuntimeFields().First(x => x.Name == "m_list")
                    select (IDictionary)type.GetValue(val)
                into values
                    from CookieCollection cookies in values.Values
                    from Cookie cookie in cookies
                    select cookie).ToList();
        }
    }
    /// <summary>
    /// Fetches a string via a GET request
    /// </summary>
    /// <param name="url"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public async Task<ReadOnlyMemory<byte>> GetBytes(string url, Dictionary<string, string> headers = null) {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
        request.Headers.Accept.TryParseAdd("application/json, text/plain, */*");
        if (headers != null) {
            if (!headers.ContainsKey("User-Agent"))
                request.Headers.TryAddWithoutValidation("User-Agent", _defaultUserAgent);
            foreach (var header in headers) request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }


        var cookies = _httpHandler.CookieContainer.GetCookieHeader(new Uri(url));
        if (!string.IsNullOrEmpty(cookies))
            request.Headers.Add("Cookie", cookies);
        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        await response.EnsureSuccessStatusCodeAsync();
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }


    /// <summary>
    /// Fetches a string via a GET request
    /// </summary>
    /// <param name="url"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public async Task<string> GetContent(string url, Dictionary<string, string> headers = null) {

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
        request.Headers.Accept.TryParseAdd("application/json, text/plain, */*");
        if (headers != null) {
            if (!headers.ContainsKey("User-Agent"))
                request.Headers.TryAddWithoutValidation("User-Agent", _defaultUserAgent);
            foreach (var header in headers) request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var cookies = _httpHandler.CookieContainer.GetCookieHeader(new Uri(url));
        if (!string.IsNullOrEmpty(cookies))
            request.Headers.Add("Cookie", cookies);
        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        await response.EnsureSuccessStatusCodeAsync();
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// POST a JSON body to a remote endpoint
    /// </summary>
    /// <param name="url"></param>
    /// <param name="content"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public async Task<string> PostBody(string url, StringContent content,
        Dictionary<string, string> headers = null) {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url)) {
            Content = content
        };
        request.Headers.Accept.TryParseAdd("application/json, text/plain, */*");
        if (headers != null) {
            if (!headers.ContainsKey("User-Agent"))
                request.Headers.TryAddWithoutValidation("User-Agent", _defaultUserAgent);
            foreach (var header in headers) request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var cookies = _httpHandler.CookieContainer.GetCookieHeader(new Uri(url));
        if (!string.IsNullOrEmpty(cookies))
            request.Headers.Add("Cookie", cookies);
        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        await response.EnsureSuccessStatusCodeAsync();
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Post an HTTP form
    /// </summary>
    /// <param name="url"></param>
    /// <param name="content"></param>
    /// <param name="headers"></param>
    /// <returns></returns>
    public async Task<string> PostForm(string url, Dictionary<string, string> content,
        Dictionary<string, string> headers = null) {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(url)) {
            Content = new FormUrlEncodedContent(content)
        };
        request.Headers.Accept.TryParseAdd("application/json, text/plain, */*");
        if (headers != null) {
            if (!headers.ContainsKey("User-Agent"))
                request.Headers.TryAddWithoutValidation("User-Agent", _defaultUserAgent);
            foreach (var header in headers) request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var cookies = _httpHandler.CookieContainer.GetCookieHeader(new Uri(url));
        if (!string.IsNullOrEmpty(cookies))
            request.Headers.Add("Cookie", cookies);
        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        await response.EnsureSuccessStatusCodeAsync();
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}