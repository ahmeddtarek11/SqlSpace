using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSpace.Application.Abstractions.AI;
using SqlSpace.Application.DTOs.AI;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Infrastructure.AI;

public sealed class TextToSqlClient(
    ILogger<TextToSqlClient> logger,
    HttpClient httpClient,
    IOptions<llmApi> options) : ITextToSqlClient
{
    private readonly ILogger<TextToSqlClient> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private readonly IOptions<llmApi> _options = options;

    public async Task<Result<SqlGenerationResponse>> SendSqlGenerationRequestAsync(
        SqlGenerationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.invalid_request", "Request payload cannot be null.", nameof(request)));
        }

        if (string.IsNullOrWhiteSpace(request.UserPrompt))
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.invalid_request", "UserPrompt cannot be empty.", nameof(request.UserPrompt)));
        }

        if (request.UserPrompt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 3)
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.invalid_request", "UserPrompt must contain at least 3 words.", nameof(request.UserPrompt)));
        }

        if (string.IsNullOrWhiteSpace(request.SchemaContext))
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.invalid_request", "SchemaContext cannot be empty.", nameof(request.SchemaContext)));
        }

        if (string.IsNullOrWhiteSpace(request.DatabaseProvider))
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.invalid_request", "DatabaseProvider is required.", nameof(request.DatabaseProvider)));
        }

        var dbType = string.Empty;
        var providerNormalized = request.DatabaseProvider.Trim().ToLowerInvariant();
        if (providerNormalized is "postgres" or "postgresql")
        {
            dbType = "postgres";
        }
        else if (providerNormalized is "mysql")
        {
            dbType = "mysql";
        }
        else if (providerNormalized is "sqlserver" or "sql_server" or "mssql")
        {
            dbType = "sqlserver";
        }
        else if (Enum.TryParse<DbProviders>(request.DatabaseProvider, ignoreCase: true, out var parsedProvider))
        {
            dbType = parsedProvider switch
            {
                DbProviders.PostgreSql => "postgres",
                DbProviders.MySql => "mysql",
                DbProviders.SqlServer => "sqlserver",
                _ => string.Empty
            };
        }

        if (string.IsNullOrWhiteSpace(dbType))
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error(
                    "llm.invalid_provider",
                    $"Unsupported database provider '{request.DatabaseProvider}'.",
                    nameof(request.DatabaseProvider)));
        }

        if (!TextToSqlClientHelpers.TryBuildRoleSchema(
                request.SchemaContext,
                request.DatabaseProvider,
                out var roleSchema,
                out var schemaError))
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.invalid_schema", schemaError));
        }

        if (string.IsNullOrWhiteSpace(_options.Value.BaseLink) ||
            !Uri.TryCreate(_options.Value.BaseLink, UriKind.Absolute, out var endpoint))
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.config_missing", "LlmApi BaseLink is not configured or is not a valid absolute URI."));
        }

        var payload = new TextToSqlRequestPayload
        {
            Question = request.UserPrompt.Trim(),
            DbType = dbType,
            RoleSchema = roleSchema
        };

        _logger.LogInformation("Sending SQL generation request to LLM. Endpoint: {Endpoint}, DbType: {DbType}, Prompt: {Prompt}",
            endpoint, dbType, request.UserPrompt.Length > 100 ? request.UserPrompt[..100] : request.UserPrompt);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload, options: TextToSqlClientHelpers.JsonOptions)
            };

            if (!string.IsNullOrWhiteSpace(_options.Value.ApiKey))
            {
                httpRequest.Headers.TryAddWithoutValidation("X-API-Key", _options.Value.ApiKey);
            }

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            _logger.LogDebug("LLM API responded. StatusCode: {StatusCode}", response.StatusCode);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("LLM API returned an empty response body. StatusCode: {StatusCode}", response.StatusCode);
                return Result<SqlGenerationResponse>.Failure(
                    new Error("llm.empty_response", "LLM API returned an empty response."));
            }

            var parsed = TextToSqlClientHelpers.ParseResponse(body, out var sql, out var apiError);

            if (parsed == TextToSqlResponseKind.Success && !string.IsNullOrWhiteSpace(sql))
            {
                _logger.LogInformation("LLM SQL generation succeeded. GeneratedSql length: {SqlLength}", sql.Length);
                return new SqlGenerationResponse
                {
                    Success = true,
                    GeneratedSql = sql,
                    Explanation = string.Empty,
                    IsValidSql = true,
                    TablesReferenced = Array.Empty<string>(),
                    ErrorMessage = null
                };
            }

            if (apiError is not null)
            {
                var code = string.IsNullOrWhiteSpace(apiError.ErrorCode)
                    ? "llm.error"
                    : $"llm.{apiError.ErrorCode.ToLowerInvariant()}";

                var message = string.IsNullOrWhiteSpace(apiError.Message)
                    ? "LLM API returned an error."
                    : apiError.Message;

                _logger.LogWarning("LLM API returned an error response. Code: {Code}, Message: {Message}", code, message);
                return Result<SqlGenerationResponse>.Failure(
                    new Error(code, message, apiError.ErrorSubcode));
            }

            _logger.LogWarning("LLM API response was not recognized. Body snippet: {Body}", body.Length > 200 ? body[..200] : body);
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.unexpected_response", "LLM API response was not recognized."));
        }




        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }




        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call LLM API.");
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.request_failed", $"Failed to call LLM API: {ex.GetType().Name}"));
        }
    }
}
