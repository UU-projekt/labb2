using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PlacementService.Api.Models;
using PlacementService.Api.Options;

namespace PlacementService.Api.Services;

public sealed class PlacementServiceFacade
{
    private readonly JobSearchClient _jobSearchClient;
    private readonly ScbPxWebClient _scbPxWebClient;
    private readonly IMemoryCache _cache;
    private readonly ScbPxWebOptions _scbOptions;

    public PlacementServiceFacade(
        JobSearchClient jobSearchClient,
        ScbPxWebClient scbPxWebClient,
        IMemoryCache cache,
        IOptions<ScbPxWebOptions> scbOptions)
    {
        _jobSearchClient = jobSearchClient;
        _scbPxWebClient = scbPxWebClient;
        _cache = cache;
        _scbOptions = scbOptions.Value;
    }

    public async Task<PlacementSearchResponse> SearchAsync(
        string query,
        string? region,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        // Step 1: call JobSearchClient to get raw items
        var result = await _jobSearchClient.SearchAsync(query, region, limit, offset, cancellationToken);
        // !TODO: if there are no items, return early
        if (result.Total == 0) return new PlacementSearchResponse(result.Offset,result.Limit,result.Total, []);


        // Step 2: collect distinct SSYK codes (normalize each code) from the jobSearch result. 
        // HINT: use NormalizeSsyk(item.OccupationSsyk) with LINQ
        var ssnykCodes = result.Items.Select(item => NormalizeSsyk(item.OccupationSsyk)).ToHashSet();

        // Step 3: build a dictionary that maps SSYK with SalaryInfo using GetSalaryCachedAsync for each distinct SSYK.
        var ssykSalaryInfoDict = ssnykCodes.ToDictionary(x => x!, v => { 
            return GetSalaryCachedAsync(v!, cancellationToken).Result;
        });

        // Step 4: create a new list of PlacementItem where OccupationSsyk is normalized and Salary is looked up from the dictionary created in Step 3.
        // HINT: use TryGetValue on the dictionary to look up the salary for each item's normalized SSYK. If no match is found, salary will be null.
        // HINT: since PlacementItem is a record (not a class), you can use: item with { OccupationSsyk = normalized, Salary = salary } to create a copy with updated fields without rewriting all properties.
        // NOTE: records behave like classes for most purposes — the key difference here is that their properties are immutable, so with is how you create a modified copy.

        var enrichedItems = result.Items.Select(item => {
            var normalized = NormalizeSsyk(item.OccupationSsyk);
            ssykSalaryInfoDict.TryGetValue(normalized!, out var salary);
            return item with { OccupationSsyk = normalized, Salary = salary };
        }).ToList();

        // References:
        // C# records: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record
        // with keyword: https://learn.microsoft.com/en-us/dotnet/csharp/tutorials/records#nondestructive-mutation

        return new PlacementSearchResponse(result.Offset, result.Limit, result.Total, enrichedItems);
    }

    public async Task<PlacementSummaryResponse> GetSummaryAsync(
        string query,
        string? region,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        // Step 1: call SearchAsync to get enriched results for the current page
        // HINT: SearchAsync already attaches salary information to each PlacementItem
        var search = await SearchAsync(query, region, limit, offset, cancellationToken);
        
        // Step 2: group the items by a composite key of SSYK and normalized OccupationLabel
        // HINT: you can use GroupBy(item => $"{item.OccupationSsyk}|{NormalizeOccupationLabel(item.OccupationLabel)}") to ensure that both the code and the human-readable label define the group.
        var groupedItems = search.Items.GroupBy(item => $"{item.OccupationSsyk}|{NormalizeOccupationLabel(item.OccupationLabel)}");


        // Step 3: for each group, count the number of ads and pick one as a "representative" salary
        // HINT: the representative salary could be the first identified item’s Salary; you don’t need to recompute salaries here.
        var bingus = groupedItems.Select(group =>
        {
            var keyParts = group.Key.Split('|');
            var ssyk = keyParts[0];
            var label = keyParts[1];
            var adsCount = group.Count();
            var salary = group.FirstOrDefault(item => item.Salary != null)?.Salary;
            return new OccupationSummaryItem(ssyk, label, adsCount, salary);
        });

        // Step 4: build a list of OccupationSummaryItem objects sorted by descending AdsCount and then label
        var sortedSummary = bingus.OrderByDescending(item => item.AdsCount).ThenBy(item => item.OccupationLabel).ToList();

        // TODO: return a PlacementSummaryResponse containing the grouped summary
        // HINT: use search.Total for the total count, not the number of groups.
        return new PlacementSummaryResponse(query, region, offset, limit, search.Total, sortedSummary);
    }

    public async Task<SalaryInfo?> GetSalaryAsync(string ssyk, int? year, CancellationToken cancellationToken)
    {
        // Step 1: normalize the incoming SSYK code so that "1234" and "123401" are treated consistently
        // HINT: use NormalizeSsyk(ssyk) to strip whitespace and non-digit characters. If the normalized code is null or empty, return null (invalid input)
        var normalized = NormalizeSsyk(ssyk);
        if(normalized == null) return null;

        // Step 2: build a cache key combining the SSYK and the year (or "latest" if year is null) and check the IMemoryCache for a cached SalaryInfo
        // HINT: TryGetValue returns true/false and the out parameter will hold the cached value.
        var cacheKey = BuildCacheKey(normalized, year);
        _cache.TryGetValue(cacheKey, out SalaryInfo? cached);

        // Step 3: if not cached, call the SCB client to fetch the salary for this SSYK and year
        // HINT: await _scbPxWebClient.GetSalaryAsync(normalized, year, cancellationToken)
        if(cached == null)
        {
            var salary = await _scbPxWebClient.GetSalaryAsync(normalized, year, cancellationToken);
            if(salary == null) return null;
            // Step 4: store the fetched salary in the cache with an expiration based on _scbOptions.CacheMinutes
            _cache.Set(cacheKey, salary, TimeSpan.FromMinutes(_scbOptions.CacheMinutes));
            return salary;
        } else return cached;
    }

    private async Task<SalaryInfo?> GetSalaryCachedAsync(string ssyk, CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(ssyk, null);
        if (_cache.TryGetValue(cacheKey, out SalaryInfo? cached))
        {
            return cached;
        }

        var salary = await _scbPxWebClient.GetSalaryAsync(ssyk, null, cancellationToken);
        _cache.Set(cacheKey, salary, TimeSpan.FromMinutes(_scbOptions.CacheMinutes));
        return salary;
    }

    private string BuildCacheKey(string ssyk, int? year)
        => year is null ? $"salary:{ssyk}:latest" : $"salary:{ssyk}:{year}";

    private static string NormalizeOccupationLabel(string? value)
        => string.IsNullOrWhiteSpace(value) ? "(okänd yrkesroll)" : value.Trim();

    private string? NormalizeSsyk(string? ssyk)
    {
        if (string.IsNullOrWhiteSpace(ssyk))
        {
            return null;
        }

        var digits = new string(ssyk.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return null;
        }

        if (_scbOptions.NormalizeSsykTo3Digits && digits.Length >= 3)
        {
            return digits[..3];
        }

        return digits.Length > 4 ? digits[..4] : digits;
    }
}
