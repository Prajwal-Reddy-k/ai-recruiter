using System.Net;
using AIRecruiter.Infrastructure.Locations;
using AIRecruiter.UnitTests.TestHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIRecruiter.UnitTests.Locations;

public class NominatimLocationSearchServiceTests
{
    private const string SampleJson = """
    [
      { "display_name": "London, UK", "lat": "51.5072", "lon": "-0.1276" }
    ]
    """;

    private static NominatimLocationSearchService CreateSut(FakeHttpMessageHandler handler, IMemoryCache? cache = null)
    {
        var factory = new FakeHttpClientFactory(handler, "https://nominatim.openstreetmap.org/");
        cache ??= new MemoryCache(new MemoryCacheOptions());
        var rateGate = new NominatimRateGate();
        return new NominatimLocationSearchService(factory, cache, rateGate, NullLogger<NominatimLocationSearchService>.Instance);
    }

    [Fact]
    public async Task SearchAsync_QueryTooShort_ReturnsEmptyWithoutCallingHttp()
    {
        var handler = FakeHttpMessageHandler.ReturningJson(SampleJson);
        var sut = CreateSut(handler);

        var results = await sut.SearchAsync("NY");

        Assert.Empty(results);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_ReturnsParsedSuggestions()
    {
        var handler = FakeHttpMessageHandler.ReturningJson(SampleJson);
        var sut = CreateSut(handler);

        var results = await sut.SearchAsync("London");

        var suggestion = Assert.Single(results);
        Assert.Equal("London, UK", suggestion.DisplayName);
        Assert.Equal(51.5072, suggestion.Lat, precision: 4);
    }

    [Fact]
    public async Task SearchAsync_SecondCallForSameQuery_UsesCacheAndSkipsHttp()
    {
        var handler = FakeHttpMessageHandler.ReturningJson(SampleJson);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(handler, cache);

        await sut.SearchAsync("London");
        await sut.SearchAsync("London");

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task SearchAsync_HandlerThrows_ReturnsEmptyInsteadOfThrowing()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("network down"));
        var sut = CreateSut(handler);

        var results = await sut.SearchAsync("London");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ServerError_ReturnsEmptyInsteadOfThrowing()
    {
        var handler = FakeHttpMessageHandler.ReturningJson("{}", HttpStatusCode.InternalServerError);
        var sut = CreateSut(handler);

        // GetFromJsonAsync throws HttpRequestException on non-success status, which the
        // service must swallow and fall back to an empty list.
        var results = await sut.SearchAsync("London");

        Assert.Empty(results);
    }

    [Fact]
    public async Task WaitAsync_EnforcesAtLeastOneSecondBetweenCalls()
    {
        var gate = new NominatimRateGate();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await gate.WaitAsync();
        await gate.WaitAsync();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds >= 950, $"Expected >= ~1000ms between calls, was {sw.ElapsedMilliseconds}ms");
    }
}
