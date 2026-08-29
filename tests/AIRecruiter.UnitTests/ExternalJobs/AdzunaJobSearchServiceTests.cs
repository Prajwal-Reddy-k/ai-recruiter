using System.Net;
using AIRecruiter.Application.DTOs.ExternalJobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Infrastructure.ExternalJobs;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.UnitTests.TestHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AIRecruiter.UnitTests.ExternalJobs;

public class AdzunaJobSearchServiceTests
{
    private const string SampleJson = """
    {
      "count": 1,
      "results": [
        { "title": "Backend Engineer", "company": { "display_name": "Acme" }, "location": { "display_name": "London" }, "description": "desc", "redirect_url": "https://example.com/job/1" }
      ]
    }
    """;

    private static AdzunaJobSearchService CreateSut(FakeHttpMessageHandler handler, IMemoryCache? cache = null)
    {
        var factory = new FakeHttpClientFactory(handler, "https://api.adzuna.com/");
        var options = Options.Create(new AdzunaOptions { AppId = "id", AppKey = "key", Country = "gb" });
        cache ??= new MemoryCache(new MemoryCacheOptions());
        return new AdzunaJobSearchService(factory, options, cache, NullLogger<AdzunaJobSearchService>.Instance);
    }

    [Fact]
    public async Task SearchAsync_ValidResponse_ReturnsMappedListings()
    {
        var handler = FakeHttpMessageHandler.ReturningJson(SampleJson);
        var sut = CreateSut(handler);

        var result = await sut.SearchAsync(new ExternalJobSearchRequest("backend", "London", 1, 10));

        var item = Assert.Single(result.Items);
        Assert.Equal("Backend Engineer", item.Title);
        Assert.Equal("Adzuna", item.Source);
    }

    [Fact]
    public async Task SearchAsync_SecondIdenticalCallWithin24h_UsesCacheAndSkipsHttp()
    {
        var handler = FakeHttpMessageHandler.ReturningJson(SampleJson);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(handler, cache);
        var request = new ExternalJobSearchRequest("backend", "London", 1, 10);

        await sut.SearchAsync(request);
        await sut.SearchAsync(request);

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task SearchAsync_TooManyRequests_ThrowsServiceUnavailableWithRetryAfter()
    {
        var handler = new FakeHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        });
        var sut = CreateSut(handler);

        var ex = await Assert.ThrowsAsync<ExternalServiceUnavailableException>(
            () => sut.SearchAsync(new ExternalJobSearchRequest("backend", "London", 1, 10)));

        Assert.Equal(30, ex.RetryAfterSeconds);
    }

    [Fact]
    public void IsAvailable_MissingCredentials_ReportsUnavailable()
    {
        var handler = FakeHttpMessageHandler.ReturningJson(SampleJson);
        var factory = new FakeHttpClientFactory(handler, "https://api.adzuna.com/");
        var options = Options.Create(new AdzunaOptions { AppId = "", AppKey = "" });
        var sut = new AdzunaJobSearchService(factory, options, new MemoryCache(new MemoryCacheOptions()), NullLogger<AdzunaJobSearchService>.Instance);

        Assert.False(sut.IsAvailable);
    }

    [Fact]
    public async Task SearchAsync_MissingCredentials_ThrowsServiceUnavailable()
    {
        var handler = FakeHttpMessageHandler.ReturningJson(SampleJson);
        var factory = new FakeHttpClientFactory(handler, "https://api.adzuna.com/");
        var options = Options.Create(new AdzunaOptions { AppId = "", AppKey = "" });
        var sut = new AdzunaJobSearchService(factory, options, new MemoryCache(new MemoryCacheOptions()), NullLogger<AdzunaJobSearchService>.Instance);

        await Assert.ThrowsAsync<ExternalServiceUnavailableException>(
            () => sut.SearchAsync(new ExternalJobSearchRequest("backend", "London", 1, 10)));
    }

    [Fact]
    public async Task DisabledExternalJobSearchService_AlwaysReportsUnavailable()
    {
        var sut = new DisabledExternalJobSearchService();

        Assert.False(sut.IsAvailable);
        await Assert.ThrowsAsync<ExternalServiceUnavailableException>(
            () => sut.SearchAsync(new ExternalJobSearchRequest(null, null, 1, 10)));
    }
}
