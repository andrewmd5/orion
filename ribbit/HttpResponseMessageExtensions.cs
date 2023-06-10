using System.Net;
namespace Ribbit;
public static class HttpResponseMessageExtensions {
    public static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response) {
        if (response.IsSuccessStatusCode) {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();

        response.Content?.Dispose();

        throw new SimpleHttpResponseException(response.StatusCode, content, response.RequestMessage.RequestUri.ToString());
    }
}

public class SimpleHttpResponseException : Exception {
    public readonly string Content;
    public readonly HttpStatusCode StatusCode;
    public readonly string Url;

    public override string ToString() {
        return $"Called: {Url} \n Status Code: {StatusCode} \n Response: {Content}";
    }

    public SimpleHttpResponseException(HttpStatusCode statusCode, string content, string url) : base($"{url} \n {content}") {
        Content = content;
        Url = url;
        StatusCode = statusCode;
    }
}