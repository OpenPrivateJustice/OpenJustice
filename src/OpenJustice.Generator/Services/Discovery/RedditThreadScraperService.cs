using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenJustice.Generator.Configuration;
using OpenJustice.Generator.Domain.Enums;
using OpenJustice.Generator.Infrastructure.Persistence;
using OpenJustice.Generator.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace OpenJustice.Generator.Services.Discovery;

/// <summary>
/// Service for scraping Reddit threads for case discovery.
/// </summary>
public interface IRedditThreadScraperService
{
    /// <summary>
    /// Fetches and processes threads from all configured subreddits.
    /// </summary>
    Task<int> FetchAndProcessAllSubredditsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fetches and processes threads from a specific subreddit.
    /// </summary>
    Task<int> FetchAndProcessSubredditAsync(RedditOptions subreddit, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for scraping Reddit threads for case discovery.
/// </summary>
public class RedditThreadScraperService : IRedditThreadScraperService
{
    private readonly DiscoveryOptions _options;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RedditThreadScraperService> _logger;
    private readonly HttpClient _httpClient;

    public RedditThreadScraperService(
        IOptions<DiscoveryOptions> options,
        AppDbContext dbContext,
        ILogger<RedditThreadScraperService> logger,
        HttpClient httpClient)
    {
        _options = options.Value;
        _dbContext = dbContext;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc/>
    public async Task<int> FetchAndProcessAllSubredditsAsync(CancellationToken cancellationToken = default)
    {
        var totalProcessed = 0;
        
        foreach (var subreddit in _options.Reddit.Where(r => r.Enabled))
        {
            try
            {
                var count = await FetchAndProcessSubredditAsync(subreddit, cancellationToken);
                totalProcessed += count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subreddit r/{Subreddit}", subreddit.Subreddit);
            }
        }
        
        return totalProcessed;
    }

    /// <inheritdoc/>
    public async Task<int> FetchAndProcessSubredditAsync(RedditOptions subreddit, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching Reddit subreddit: r/{Subreddit}", subreddit.Subreddit);
        
        var posts = await FetchSubredditPostsAsync(subreddit, cancellationToken);
        var processedCount = 0;
        
        foreach (var post in posts)
        {
            var hash = GenerateDiscoveryHash($"reddit_{subreddit.Subreddit}", post.Url);
            
            // Check for duplicates
            if (await _dbContext.DiscoveredCases.AnyAsync(d => d.DiscoveryHash == hash, cancellationToken))
            {
                _logger.LogDebug("Skipping duplicate post: {Title}", post.Title);
                continue;
            }
            
            var discoveredCase = new DiscoveredCase
            {
                DiscoveryHash = hash,
                Title = post.Title,
                Summary = post.SelfText?.Length > 4000 ? post.SelfText[..4000] : post.SelfText,
                SourceUrl = post.Url,
                SourceName = $"r/{subreddit.Subreddit}",
                SourceType = DiscoverySourceType.Reddit,
                PublishedDate = post.CreatedUtc,
                DiscoveredAt = DateTime.UtcNow,
                Status = DiscoveryStatus.Pending,
                RawContent = JsonSerializer.Serialize(post),
                Metadata = JsonSerializer.Serialize(new
                {
                    post.Author,
                    post.Score,
                    post.NumComments,
                    post.Permalink
                })
            };
            
            _dbContext.DiscoveredCases.Add(discoveredCase);
            processedCount++;
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Processed {Count} posts from subreddit r/{Subreddit}", processedCount, subreddit.Subreddit);
        
        return processedCount;
    }

    private async Task<List<RedditPost>> FetchSubredditPostsAsync(RedditOptions subreddit, CancellationToken cancellationToken)
    {
        var posts = new List<RedditPost>();
        
        try
        {
            // Try to use Reddit API (no auth required for public feeds)
            var url = $"https://www.reddit.com/r/{subreddit.Subreddit}/new.json?limit=50";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "OpenJustice/1.0");
            
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch subreddit r/{Subreddit}: {StatusCode}", 
                    subreddit.Subreddit, response.StatusCode);
                return posts;
            }
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (!json.TryGetProperty("data", out var dataElement) || 
                !dataElement.TryGetProperty("children", out var children))
            {
                return posts;
            }
            
            foreach (var child in children.EnumerateArray())
            {
                if (!child.TryGetProperty("data", out var postData)) continue;
                
                var post = new RedditPost
                {
                    Title = postData.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                    SelfText = postData.TryGetProperty("selftext", out var selftext) ? selftext.GetString() : null,
                    Url = postData.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? "" : "",
                    Author = postData.TryGetProperty("author", out var author) ? author.GetString() : null,
                    CreatedUtc = DateTimeOffset.FromUnixTimeSeconds(
                        postData.TryGetProperty("created_utc", out var created) ? created.GetInt64() : 0).DateTime,
                    Score = postData.TryGetProperty("score", out var score) ? score.GetInt32() : 0,
                    NumComments = postData.TryGetProperty("num_comments", out var numComments) ? numComments.GetInt32() : 0,
                    Permalink = postData.TryGetProperty("permalink", out var permalink) ? permalink.GetString() : null
                };
                
                post.Url = $"https://www.reddit.com{post.Permalink}";
                
                // Filter posts that might be relevant (containing crime-related keywords)
                if (IsPotentiallyRelevant(post.Title, post.SelfText))
                {
                    posts.Add(post);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subreddit r/{Subreddit}", subreddit.Subreddit);
        }
        
        return posts;
    }

    private static bool IsPotentiallyRelevant(string title, string? selfText)
    {
        // Keywords that might indicate crime-related content
        var keywords = new[] 
        { 
            "crime", "homicídio", "assassinato", "morte", "violência", 
            "polícia", "justiça", "tribunal", "processo", "réu", "vítima",
            "mort", "tuerie", "meurtre", "violencia", "policía", "juicio"
        };
        
        var searchText = $"{title} {selfText}".ToLowerInvariant();
        
        // If the post has very little text, be more restrictive
        if (string.IsNullOrEmpty(selfText) || selfText.Length < 50)
        {
            // For link posts, only include if title has strong indicators
            return keywords.Any(k => searchText.Contains(k));
        }
        
        // For text posts, be more inclusive as the content may be relevant
        return keywords.Any(k => searchText.Contains(k)) || searchText.Length > 200;
    }

    private static string GenerateDiscoveryHash(string sourceName, string url)
    {
        var input = $"{sourceName}:{url}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

/// <summary>
/// Represents a Reddit post.
/// </summary>
internal class RedditPost
{
    public string Title { get; set; } = string.Empty;
    public string? SelfText { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Author { get; set; }
    public DateTime CreatedUtc { get; set; }
    public int Score { get; set; }
    public int NumComments { get; set; }
    public string? Permalink { get; set; }
}
