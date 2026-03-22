using Microsoft.AspNetCore.WebUtilities;
using PlacementService.Ui.Models;

namespace PlacementService.Ui.Services;

public sealed class PlacementApiClient
{
    private readonly HttpClient _httpClient;

    public PlacementApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResult<PlacementSearchResponse>> SearchAsync(
        string query,
        string? region,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        // Step 1: build a dictionary with query, limit and offset, plus region if provided to act as the parameters for the API-query. Build a url variable to include the API endpoint and the query parameters
        // HINT: Use QueryHelpers.AddQueryString to append parameters to the base path
        var constructedQuery = QueryHelpers.AddQueryString("/api/placements/search", new Dictionary<string, string?>
        {
            ["q"] = query,
            ["region"] = region,
            ["limit"] = limit.ToString(),
            ["offset"] = offset.ToString()
        });

        // Step 2: call _httpClient.GetAsync() with the full URL and inspect the response.StatusCode. You should then handle the following responses correctly:
        //   - 200 OK: deserialize JSON into PlacementSearchResponse.
        var ans = await _httpClient.GetAsync(constructedQuery, cancellationToken);
        if((int) ans.StatusCode == 200)
        {
            PlacementSearchResponse? data = await ans.Content.ReadFromJsonAsync<PlacementSearchResponse>(cancellationToken: cancellationToken);
            // Step 3: return a ApiResult<PlacementSearchResponse> with data or an error.
            return new ApiResult<PlacementSearchResponse>(data, null);
        } else
        {
            // HINT: In case of a successful response, ReadFromJsonAsync can be used on the response´s Content property to read the data.
            //   - 400 BadRequest: return a friendly message like "Felaktiga parametrar".
            //   - 404 NotFound: return "Inga resultat hittades".
            //   - Other status codes: return a generic error.
            return new ApiResult<PlacementSearchResponse>(null, ans.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => "Felaktiga parametrar",
                System.Net.HttpStatusCode.NotFound => "Inga resultat hittades",
                _ => "Ett fel inträffade"
            });
        }
    }

    // offset is included for API consistency with SearchAsync but summary always uses offset=0 so the grouping reflects the full result set - not a single page.
    public async Task<ApiResult<PlacementSummaryResponse>> SummaryAsync(
        string query,
        string? region,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        // Step 1: build a dictionary with query, limit and offset, plus region if provided to act as the parameters for the API-query. Build a url variable to include the API endpoint and the query parameters
        // HINT: Use QueryHelpers.AddQueryString to append parameters to the base path
        var constructedQuery = QueryHelpers.AddQueryString("/api/placements/summary", new Dictionary<string, string?>
        {
            ["q"] = query,
            ["region"] = region,
            ["limit"] = limit.ToString(),
            ["offset"] = offset.ToString()
        });
        // Step 2: call _httpClient.GetAsync() with the full URL and inspect the response.StatusCode. You should then handle the following responses correctly:
        //   - 200 OK: deserialize JSON into PlacementSearchResponse.
        var ans = await _httpClient.GetAsync(constructedQuery, cancellationToken);
        if ((int)ans.StatusCode == 200)
        {
            PlacementSummaryResponse? data = await ans.Content.ReadFromJsonAsync<PlacementSummaryResponse>(cancellationToken: cancellationToken);
            // Step 3: return a ApiResult<PlacementSearchResponse> with data or an error.
            return new ApiResult<PlacementSummaryResponse>(data, null);
        }
        else
        {
            // HINT: In case of a successful response, ReadFromJsonAsync can be used on the response´s Content property to read the data.
            //   - 400 BadRequest: return a friendly message like "Felaktiga parametrar".
            //   - 404 NotFound: return "Inga resultat hittades".
            //   - Other status codes: return a generic error.
            return new ApiResult<PlacementSummaryResponse>(null, ans.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => "Felaktiga parametrar",
                System.Net.HttpStatusCode.NotFound => "Inga resultat hittades",
                _ => "Ett fel inträffade"
            });
        }
    }
}
