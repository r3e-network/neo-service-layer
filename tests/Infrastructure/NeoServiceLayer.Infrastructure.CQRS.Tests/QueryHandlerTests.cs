using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Infrastructure.CQRS;
using Xunit;

namespace NeoServiceLayer.Infrastructure.CQRS.Tests
{
    public class QueryHandlerTests
    {
        private readonly Mock<ILogger<QueryBus>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly QueryBus _queryBus;

        public QueryHandlerTests()
        {
            _mockLogger = new Mock<ILogger<QueryBus>>();
            _mockCache = new Mock<IMemoryCache>();
            _queryBus = new QueryBus(_mockLogger.Object, _mockCache.Object);
        }

        [Fact]
        public async Task HandleAsync_WithSimpleQuery_ReturnsResult()
        {
            // Arrange
            var query = new GetUserByIdQuery { UserId = Guid.NewGuid() };
            var handler = new GetUserByIdQueryHandler();
            _queryBus.RegisterHandler<GetUserByIdQuery, UserDto>(handler);

            // Act
            var result = await _queryBus.SendAsync<GetUserByIdQuery, UserDto>(query);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(query.UserId);
            result.Username.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task HandleAsync_WithPaginatedQuery_ReturnsPaginatedResult()
        {
            // Arrange
            var query = new GetUsersQuery 
            { 
                PageNumber = 2, 
                PageSize = 10,
                SortBy = "Username",
                SortDescending = false
            };
            
            var handler = new GetUsersQueryHandler();
            _queryBus.RegisterHandler<GetUsersQuery, PaginatedResult<UserDto>>(handler);

            // Act
            var result = await _queryBus.SendAsync<GetUsersQuery, PaginatedResult<UserDto>>(query);

            // Assert
            result.Should().NotBeNull();
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.Items.Should().HaveCount(10);
            result.TotalCount.Should().Be(100);
            result.TotalPages.Should().Be(10);
        }

        [Fact]
        public async Task HandleAsync_WithCachedQuery_ReturnsCachedResult()
        {
            // Arrange
            var query = new GetCachedDataQuery { Key = "test-key" };
            var cachedValue = new CachedData { Value = "cached-value" };
            
            object cacheEntry = cachedValue;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheEntry))
                .Returns(true);

            var handler = new GetCachedDataQueryHandler(_mockCache.Object);
            _queryBus.RegisterHandler<GetCachedDataQuery, CachedData>(handler);

            // Act
            var result = await _queryBus.SendAsync<GetCachedDataQuery, CachedData>(query);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().Be("cached-value");
            result.FromCache.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_WithFilteredQuery_AppliesFilters()
        {
            // Arrange
            var query = new GetFilteredProductsQuery
            {
                MinPrice = 10,
                MaxPrice = 100,
                Category = "Electronics",
                InStock = true
            };

            var handler = new GetFilteredProductsQueryHandler();
            _queryBus.RegisterHandler<GetFilteredProductsQuery, List<ProductDto>>(handler);

            // Act
            var result = await _queryBus.SendAsync<GetFilteredProductsQuery, List<ProductDto>>(query);

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(p => p.Price >= 10 && p.Price <= 100);
            result.Should().OnlyContain(p => p.Category == "Electronics");
            result.Should().OnlyContain(p => p.InStock);
        }

        [Fact]
        public async Task HandleAsync_WithAggregateQuery_CalculatesAggregates()
        {
            // Arrange
            var query = new GetSalesStatisticsQuery
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow,
                GroupBy = "Category"
            };

            var handler = new GetSalesStatisticsQueryHandler();
            _queryBus.RegisterHandler<GetSalesStatisticsQuery, SalesStatistics>(handler);

            // Act
            var result = await _queryBus.SendAsync<GetSalesStatisticsQuery, SalesStatistics>(query);

            // Assert
            result.Should().NotBeNull();
            result.TotalSales.Should().BeGreaterThan(0);
            result.AverageSale.Should().BeGreaterThan(0);
            result.SalesByCategory.Should().NotBeEmpty();
            result.PeriodStart.Should().Be(query.StartDate);
            result.PeriodEnd.Should().Be(query.EndDate);
        }

