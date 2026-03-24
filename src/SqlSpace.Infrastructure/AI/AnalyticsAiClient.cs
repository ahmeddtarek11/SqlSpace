using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSpace.Application.Abstractions.Analytics;
using SqlSpace.Application.DTOs.Analytics;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Infrastructure.AI;

public sealed class AnalyticsAiClient(
    ILogger<AnalyticsAiClient> logger,
    HttpClient httpClient,
    IOptions<llmApi> options) : IAnalyticsAiClient
{
    private readonly ILogger<AnalyticsAiClient> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private readonly IOptions<llmApi> _options = options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<Result<IReadOnlyList<ChartSuggestionDto>>> GetChartSuggestionsAsync(
        string schemaContext,
        string databaseProvider,
        string? userPrompt,
        string? ragContext,
        int maxSuggestions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(schemaContext))
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics_ai.invalid_schema", "Schema context is required."));

        if (string.IsNullOrWhiteSpace(databaseProvider))
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics_ai.invalid_provider", "Database provider is required."));

        // Normalize provider name to what the Python service expects
        var dbType = NormalizeDbType(databaseProvider);
        if (string.IsNullOrWhiteSpace(dbType))
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics_ai.unsupported_provider", $"Unsupported provider: {databaseProvider}"));

        // Build role_schema from the schema context JSON
        if (!TextToSqlClientHelpers.TryBuildRoleSchema(schemaContext, databaseProvider, out var roleSchema, out var schemaError))
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics_ai.invalid_schema", schemaError));

        if (string.IsNullOrWhiteSpace(_options.Value.BaseLink) ||
            !Uri.TryCreate(_options.Value.BaseLink, UriKind.Absolute, out _))
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics_ai.config_missing", "LlmApi BaseLink is not configured."));

        var payload = new
        {
            db_type = dbType,
            role_schema = roleSchema,
            user_prompt = userPrompt,
            rag_context = ragContext,
            max_suggestions = maxSuggestions,
        };

        _logger.LogInformation("Requesting chart suggestions. DbType: {DbType}, MaxSuggestions: {Max}",
            dbType, maxSuggestions);

        try
        {
            var endpoint = new Uri(new Uri(_options.Value.BaseLink), "/schema-to-analytics");

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };

            if (!string.IsNullOrWhiteSpace(_options.Value.ApiKey))
                httpRequest.Headers.TryAddWithoutValidation("X-API-Key", _options.Value.ApiKey);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(body))
                return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                    new Error("analytics_ai.empty_response", "AI service returned an empty response."));

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;

            if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase) &&
                root.TryGetProperty("suggestions", out var suggestionsEl))
            {
                var suggestions = new List<ChartSuggestionDto>();

                foreach (var item in suggestionsEl.EnumerateArray())
                {
                    // Normalize snake_case chart_type (e.g. "horizontal_bar") to
                    // PascalCase-compatible form by stripping underscores so that
                    // Enum.TryParse<ChartType> with ignoreCase works.
                    // Normalize snake_case chart_type → PascalCase enum → snake_case string
                    // e.g. "multi_axis_line" → strip underscores → "multiaxisline"
                    //      → Enum.TryParse → MultiAxisLine → ToSnakeCase → "multi_axis_line"
                    var rawChartType = item.TryGetProperty("chart_type", out var ct)
                        ? ct.GetString() ?? "bar"
                        : "bar";
                    var strippedChartType = rawChartType.Replace("_", "");
                    string chartTypeForFrontend;
                    if (Enum.TryParse<ChartType>(strippedChartType, ignoreCase: true, out var parsedChartType))
                        chartTypeForFrontend = ToSnakeCase(parsedChartType.ToString());
                    else
                        chartTypeForFrontend = rawChartType; // fallback to original

                    suggestions.Add(new ChartSuggestionDto
                    {
                        Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                        Description = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                        Sql = item.TryGetProperty("sql", out var s) ? s.GetString() ?? "" : "",
                        ChartType = chartTypeForFrontend,
                        ChartConfigJson = item.TryGetProperty("chart_config", out var cc)
                            ? cc.GetRawText()
                            : "{}",
                        Insight = item.TryGetProperty("insight", out var ins) ? ins.GetString() : null,
                    });
                }

                _logger.LogInformation("Received {Count} chart suggestions.", suggestions.Count);
                return suggestions;
            }

            var errorMessage = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "AI service returned an error.";
            _logger.LogWarning("Analytics AI returned error. Body: {Body}", body.Length > 300 ? body[..300] : body);
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics_ai.error", errorMessage ?? "AI service returned an error."));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call analytics AI service.");
            return Result<IReadOnlyList<ChartSuggestionDto>>.Failure(
                new Error("analytics_ai.request_failed", $"Failed to call AI service: {ex.GetType().Name}"));
        }
    }

    private static string ToSnakeCase(string pascalCase)
    {
        var sb = new System.Text.StringBuilder(pascalCase.Length + 4);
        for (var i = 0; i < pascalCase.Length; i++)
        {
            var ch = pascalCase[i];
            if (i > 0 && char.IsUpper(ch)) sb.Append('_');
            sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }

    private static string NormalizeDbType(string provider)
    {
        var normalized = provider.Trim().ToLowerInvariant();
        if (normalized is "postgres" or "postgresql")
            return "postgres";
        if (normalized is "mysql")
            return "mysql";
        if (normalized is "sqlserver" or "sql_server" or "mssql")
            return "sqlserver";

        if (Enum.TryParse<DbProviders>(provider, ignoreCase: true, out var parsed))
        {
            return parsed switch
            {
                DbProviders.PostgreSql or DbProviders.CockroachDb or DbProviders.Supabase or DbProviders.Redshift => "postgres",
                DbProviders.MySql or DbProviders.MariaDb or DbProviders.PlanetScale => "mysql",
                DbProviders.SqlServer => "sqlserver",
                _ => string.Empty,
            };
        }

        return string.Empty;
    }
}
