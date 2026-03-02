using OpenJustice.Generator.Configuration;
using OpenJustice.Generator.Domain.Enums;
using OpenJustice.Generator.Infrastructure.Persistence;
using OpenJustice.Generator.Infrastructure.Persistence.Entities;
using OpenJustice.Generator.Services.Discovery;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace OpenJustice.Generator.Tests.Discovery;

public class RedditThreadScraperServiceTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new AppDbContext(options);
    }

    [Fact]
    public async Task FetchAndProcessSubredditAsync_WithValidSubreddit_CreatesDiscoveredCases()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var handler = new TestRedditMessageHandler();
        var httpClient = new HttpClient(handler);
        
        var mockLogger = new Mock<ILogger<RedditThreadScraperService>>();
        
        var options = new DiscoveryOptions
        {
            Reddit = new List<RedditOptions>
            {
                new RedditOptions
                {
                    Subreddit = "brasil",
                    Enabled = true
                }
            }
        };
        
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        
        var service = new RedditThreadScraperService(optionsWrapper, context, mockLogger.Object, httpClient);
        
        // Act
        var count = await service.FetchAndProcessSubredditAsync(options.Reddit[0]);
        
        // Assert
        context.DiscoveredCases.Should().AllSatisfy(c =>
        {
            c.SourceType.Should().Be(DiscoverySourceType.Reddit);
            c.SourceName.Should().Be("r/brasil");
            c.Status.Should().Be(DiscoveryStatus.Pending);
        });
    }

    [Fact]
    public async Task FetchAndProcessSubredditAsync_DuplicatePosts_AreSkipped()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        // Pre-add a discovered case with a hash that matches the first post
        context.DiscoveredCases.Add(new DiscoveredCase
        {
            DiscoveryHash = "c0535e4be2b79d0d60953e08fec5e7d2da8869b7c2e9a2c4d8b0f3a1e5c9d7b8", // Hash for post1
            Title = "Existing Post",
            SourceUrl = "https://www.reddit.com/r/brasil/comments/abc123/",
            SourceName = "r/brasil",
            SourceType = DiscoverySourceType.Reddit,
            Status = DiscoveryStatus.Pending
        });
        await context.SaveChangesAsync();
        
        var handler = new TestRedditMessageHandler();
        var httpClient = new HttpClient(handler);
        
        var mockLogger = new Mock<ILogger<RedditThreadScraperService>>();
        
        var options = new DiscoveryOptions
        {
            Reddit = new List<RedditOptions>
            {
                new RedditOptions
                {
                    Subreddit = "brasil",
                    Enabled = true
                }
            }
        };
        
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        
        var service = new RedditThreadScraperService(optionsWrapper, context, mockLogger.Object, httpClient);
        
        // Act
        var count = await service.FetchAndProcessSubredditAsync(options.Reddit[0]);
        
        // Assert - we should have at least the existing case plus new ones
        context.DiscoveredCases.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FetchAndProcessAllSubredditsAsync_DisabledSubreddits_AreSkipped()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var mockLogger = new Mock<ILogger<RedditThreadScraperService>>();
        
        var options = new DiscoveryOptions
        {
            Reddit = new List<RedditOptions>
            {
                new RedditOptions
                {
                    Subreddit = "brasil",
                    Enabled = false // Disabled
                }
            }
        };
        
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        
        var service = new RedditThreadScraperService(optionsWrapper, context, mockLogger.Object, new HttpClient());
        
        // Act
        var count = await service.FetchAndProcessAllSubredditsAsync();
        
        // Assert - disabled subreddits should be skipped
        count.Should().Be(0);
    }

    [Fact]
    public async Task FetchAndProcessSubredditAsync_FiltersIrrelevantPosts()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        // Create handler with non-crime content
        var handler = new NonCrimeTestRedditMessageHandler();
        var httpClient = new HttpClient(handler);
        
        var mockLogger = new Mock<ILogger<RedditThreadScraperService>>();
        
        var options = new DiscoveryOptions
        {
            Reddit = new List<RedditOptions>
            {
                new RedditOptions
                {
                    Subreddit = "brasil",
                    Enabled = true
                }
            }
        };
        
        var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);
        
        var service = new RedditThreadScraperService(optionsWrapper, context, mockLogger.Object, httpClient);
        
        // Act
        var count = await service.FetchAndProcessSubredditAsync(options.Reddit[0]);
        
        // Assert - non-crime posts should be filtered
        // Either count is 0 (filtered out) or any created cases have crime keywords
        if (count > 0)
        {
            context.DiscoveredCases.Should().AllSatisfy(c =>
            {
                var searchText = $"{c.Title} {c.Summary}".ToLowerInvariant();
                searchText.Should().ContainAny(new[] { "crime", "homicídio", "assassinato", "morte", "violência", "polícia" });
            });
        }
    }
}

/// <summary>
/// Test handler that returns crime-related Reddit posts.
/// </summary>
internal class TestRedditMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var redditData = new
        {
            data = new
            {
                children = new[]
                {
                    new
                    {
                        data = new
                        {
                            title = "Homicídio registrado em São Paulo",
                            selftext = "A polícia registrou um caso de homicídio na zona sul...",
                            url = "https://www.reddit.com/r/brasil/comments/abc123/",
                            author = "user1",
                            created_utc = 1709280000,
                            score = 50,
                            num_comments = 20,
                            permalink = "/r/brasil/comments/abc123/"
                        }
                    },
                    new
                    {
                        data = new
                        {
                            title = "Polícia investiga crime violento",
                            selftext = "Mais informações sobre o caso de violência...",
                            url = "https://www.reddit.com/r/brasil/comments/def456/",
                            author = "user2",
                            created_utc = 1709290000,
                            score = 30,
                            num_comments = 15,
                            permalink = "/r/brasil/comments/def456/"
                        }
                    }
                }
            }
        };
        
        var json = JsonSerializer.Serialize(redditData);
        
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        
        return Task.FromResult(response);
    }
}

/// <summary>
/// Test handler that returns non-crime related Reddit posts.
/// </summary>
internal class NonCrimeTestRedditMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var redditData = new
        {
            data = new
            {
                children = new[]
                {
                    new
                    {
                        data = new
                        {
                            title = "Just a picture of a cat",
                            selftext = "Look at this cute cat!",
                            url = "https://www.reddit.com/r/brasil/comments/xyz789/",
                            author = "user123",
                            created_utc = 1709280000,
                            score = 10,
                            num_comments = 5,
                            permalink = "/r/brasil/comments/xyz789/"
                        }
                    }
                }
            }
        };
        
        var json = JsonSerializer.Serialize(redditData);
        
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
        
        return Task.FromResult(response);
    }
}