        [Fact]
        public async Task HandleAsync_WithProjection_ProjectsCorrectly()
        {
            // Arrange
            var query = new GetUserProjectionQuery 
            { 
                UserId = Guid.NewGuid(),
                IncludeOrders = true,
                IncludeAddresses = false
            };

            var handler = new GetUserProjectionQueryHandler();
            _queryBus.RegisterHandler<GetUserProjectionQuery, UserProjection>(handler);

            // Act
            var result = await _queryBus.SendAsync<GetUserProjectionQuery, UserProjection>(query);

            // Assert
            result.Should().NotBeNull();
            result.Orders.Should().NotBeEmpty();
            result.Addresses.Should().BeNull();
        }

        [Fact]
        public async Task HandleAsync_WithSearchQuery_ReturnsSearchResults()
        {
            // Arrange
            var query = new SearchQuery
            {
                SearchTerm = "test product",
                SearchFields = new[] { "Name", "Description" },
                MaxResults = 20,
                FuzzySearch = true
            };

            var handler = new SearchQueryHandler();
            _queryBus.RegisterHandler<SearchQuery, SearchResult>(handler);

            // Act
            var result = await _queryBus.SendAsync<SearchQuery, SearchResult>(query);

            // Assert
            result.Should().NotBeNull();
            result.Results.Should().NotBeEmpty();
            result.Results.Count.Should().BeLessThanOrEqualTo(20);
            result.TotalMatches.Should().BeGreaterThan(0);
            result.SearchTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public async Task HandleAsync_WithCancellation_CancelsQuery()
        {
            // Arrange
            var query = new LongRunningQuery { Duration = TimeSpan.FromSeconds(10) };
            var handler = new LongRunningQueryHandler();
            _queryBus.RegisterHandler<LongRunningQuery, string>(handler);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(100);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _queryBus.SendAsync<LongRunningQuery, string>(query, cts.Token));
        }

        [Fact]
        public async Task HandleAsync_WithBatchQuery_ProcessesBatch()
        {
            // Arrange
            var query = new BatchQuery<Guid>
            {
                Ids = Enumerable.Range(1, 50).Select(_ => Guid.NewGuid()).ToList()
            };

            var handler = new BatchQueryHandler();
            _queryBus.RegisterHandler<BatchQuery<Guid>, BatchResult<UserDto>>(handler);

            // Act
            var result = await _queryBus.SendAsync<BatchQuery<Guid>, BatchResult<UserDto>>(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(50);
            result.FailedIds.Should().BeEmpty();
            result.ProcessingTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public async Task HandleAsync_WithGraphQuery_ReturnsGraphStructure()
        {
            // Arrange
            var query = new GetOrganizationTreeQuery
            {
                RootId = Guid.NewGuid(),
                MaxDepth = 3,
                IncludeInactive = false
            };

            var handler = new GetOrganizationTreeQueryHandler();
            _queryBus.RegisterHandler<GetOrganizationTreeQuery, TreeNode>(handler);

            // Act
            var result = await _queryBus.SendAsync<GetOrganizationTreeQuery, TreeNode>(query);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(query.RootId);
            result.Children.Should().NotBeEmpty();
            result.GetDepth().Should().BeLessThanOrEqualTo(3);
        }

        [Fact]
        public void RegisterHandler_WithDuplicateRegistration_ThrowsException()
        {
            // Arrange
            var handler1 = new GetUserByIdQueryHandler();
            var handler2 = new GetUserByIdQueryHandler();
            
            _queryBus.RegisterHandler<GetUserByIdQuery, UserDto>(handler1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _queryBus.RegisterHandler<GetUserByIdQuery, UserDto>(handler2));
        }

        [Fact]
        public async Task SendAsync_WithoutHandler_ThrowsException()
        {
            // Arrange
            var query = new UnhandledQuery();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _queryBus.SendAsync<UnhandledQuery, object>(query));
        }
    }

