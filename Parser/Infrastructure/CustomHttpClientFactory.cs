using System.Net.Http;

namespace Parser.Infrastructure
{
    public static class CustomHttpClientFactory
    {
        public static HttpClient CreateClient() => new HttpClient();
        public static HttpClient CreateClient(HttpClientHandler handler) => new HttpClient(handler);
    }
}
