using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSpace.Application.Abstractions.AI;
using SqlSpace.Application.DTOs.AI;
using SqlSpace.Domain.Common.Results;

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
        if (!TextToSqlClientHelpers.TryValidateRequest(request, out var validationError))
        {
            return Result<SqlGenerationResponse>.Failure(validationError);
        }

        if (!TextToSqlClientHelpers.TryMapDbType(request.DatabaseProvider, out var dbType))
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error(
                    "llm.invalid_provider",
                    $"Unsupported database provider '{request.DatabaseProvider}'.",
                    nameof(request.DatabaseProvider)));
        }

        if (!TextToSqlClientHelpers.TryBuildRoleSchema(
                request.SchemaContext,
                out var roleSchema,
                out var schemaError))
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.invalid_schema", schemaError));
        }

        var payload = new TextToSqlRequestPayload
        {
            Question = request.UserPrompt,
            DbType = dbType,
            RoleSchema = roleSchema
        };

        var endpoint = TextToSqlClientHelpers.ResolveEndpoint(_options.Value, _httpClient.BaseAddress);
        if (endpoint is null)
        {
            return Result<SqlGenerationResponse>.Failure(
                new Error("llm.config_missing", "LlmApi BaseLink is not configured."));
        }

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
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(body))
            {
                return Result<SqlGenerationResponse>.Failure(
                    new Error("llm.empty_response", "LLM API returned an empty response."));
            }

            var parsed = TextToSqlClientHelpers.ParseResponse(body, out var sql, out var apiError);

            if (parsed == TextToSqlResponseKind.Success && !string.IsNullOrWhiteSpace(sql))
            {
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
                return Result<SqlGenerationResponse>.Failure(TextToSqlClientHelpers.ToError(apiError));
            }

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
