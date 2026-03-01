using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using AtrocidadesRSS.Generator.Configuration;
using AtrocidadesRSS.Generator.Domain.Enums;
using AtrocidadesRSS.Generator.Infrastructure.Persistence;
using AtrocidadesRSS.Generator.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AtrocidadesRSS.Generator.Services.Discovery;

/// <summary>
/// Service for aggregating and processing RSS feed content for case discovery.
/// </summary>
public interface IRssAggregatorService
{
    /// <summary>
    /// Fetches and processes items from all configured RSS feeds.
    /// </summary>
    Task<int> FetchAndProcessAllFeedsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fetches and processes items from a specific RSS feed.
    /// </summary>
    Task<int> FetchAndProcessFeedAsync(RssFeedOptions feed, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for aggregating and processing RSS feed content for case discovery.
/// </summary>
public class RssAggregatorService : IRssAggregatorService
{
    private readonly DiscoveryOptions _options;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RssAggregatorService> _logger;
    private readonly HttpClient _httpClient;

    public RssAggregatorService(
        IOptions<DiscoveryOptions> options,
        AppDbContext dbContext,
        ILogger<RssAggregatorService> logger,
        HttpClient httpClient)
    {
        _options = options.Value;
        _dbContext = dbContext;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc/>
    public async Task<int> FetchAndProcessAllFeedsAsync(CancellationToken cancellationToken = default)
    {
        var totalProcessed = 0;
        
        foreach (var feed in _options.RssFeeds.Where(f => f.Enabled))
        {
            try
            {
                var count = await FetchAndProcessFeedAsync(feed, cancellationToken);
                totalProcessed += count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RSS feed {FeedName}", feed.Name);
            }
        }
        
        return totalProcessed;
    }

    /// <inheritdoc/>
    public async Task<int> FetchAndProcessFeedAsync(RssFeedOptions feed, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching RSS feed: {FeedName} ({Url})", feed.Name, feed.Url);
        
        var items = await FetchFeedItemsAsync(feed.Url, cancellationToken);
        var processedCount = 0;
        
        foreach (var item in items)
        {
            var hash = GenerateDiscoveryHash(feed.Name, item.Url);
            
            // Check for duplicates
            if (await _dbContext.DiscoveredCases.AnyAsync(d => d.DiscoveryHash == hash, cancellationToken))
            {
                _logger.LogDebug("Skipping duplicate item: {Title}", item.Title);
                continue;
            }
            
            var discoveredCase = new DiscoveredCase
            {
                DiscoveryHash = hash,
                Title = item.Title,
                Summary = item.Summary,
                SourceUrl = item.Url,
                SourceName = feed.Name,
                SourceType = DiscoverySourceType.RSS,
                PublishedDate = item.PublishDate.DateTime,
                DiscoveredAt = DateTime.UtcNow,
                Status = DiscoveryStatus.Pending,
                RawContent = item.RawContent,
                Metadata = item.Metadata
            };
            
            _dbContext.DiscoveredCases.Add(discoveredCase);
            processedCount++;
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Processed {Count} items from RSS feed {FeedName}", processedCount, feed.Name);
        
        return processedCount;
    }

    private async Task<List<RssFeedItem>> FetchFeedItemsAsync(string url, CancellationToken cancellationToken)
    {
        var items = new List<RssFeedItem>();
        
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            using var stringReader = new StringReader(content);
            using var xmlReader = XmlReader.Create(stringReader);
            
            var feed = SyndicationFeed.Load(xmlReader);
            
            if (feed == null)
            {
                _logger.LogWarning("Failed to parse RSS feed from {Url}", url);
                return items;
            }
            
            foreach (var item in feed.Items)
            {
                var link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? string.Empty;
                var summary = item.Summary?.Text ?? string.Empty;
                
                // Clean HTML from summary
                summary = StripHtml(summary);
                
                items.Add(new RssFeedItem
                {
                    Title = item.Title?.Text ?? "Untitled",
                    Summary = summary.Length > 4000 ? summary[..4000] : summary,
                    Url = link,
                    PublishDate = item.PublishDate.DateTime,
                    RawContent = item.ToString(),
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Categories = item.Categories.Select(c => c.Name).ToList(),
                        Author = item.Authors.FirstOrDefault()?.Name
                    })
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching RSS feed from {Url}", url);
        }
        
        return items;
    }

    private static string GenerateDiscoveryHash(string sourceName, string url)
    {
        var input = $"{sourceName}:{url}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        
        // Simple HTML tag removal
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}

/// <summary>
/// Represents a normalized item from an RSS feed.
/// </summary>
internal class RssFeedItem
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset PublishDate { get; set; }
    public string? RawContent { get; set; }
    public string? Metadata { get; set; }
}
