using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSpace.Application.Abstractions.Analytics;
using SqlSpace.Application.DTOs.Reports;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Infrastructure.AI;

public sealed class ReportAiClient(
    ILogger<ReportAiClient> logger,
    HttpClient httpClient,
    IOptions<llmApi> options) : IReportAiClient
{
    private readonly ILogger<ReportAiClient> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private readonly IOptions<llmApi> _options = options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<Result<PlanReportResponseDto>> PlanReportAsync(
        string schemaContext,
        string databaseProvider,
        string userPrompt,
        int maxSections,
        CancellationToken cancellationToken)
    {
        if (!TryGetBaseUri(out var baseUri))
            return Result<PlanReportResponseDto>.Failure(
                new Error("report_ai.config_missing", "LlmApi BaseLink is not configured."));

        if (!TextToSqlClientHelpers.TryBuildRoleSchema(schemaContext, databaseProvider, out var roleSchema, out var schemaError))
            return Result<PlanReportResponseDto>.Failure(
                new Error("report_ai.invalid_schema", schemaError));

        var dbType = NormalizeDbType(databaseProvider);
        if (string.IsNullOrWhiteSpace(dbType))
            return Result<PlanReportResponseDto>.Failure(
                new Error("report_ai.unsupported_provider", $"Unsupported provider: {databaseProvider}"));

        var payload = new
        {
            db_type = dbType,
            role_schema = roleSchema,
            user_prompt = userPrompt,
            max_sections = maxSections,
        };

        _logger.LogInformation("Requesting report plan. DbType: {DbType}, MaxSections: {Max}", dbType, maxSections);

        try
        {
            var endpoint = new Uri(baseUri, "/plan-report");
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AddApiKey(request);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(body))
                return Result<PlanReportResponseDto>.Failure(
                    new Error("report_ai.empty_response", "AI service returned an empty response."));

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                var msg = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "AI service returned an error.";
                _logger.LogWarning("Plan report AI error. Body: {Body}", body.Length > 300 ? body[..300] : body);
                return Result<PlanReportResponseDto>.Failure(
                    new Error("report_ai.error", msg ?? "AI service returned an error."));
            }

            var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "Report" : "Report";
            var summary = root.TryGetProperty("summary", out var s) ? s.GetString() : null;

            var sections = new List<PlannedSectionDto>();
            if (root.TryGetProperty("sections", out var sectionsEl))
            {
                foreach (var item in sectionsEl.EnumerateArray())
                {
                    var rawChartType = item.TryGetProperty("chart_type", out var ct) ? ct.GetString() : null;
                    sections.Add(new PlannedSectionDto
                    {
                        Heading = item.TryGetProperty("heading", out var h) ? h.GetString() ?? "" : "",
                        Sql = item.TryGetProperty("sql", out var sql) ? sql.GetString() : null,
                        ChartType = rawChartType,
                        ChartConfig = item.TryGetProperty("chart_config", out var cc) ? cc.GetRawText() : null,
                    });
                }
            }

            return new PlanReportResponseDto { Title = title, Summary = summary, Sections = sections };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call plan-report AI service.");
            return Result<PlanReportResponseDto>.Failure(
                new Error("report_ai.request_failed", $"Failed to call AI service: {ex.GetType().Name}"));
        }
    }

    public async Task<Result<string>> NarrateSectionAsync(
        string heading,
        string userPrompt,
        string? sql,
        string? sampleRowsJson,
        CancellationToken cancellationToken)
    {
        if (!TryGetBaseUri(out var baseUri))
            return Result<string>.Failure(
                new Error("report_ai.config_missing", "LlmApi BaseLink is not configured."));

        var payload = new
        {
            heading,
            user_prompt = userPrompt,
            sql = sql ?? string.Empty,
            sample_rows_json = sampleRowsJson ?? "[]",
        };

        try
        {
            var endpoint = new Uri(baseUri, "/narrate-section");
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AddApiKey(request);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                var msg = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
                _logger.LogWarning("Narrate section returned non-success. Heading: {Heading}", heading);
                return msg ?? string.Empty; // degrade gracefully — return whatever message the LLM gave
            }

            return root.TryGetProperty("narrative", out var narEl) ? narEl.GetString() ?? string.Empty : string.Empty;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call narrate-section AI service. Heading: {Heading}", heading);
            // Degrade gracefully — return empty string rather than failing the whole report
            return string.Empty;
        }
    }

    private bool TryGetBaseUri(out Uri baseUri)
    {
        baseUri = null!;
        if (string.IsNullOrWhiteSpace(_options.Value.BaseLink))
            return false;
        return Uri.TryCreate(_options.Value.BaseLink, UriKind.Absolute, out baseUri!);
    }

    private void AddApiKey(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.Value.ApiKey))
            request.Headers.TryAddWithoutValidation("X-API-Key", _options.Value.ApiKey);
    }

    private static string NormalizeDbType(string provider)
    {
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
        var n = provider.Trim().ToLowerInvariant();
        return n switch
        {
            "postgres" or "postgresql" => "postgres",
            "mysql" => "mysql",
            "sqlserver" or "mssql" => "sqlserver",
            _ => string.Empty,
        };
    }
}
