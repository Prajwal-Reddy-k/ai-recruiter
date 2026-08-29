namespace AIRecruiter.UnitTests.TestHelpers;

public class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;

    public FakeHttpClientFactory(HttpMessageHandler handler, string baseAddress)
    {
        _client = new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
    }

    public HttpClient CreateClient(string name) => _client;
}
