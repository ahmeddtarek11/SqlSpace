using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.Insights;
using SqlSpace.Application.Abstractions.Users;
using SqlSpace.Application.DTOs.Query;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Services.Insights;

public sealed class InsightsService(
    IApplicationDbContext dbContext,
    IAccessControlService accessControlService,
    IUserRepository userRepository) : IInsightsService
{
    private static readonly Regex FromJoinRegex = new(
        @"\b(?:FROM|JOIN)\s+(?<table>[A-Za-z0-9_\.\[\]""`]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IAccessControlService _accessControlService = accessControlService;
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<Result<ConnectionInsights>> GetUserInsightsAsync(
        Guid connectionId,
        string userId,
        InsightsQuery query,
        CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return Result<ConnectionInsights>.Failure(
                new Error("insights.invalid_connection_id", "ConnectionId is required.", nameof(connectionId)));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<ConnectionInsights>.Failure(
                new Error("insights.invalid_user_id", "UserId is required.", nameof(userId)));
        }

        var accessResult = await _accessControlService.HasAccessToConnectionAsync(
            connectionId,
            userId,
            cancellationToken);

        if (accessResult.IsFailure)
        {
            return Result<ConnectionInsights>.Failure(accessResult.Errors);
        }

        if (accessResult.Value != true)
        {
            return Result<ConnectionInsights>.Failure(
                new Error("insights.forbidden", "User does not have access to this connection.", nameof(userId)));
        }

        return await BuildInsightsAsync(connectionId, userId, isAdmin: false, query, cancellationToken);
    }

    public async Task<Result<ConnectionInsights>> GetAdminInsightsAsync(
        Guid connectionId,
        string adminUserId,
        InsightsQuery query,
        CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            return Result<ConnectionInsights>.Failure(
                new Error("insights.invalid_connection_id", "ConnectionId is required.", nameof(connectionId)));
        }

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Result<ConnectionInsights>.Failure(
                new Error("insights.invalid_user_id", "UserId is required.", nameof(adminUserId)));
        }

        var connection = await _dbContext.ConnectedDatabases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && !c.IsDeleted, cancellationToken);

        if (connection is null)
        {
            return Result<ConnectionInsights>.Failure(
                new Error("insights.connection_not_found", "Connection not found.", nameof(connectionId)));
        }

        if (!string.Equals(connection.DbAdminId, adminUserId, StringComparison.Ordinal))
        {
            return Result<ConnectionInsights>.Failure(
                new Error("insights.forbidden", "User is not authorized to view admin insights.", nameof(adminUserId)));
        }

        return await BuildInsightsAsync(connectionId, adminUserId, isAdmin: true, query, cancellationToken);
    }

    private async Task<Result<ConnectionInsights>> BuildInsightsAsync(
        Guid connectionId,
        string userId,
        bool isAdmin,
        InsightsQuery query,
        CancellationToken cancellationToken)
    {
        var connection = await _dbContext.ConnectedDatabases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && !c.IsDeleted, cancellationToken);

        if (connection is null)
        {
            return Result<ConnectionInsights>.Failure(
                new Error("insights.connection_not_found", "Connection not found.", nameof(connectionId)));
        }

        var dateFrom = query.DateFrom ?? DateTime.UtcNow.Date.AddDays(-30);
        var dateTo = query.DateTo ?? DateTime.UtcNow;

        if (dateFrom > dateTo)
        {
            return Result<ConnectionInsights>.Failure(
                new Error("insights.invalid_date_range", "dateFrom cannot be after dateTo.", nameof(query.DateFrom)));
        }

        var baseQuery = _dbContext.QueryHistories
            .AsNoTracking()
            .Where(q => q.DatabaseConnectionId == connectionId)
            .Where(q => q.ExecutedAt >= dateFrom && q.ExecutedAt <= dateTo);

        if (!isAdmin)
        {
            baseQuery = baseQuery.Where(q => q.UserId == userId);
        }

        var totalQueries = await baseQuery.CountAsync(cancellationToken);

        if (totalQueries == 0)
        {
            return new ConnectionInsights
            {
                Summary = new InsightsSummary
                {
                    TotalQueries = 0,
                    SuccessfulQueries = 0,
                    FailedQueries = 0,
                    AverageExecutionTimeMs = 0,
                    TotalRowsReturned = 0,
                    FirstQueryDate = null,
                    LastQueryDate = null
                },
                Volume = BuildEmptyVolume(dateFrom, dateTo, query.Bucket),
                TopConnections = BuildTopConnections(connectionId, connection.ConnectionName, 0, isAdmin),
                TopUsers = Array.Empty<UserQueryCount>(),
                TopTables = Array.Empty<TableQueryCount>(),
                Cards = BuildCards(connectionId, connection.ConnectionName, null, BuildEmptyVolume(dateFrom, dateTo, query.Bucket), Array.Empty<TableQueryCount>(), 0, 0)
            };
        }

        var successfulQueries = await baseQuery.CountAsync(q => q.Status == QueryStatus.Success, cancellationToken);
        var averageExecutionTime = await baseQuery
            .Where(q => q.ExecutionTimeMs.HasValue)
            .AverageAsync(q => (double?)q.ExecutionTimeMs, cancellationToken) ?? 0d;

        var totalRows = await baseQuery
            .Where(q => q.RowsReturned.HasValue)
            .SumAsync(q => (long?)q.RowsReturned, cancellationToken) ?? 0L;

        var firstQueryDate = await baseQuery.MinAsync(q => (DateTime?)q.ExecutedAt, cancellationToken);
        var lastQueryDate = await baseQuery.MaxAsync(q => (DateTime?)q.ExecutedAt, cancellationToken);

        var summary = new InsightsSummary
        {
            TotalQueries = totalQueries,
            SuccessfulQueries = successfulQueries,
            FailedQueries = totalQueries - successfulQueries,
            AverageExecutionTimeMs = averageExecutionTime,
            TotalRowsReturned = totalRows,
            FirstQueryDate = firstQueryDate,
            LastQueryDate = lastQueryDate
        };

        var volume = await BuildVolumeAsync(baseQuery, dateFrom, dateTo, query.Bucket, cancellationToken);
        var topTables = await BuildTopTablesAsync(baseQuery, query.TopN, cancellationToken);
        var topConnections = BuildTopConnections(connectionId, connection.ConnectionName, totalQueries, isAdmin);
        var topUsers = isAdmin
            ? await BuildTopUsersAsync(baseQuery, query.TopN, cancellationToken)
            : Array.Empty<UserQueryCount>();

        var cards = BuildCards(
            connectionId,
            connection.ConnectionName,
            lastQueryDate,
            volume,
            topTables,
            successfulQueries,
            totalQueries - successfulQueries,
            topUsers);

        return new ConnectionInsights
        {
            Summary = summary,
            Volume = volume,
            TopTables = topTables,
            TopConnections = topConnections,
            TopUsers = topUsers,
            Cards = cards
        };
    }

    private static IReadOnlyList<InsightVolumeBucket> BuildEmptyVolume(
        DateTime dateFrom,
        DateTime dateTo,
        InsightsBucket bucket)
    {
        var buckets = new List<InsightVolumeBucket>();
        foreach (var date in EnumerateBuckets(dateFrom, dateTo, bucket))
        {
            buckets.Add(new InsightVolumeBucket
            {
                Date = date,
                Total = 0,
                Successful = 0,
                Failed = 0
            });
        }

        return buckets;
    }

    private static IReadOnlyList<ConnectionQueryCount> BuildTopConnections(
        Guid connectionId,
        string connectionName,
        int totalQueries,
        bool isAdmin)
    {
        if (isAdmin)
        {
            return Array.Empty<ConnectionQueryCount>();
        }

        return new List<ConnectionQueryCount>
        {
            new()
            {
                ConnectionId = connectionId,
                ConnectionName = connectionName,
                QueryCount = totalQueries
            }
        };
    }

    private async Task<IReadOnlyList<UserQueryCount>> BuildTopUsersAsync(
        IQueryable<Domain.Models.QueryHistory> baseQuery,
        int topN,
        CancellationToken cancellationToken)
    {
        var topUsers = await baseQuery
            .GroupBy(q => q.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.UserId)
            .Take(Math.Max(topN, 1))
            .ToListAsync(cancellationToken);

        if (topUsers.Count == 0)
        {
            return Array.Empty<UserQueryCount>();
        }

        var userIds = topUsers.Select(u => u.UserId).ToArray();
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var usersById = users.ToDictionary(u => u.Id, StringComparer.Ordinal);

        return topUsers.Select(u =>
        {
            usersById.TryGetValue(u.UserId, out var user);
            return new UserQueryCount
            {
                UserId = u.UserId,
                UserEmail = user?.Email ?? string.Empty,
                UserName = user?.UserName ?? string.Empty,
                QueryCount = u.Count
            };
        }).ToList();
    }

    private async Task<IReadOnlyList<TableQueryCount>> BuildTopTablesAsync(
        IQueryable<Domain.Models.QueryHistory> baseQuery,
        int topN,
        CancellationToken cancellationToken)
    {
        var sqlList = await baseQuery
            .Where(q => q.Status == QueryStatus.Success && !string.IsNullOrWhiteSpace(q.GeneratedSql))
            .Select(q => q.GeneratedSql)
            .ToListAsync(cancellationToken);

        if (sqlList.Count == 0)
        {
            return Array.Empty<TableQueryCount>();
        }

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var sql in sqlList)
        {
            var tables = ExtractTableNames(sql);
            foreach (var table in tables.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                counts[table] = counts.TryGetValue(table, out var current) ? current + 1 : 1;
            }
        }

        return counts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(topN, 1))
            .Select(kv => new TableQueryCount { TableName = kv.Key, QueryCount = kv.Value })
            .ToList();
    }

    private async Task<IReadOnlyList<InsightVolumeBucket>> BuildVolumeAsync(
        IQueryable<QueryHistory> baseQuery,
        DateTime dateFrom,
        DateTime dateTo,
        InsightsBucket bucket,
        CancellationToken cancellationToken)
    {
        var executions = await baseQuery
            .Select(q => new { q.ExecutedAt, q.Status })
            .ToListAsync(cancellationToken);

        var grouped = executions
            .GroupBy(e => GetBucketStart(e.ExecutedAt, bucket))
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Total = g.Count(),
                    Successful = g.Count(x => x.Status == QueryStatus.Success),
                    Failed = g.Count(x => x.Status != QueryStatus.Success)
                });

        var buckets = new List<InsightVolumeBucket>();
        foreach (var date in EnumerateBuckets(dateFrom, dateTo, bucket))
        {
            if (grouped.TryGetValue(date, out var counts))
            {
                buckets.Add(new InsightVolumeBucket
                {
                    Date = date,
                    Total = counts.Total,
                    Successful = counts.Successful,
                    Failed = counts.Failed
                });
            }
            else
            {
                buckets.Add(new InsightVolumeBucket
                {
                    Date = date,
                    Total = 0,
                    Successful = 0,
                    Failed = 0
                });
            }
        }

        return buckets;
    }

    private static DateTime GetBucketStart(DateTime dateTime, InsightsBucket bucket)
    {
        var date = dateTime.Date;
        return bucket switch
        {
            InsightsBucket.Day => date,
            InsightsBucket.Week => date.AddDays(-((int)date.DayOfWeek + 6) % 7),
            InsightsBucket.Month => new DateTime(date.Year, date.Month, 1),
            _ => date
        };
    }

    private static IEnumerable<DateTime> EnumerateBuckets(DateTime dateFrom, DateTime dateTo, InsightsBucket bucket)
    {
        var current = GetBucketStart(dateFrom, bucket);
        var end = GetBucketStart(dateTo, bucket);

        while (current <= end)
        {
            yield return current;
            current = bucket switch
            {
                InsightsBucket.Day => current.AddDays(1),
                InsightsBucket.Week => current.AddDays(7),
                InsightsBucket.Month => current.AddMonths(1),
                _ => current.AddDays(1)
            };
        }
    }

    private static IReadOnlyList<string> ExtractTableNames(string sql)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(sql))
        {
            return tables.ToList();
        }

        foreach (Match match in FromJoinRegex.Matches(sql))
        {
            AddExtractedTable(tables, match.Groups["table"].Value);
        }

        return tables.ToList();
    }

    private static void AddExtractedTable(HashSet<string> tables, string rawTable)
    {
        if (string.IsNullOrWhiteSpace(rawTable))
        {
            return;
        }

        var token = rawTable.Trim();
        token = token.TrimEnd(',', ';', ')');

        if (token.Length == 0)
        {
            return;
        }

        var parts = token.Split('.');
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = StripQuotes(parts[i].Trim());
        }

        var tableName = string.Join(".", parts);
        if (tableName.Length > 0)
        {
            tables.Add(tableName);
        }
    }

    private static string StripQuotes(string identifier)
    {
        if (identifier.Length >= 2)
        {
            if ((identifier[0] == '"' && identifier[^1] == '"') ||
                (identifier[0] == '[' && identifier[^1] == ']') ||
                (identifier[0] == '`' && identifier[^1] == '`'))
            {
                return identifier[1..^1];
            }
        }

        return identifier;
    }

    private static IReadOnlyList<InsightChartCard> BuildCards(
        Guid connectionId,
        string connectionName,
        DateTime? lastUpdatedUtc,
        IReadOnlyList<InsightVolumeBucket> volume,
        IReadOnlyList<TableQueryCount> topTables,
        int successfulQueries,
        int failedQueries,
        IReadOnlyList<UserQueryCount>? topUsers = null)
    {
        var cards = new List<InsightChartCard>();

        var volumePoints = volume
            .Select(v => new InsightPoint
            {
                Label = v.Date.ToString("yyyy-MM-dd"),
                Value = v.Total
            })
            .ToList();

        cards.Add(new InsightChartCard
        {
            Id = "query-volume",
            Title = "Query Volume",
            ChartType = "area",
            ConnectionId = connectionId,
            ConnectionName = connectionName,
            LastUpdatedUtc = lastUpdatedUtc,
            Series = new List<InsightSeries>
            {
                new()
                {
                    Name = "Total",
                    Points = volumePoints
                }
            }
        });

        cards.Add(new InsightChartCard
        {
            Id = "success-vs-failure",
            Title = "Success vs Failure",
            ChartType = "pie",
            ConnectionId = connectionId,
            ConnectionName = connectionName,
            LastUpdatedUtc = lastUpdatedUtc,
            Series = new List<InsightSeries>
            {
                new()
                {
                    Name = "Status",
                    Points = new List<InsightPoint>
                    {
                        new() { Label = "Success", Value = successfulQueries },
                        new() { Label = "Failed", Value = failedQueries }
                    }
                }
            }
        });

        if (topTables.Count > 0)
        {
            cards.Add(new InsightChartCard
            {
                Id = "top-tables",
                Title = "Top Tables",
                ChartType = "bar",
                ConnectionId = connectionId,
                ConnectionName = connectionName,
                LastUpdatedUtc = lastUpdatedUtc,
                Series = new List<InsightSeries>
                {
                    new()
                    {
                        Name = "Queries",
                        Points = topTables
                            .Select(t => new InsightPoint { Label = t.TableName, Value = t.QueryCount })
                            .ToList()
                    }
                }
            });
        }

        if (topUsers is { Count: > 0 })
        {
            cards.Add(new InsightChartCard
            {
                Id = "top-users",
                Title = "Top Users",
                ChartType = "bar",
                ConnectionId = connectionId,
                ConnectionName = connectionName,
                LastUpdatedUtc = lastUpdatedUtc,
                Series = new List<InsightSeries>
                {
                    new()
                    {
                        Name = "Queries",
                        Points = topUsers
                            .Select(u => new InsightPoint
                            {
                                Label = string.IsNullOrWhiteSpace(u.UserName) ? u.UserEmail : u.UserName,
                                Value = u.QueryCount
                            })
                            .ToList()
                    }
                }
            });
        }

        return cards;
    }
}
