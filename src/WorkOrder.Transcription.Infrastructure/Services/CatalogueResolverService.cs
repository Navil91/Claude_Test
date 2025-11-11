using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using WorkOrder.Transcription.Application.Interfaces;
using WorkOrder.Transcription.Domain.ValueObjects;

namespace WorkOrder.Transcription.Infrastructure.Services;

public class CatalogueResolverService : ICatalogueResolverService
{
    private readonly IAssetRepository _assetRepo;
    private readonly IMemoryCache _cache;
    private const double FUZZY_THRESHOLD = 0.90; // 90% similarity

    public CatalogueResolverService(IAssetRepository assetRepo, IMemoryCache cache)
    {
        _assetRepo = assetRepo;
        _cache = cache;
    }

    public async Task<AssetResolution> ResolveAsync(List<string> candidates, CancellationToken cancellationToken = default)
    {
        if (!candidates.Any())
            return AssetResolution.NotFound();

        // Tier 1: Exact match (case-insensitive, normalized)
        foreach (var candidate in candidates)
        {
            var exact = await _assetRepo.FindExactAsync(Normalize(candidate), cancellationToken);
            if (exact != null)
                return AssetResolution.Exact(exact, confidence: 1.0);
        }

        // Tier 2: Fuzzy match (prefix-constrained)
        var fuzzyResults = new List<(Domain.Entities.Asset asset, double similarity)>();

        foreach (var candidate in candidates)
        {
            var prefix = GetPrefix(candidate);
            if (string.IsNullOrWhiteSpace(prefix))
                continue;

            var assetsWithPrefix = await GetAssetsByPrefixAsync(prefix, cancellationToken);

            foreach (var asset in assetsWithPrefix)
            {
                var similarity = CalculateSimilarity(candidate, asset.AssetId);
                if (similarity >= FUZZY_THRESHOLD)
                    fuzzyResults.Add((asset, similarity));
            }
        }

        if (!fuzzyResults.Any())
            return AssetResolution.NotFound();

        // Pick highest similarity
        var best = fuzzyResults.OrderByDescending(x => x.similarity).First();
        return AssetResolution.Fuzzy(best.asset, confidence: best.similarity);
    }

    private string Normalize(string id)
    {
        return id.Replace(" ", "").Replace("–", "-").ToUpperInvariant();
    }

    private string GetPrefix(string id)
    {
        var match = Regex.Match(id, @"^([A-Z]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : "";
    }

    private double CalculateSimilarity(string a, string b)
    {
        a = Normalize(a);
        b = Normalize(b);

        var distance = ComputeLevenshteinDistance(a, b);
        var maxLength = Math.Max(a.Length, b.Length);

        return maxLength > 0 ? 1.0 - (double)distance / maxLength : 0.0;
    }

    private int ComputeLevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var m = a.Length;
        var n = b.Length;
        var d = new int[m + 1, n + 1];

        for (var i = 0; i <= m; i++) d[i, 0] = i;
        for (var j = 0; j <= n; j++) d[0, j] = j;

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[m, n];
    }

    private async Task<List<Domain.Entities.Asset>> GetAssetsByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        var cacheKey = $"assets:prefix:{prefix}";
        if (_cache.TryGetValue(cacheKey, out List<Domain.Entities.Asset>? cached) && cached != null)
            return cached;

        var assets = await _assetRepo.FindByPrefixAsync(prefix, cancellationToken);
        _cache.Set(cacheKey, assets, TimeSpan.FromMinutes(30));
        return assets;
    }
}