    // Test Queries
    public class GetUserByIdQuery : IQuery<UserDto>
    {
        public Guid UserId { get; set; }
    }

    public class GetUsersQuery : IQuery<PaginatedResult<UserDto>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string SortBy { get; set; }
        public bool SortDescending { get; set; }
    }

    public class GetCachedDataQuery : IQuery<CachedData>
    {
        public string Key { get; set; }
    }

    public class GetFilteredProductsQuery : IQuery<List<ProductDto>>
    {
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string Category { get; set; }
        public bool InStock { get; set; }
    }

    public class GetSalesStatisticsQuery : IQuery<SalesStatistics>
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string GroupBy { get; set; }
    }

    public class GetUserProjectionQuery : IQuery<UserProjection>
    {
        public Guid UserId { get; set; }
        public bool IncludeOrders { get; set; }
        public bool IncludeAddresses { get; set; }
    }

    public class SearchQuery : IQuery<SearchResult>
    {
        public string SearchTerm { get; set; }
        public string[] SearchFields { get; set; }
        public int MaxResults { get; set; }
        public bool FuzzySearch { get; set; }
    }

    public class LongRunningQuery : IQuery<string>
    {
        public TimeSpan Duration { get; set; }
    }

    public class BatchQuery<T> : IQuery<BatchResult<UserDto>>
    {
        public List<T> Ids { get; set; }
    }

    public class GetOrganizationTreeQuery : IQuery<TreeNode>
    {
        public Guid RootId { get; set; }
        public int MaxDepth { get; set; }
        public bool IncludeInactive { get; set; }
    }

    public class UnhandledQuery : IQuery<object> { }

    // Test DTOs
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class CachedData
    {
        public string Value { get; set; }
        public bool FromCache { get; set; }
    }

    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public bool InStock { get; set; }
    }

    public class SalesStatistics
    {
        public decimal TotalSales { get; set; }
        public decimal AverageSale { get; set; }
        public Dictionary<string, decimal> SalesByCategory { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class UserProjection
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<Order> Orders { get; set; }
        public List<Address> Addresses { get; set; }
    }

    public class Order
    {
        public Guid OrderId { get; set; }
        public decimal Total { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    public class SearchResult
    {
        public List<SearchResultItem> Results { get; set; }
        public int TotalMatches { get; set; }
        public TimeSpan SearchTime { get; set; }
    }

    public class SearchResultItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public double Score { get; set; }
    }

    public class BatchResult<T>
    {
        public List<T> Items { get; set; }
        public List<Guid> FailedIds { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }

    public class TreeNode
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<TreeNode> Children { get; set; } = new();

        public int GetDepth()
        {
            if (Children == null || !Children.Any())
                return 1;
            return 1 + Children.Max(c => c.GetDepth());
        }
    }

    // Test Handlers
    public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
    {
        public async Task<UserDto> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return new UserDto
            {
                Id = query.UserId,
                Username = "testuser",
                Email = "test@example.com"
            };
        }
    }

    public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PaginatedResult<UserDto>>
    {
        public async Task<PaginatedResult<UserDto>> HandleAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            
            var users = Enumerable.Range(1, 10).Select(i => new UserDto
            {
                Id = Guid.NewGuid(),
                Username = $"user{i}",
                Email = $"user{i}@example.com"
            }).ToList();

            return new PaginatedResult<UserDto>
            {
                Items = users,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = 100,
                TotalPages = 10
            };
        }
    }

    public class GetCachedDataQueryHandler : IQueryHandler<GetCachedDataQuery, CachedData>
    {
        private readonly IMemoryCache _cache;

        public GetCachedDataQueryHandler(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<CachedData> HandleAsync(GetCachedDataQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            
            if (_cache.TryGetValue(query.Key, out CachedData cached))
            {
                cached.FromCache = true;
                return cached;
            }

            return new CachedData
            {
                Value = "fresh-value",
                FromCache = false
            };
        }
    }

    public class GetFilteredProductsQueryHandler : IQueryHandler<GetFilteredProductsQuery, List<ProductDto>>
    {
        public async Task<List<ProductDto>> HandleAsync(GetFilteredProductsQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);

            var products = Enumerable.Range(1, 20).Select(i => new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = $"Product {i}",
                Price = i * 5,
                Category = i % 2 == 0 ? "Electronics" : "Other",
                InStock = i % 3 != 0
            });

            return products
                .Where(p => p.Price >= query.MinPrice && p.Price <= query.MaxPrice)
                .Where(p => p.Category == query.Category)
                .Where(p => !query.InStock || p.InStock)
                .ToList();
        }
    }

    public class GetSalesStatisticsQueryHandler : IQueryHandler<GetSalesStatisticsQuery, SalesStatistics>
    {
        public async Task<SalesStatistics> HandleAsync(GetSalesStatisticsQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);

            return new SalesStatistics
            {
                TotalSales = 50000,
                AverageSale = 250,
                SalesByCategory = new Dictionary<string, decimal>
                {
                    ["Electronics"] = 30000,
                    ["Clothing"] = 15000,
                    ["Other"] = 5000
                },
                PeriodStart = query.StartDate,
                PeriodEnd = query.EndDate
            };
        }
    }

    public class GetUserProjectionQueryHandler : IQueryHandler<GetUserProjectionQuery, UserProjection>
    {
        public async Task<UserProjection> HandleAsync(GetUserProjectionQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);

            var projection = new UserProjection
            {
                Id = query.UserId,
                Name = "Test User"
            };

            if (query.IncludeOrders)
            {
                projection.Orders = new List<Order>
                {
                    new Order { OrderId = Guid.NewGuid(), Total = 100 },
                    new Order { OrderId = Guid.NewGuid(), Total = 200 }
                };
            }

            if (query.IncludeAddresses)
            {
                projection.Addresses = new List<Address>
                {
                    new Address { Street = "123 Main St", City = "Test City" }
                };
            }

            return projection;
        }
    }

    public class SearchQueryHandler : IQueryHandler<SearchQuery, SearchResult>
    {
        public async Task<SearchResult> HandleAsync(SearchQuery query, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            await Task.Delay(50, cancellationToken);

            var results = Enumerable.Range(1, 15).Select(i => new SearchResultItem
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"Result {i} for {query.SearchTerm}",
                Score = 1.0 - (i * 0.05)
            }).Take(query.MaxResults).ToList();

            return new SearchResult
            {
                Results = results,
                TotalMatches = 50,
                SearchTime = DateTime.UtcNow - startTime
            };
        }
    }

    public class LongRunningQueryHandler : IQueryHandler<LongRunningQuery, string>
    {
        public async Task<string> HandleAsync(LongRunningQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Delay(query.Duration, cancellationToken);
            return "Completed";
        }
    }

    public class BatchQueryHandler : IQueryHandler<BatchQuery<Guid>, BatchResult<UserDto>>
    {
        public async Task<BatchResult<UserDto>> HandleAsync(BatchQuery<Guid> query, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            await Task.Delay(100, cancellationToken);

            var items = query.Ids.Select(id => new UserDto
            {
                Id = id,
                Username = $"user_{id}",
                Email = $"{id}@example.com"
            }).ToList();

            return new BatchResult<UserDto>
            {
                Items = items,
                FailedIds = new List<Guid>(),
                ProcessingTime = DateTime.UtcNow - startTime
            };
        }
    }

    public class GetOrganizationTreeQueryHandler : IQueryHandler<GetOrganizationTreeQuery, TreeNode>
    {
        public async Task<TreeNode> HandleAsync(GetOrganizationTreeQuery query, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return BuildTree(query.RootId, query.MaxDepth, 1);
        }

        private TreeNode BuildTree(Guid id, int maxDepth, int currentDepth)
        {
            var node = new TreeNode
            {
                Id = id,
                Name = $"Node {id}"
            };

            if (currentDepth < maxDepth)
            {
                for (int i = 0; i < 3; i++)
                {
                    node.Children.Add(BuildTree(Guid.NewGuid(), maxDepth, currentDepth + 1));
                }
            }

            return node;
        }
    }
}