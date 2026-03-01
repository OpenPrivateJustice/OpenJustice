namespace AtrocidadesRSS.Generator.Configuration;

/// <summary>
/// Configuration options for RSS feed discovery.
/// </summary>
public class RssFeedOptions
{
    /// <summary>
    /// URL of the RSS feed.
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for the feed.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this feed is enabled for discovery.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Polling interval in minutes.
    /// </summary>
    public int PollIntervalMinutes { get; set; } = 60;
}

/// <summary>
/// Configuration options for Reddit thread discovery.
/// </summary>
public class RedditOptions
{
    /// <summary>
    /// Subreddit to monitor (without r/ prefix).
    /// </summary>
    public string Subreddit { get; set; } = string.Empty;
    
    /// <summary>
    /// Reddit application client ID.
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Reddit application client secret.
    /// </summary>
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// Whether Reddit discovery is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Polling interval in minutes.
    /// </summary>
    public int PollIntervalMinutes { get; set; } = 30;
}

/// <summary>
/// Discovery configuration options.
/// </summary>
public class DiscoveryOptions
{
    /// <summary>
    /// RSS feeds to monitor.
    /// </summary>
    public List<RssFeedOptions> RssFeeds { get; set; } = new();
    
    /// <summary>
    /// Reddit subreddits to monitor.
    /// </summary>
    public List<RedditOptions> Reddit { get; set; } = new();
    
    /// <summary>
    /// Enable automated discovery background service.
    /// </summary>
    public bool EnableBackgroundDiscovery { get; set; } = false;
}
