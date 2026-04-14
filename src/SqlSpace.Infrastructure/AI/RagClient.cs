using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSpace.Application.Abstractions.SecurityAndLLM;
using SqlSpace.Application.DTOs.RAG;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Infrastructure.AI;

public class RagClient(HttpClient httpClient, IOptions<RagApiOptions> options, ILogger<RagClient> logger) : IRagClient
{
    private readonly ILogger<RagClient> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private readonly RagApiOptions _options = options.Value;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Ingest ────────────────────────────────────────────────────────────────

    public async Task<Result<RagIngestResultDto>> IngestDocumentAsync(
        string tenantId,
        string uploadedBy,
        string uploaderRole,
        string[] allowedRoles,
        RagFileUploadDto file,
        CancellationToken cancellationToken)
    {
        // --- validation ---
        if (string.IsNullOrWhiteSpace(tenantId))
            return Result<RagIngestResultDto>.Failure(
                new Error("rag.invalid_request", "tenantId cannot be empty.", nameof(tenantId)));

        if (string.IsNullOrWhiteSpace(uploadedBy))
            return Result<RagIngestResultDto>.Failure(
                new Error("rag.invalid_request", "uploadedBy cannot be empty.", nameof(uploadedBy)));

        if (string.IsNullOrWhiteSpace(uploaderRole))
            return Result<RagIngestResultDto>.Failure(
                new Error("rag.invalid_request", "uploaderRole cannot be empty.", nameof(uploaderRole)));

        if (allowedRoles is null || allowedRoles.Length == 0)
            return Result<RagIngestResultDto>.Failure(
                new Error("rag.invalid_request", "allowedRoles must contain at least one role.", nameof(allowedRoles)));

        if (file is null)
            return Result<RagIngestResultDto>.Failure(
                new Error("rag.invalid_request", "File cannot be null.", nameof(file)));

        if (file.Length == 0)
            return Result<RagIngestResultDto>.Failure(
                new Error("rag.invalid_request", "File is empty.", nameof(file)));

        var maxBytes = (long)_options.MaxUploadSizeMb * 1024 * 1024;
        if (file.Length > maxBytes)
            return Result<RagIngestResultDto>.Failure(
                new Error("rag.file_too_large", $"File exceeds the {_options.MaxUploadSizeMb} MB limit.", nameof(file)));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = _options.AllowedExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();

        if (!allowedExtensions.Contains(extension))
            return Result<RagIngestResultDto>.Failure(
                new Error("rag.invalid_file_type",
                    $"File type '{extension}' is not allowed. Accepted: {_options.AllowedExtensions}.",
                    nameof(file)));

        // --- build multipart form ---
        using var form = new MultipartFormDataContent();

        var fileContent = new StreamContent(file.Content);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "file", file.FileName);

        form.Add(new StringContent(tenantId),                                "tenant_id");
        form.Add(new StringContent(uploadedBy),                              "uploaded_by");
        form.Add(new StringContent(uploaderRole),                            "uploader_role");
        form.Add(new StringContent(JsonSerializer.Serialize(allowedRoles)),  "allowed_roles");
        form.Add(new StringContent("{}"),                                    "metadata");

        // --- send request ---
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/rag/ingest")
            {
                Content = form
            };
            AddApiKeyHeader(request);

            _logger.LogInformation(
                "Sending ingest request to RAG service. TenantId: {TenantId}, File: {FileName}, Size: {Size}B",
                tenantId, file.FileName, file.Length);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("RAG ingest responded. StatusCode: {StatusCode}", response.StatusCode);

            if (string.IsNullOrWhiteSpace(body))
                return Result<RagIngestResultDto>.Failure(
                    new Error("rag.empty_response", "RAG service returned an empty response."));

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<RagIngestResultDto>(body, _jsonOptions);
                if (result is null)
                    return Result<RagIngestResultDto>.Failure(
                        new Error("rag.parse_error", "Could not parse the RAG ingest response."));

                _logger.LogInformation(
                    "RAG ingest succeeded. FileId: {FileId}, Chunks: {Chunks}",
                    result.FileId, result.ChunksCreated);

                return Result<RagIngestResultDto>.Success(result);
            }

            return ParseErrorResponse<RagIngestResultDto>(body);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call RAG ingest endpoint.");
            return Result<RagIngestResultDto>.Failure(
                new Error("rag.request_failed", $"Failed to reach RAG service: {ex.GetType().Name}"));
        }
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public async Task<Result<RagQueryResultDto>> QueryAsync(
        string tenantId,
        string userRole,
        string query,
        int topK = 5,
        string[]? fileIds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return Result<RagQueryResultDto>.Failure(
                new Error("rag.invalid_request", "tenantId cannot be empty.", nameof(tenantId)));

        if (string.IsNullOrWhiteSpace(userRole))
            return Result<RagQueryResultDto>.Failure(
                new Error("rag.invalid_request", "userRole cannot be empty.", nameof(userRole)));

        if (string.IsNullOrWhiteSpace(query))
            return Result<RagQueryResultDto>.Failure(
                new Error("rag.invalid_request", "query cannot be empty.", nameof(query)));

        // --- build JSON payload ---
        var payload = new RagQueryPayload
        {
            TenantId  = tenantId,
            UserRole  = userRole,
            Query     = query,
            TopK      = topK,
            Filters   = new RagQueryFilters { FileIds = fileIds ?? [] }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/rag/query")
            {
                Content = JsonContent.Create(payload, options: _jsonOptions)
            };
            AddApiKeyHeader(request);

            _logger.LogInformation(
                "Sending query to RAG service. TenantId: {TenantId}, Role: {Role}, TopK: {TopK}",
                tenantId, userRole, topK);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("RAG query responded. StatusCode: {StatusCode}", response.StatusCode);

            if (string.IsNullOrWhiteSpace(body))
                return Result<RagQueryResultDto>.Failure(
                    new Error("rag.empty_response", "RAG service returned an empty response."));

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<RagQueryResultDto>(body, _jsonOptions);
                if (result is null)
                    return Result<RagQueryResultDto>.Failure(
                        new Error("rag.parse_error", "Could not parse the RAG query response."));

                _logger.LogInformation("RAG query succeeded. Sources: {SourceCount}", result.Sources.Count);
                return Result<RagQueryResultDto>.Success(result);
            }

            return ParseErrorResponse<RagQueryResultDto>(body);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call RAG query endpoint.");
            return Result<RagQueryResultDto>.Failure(
                new Error("rag.request_failed", $"Failed to reach RAG service: {ex.GetType().Name}"));
        }
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    public async Task<Result<bool>> DeleteFileAsync(
        string tenantId,
        string fileId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return Result<bool>.Failure(
                new Error("rag.invalid_request", "tenantId cannot be empty.", nameof(tenantId)));

        if (string.IsNullOrWhiteSpace(fileId))
            return Result<bool>.Failure(
                new Error("rag.invalid_request", "fileId cannot be empty.", nameof(fileId)));

        var encodedFileId = Uri.EscapeDataString(fileId);
        var encodedTenantId = Uri.EscapeDataString(tenantId);

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/rag/files/{encodedFileId}?tenant_id={encodedTenantId}");

            AddApiKeyHeader(request);

            _logger.LogInformation(
                "Sending delete request to RAG service. TenantId: {TenantId}, FileId: {FileId}",
                tenantId, fileId);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("RAG delete responded. StatusCode: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
                return Result<bool>.Success(true);

            if (string.IsNullOrWhiteSpace(body))
                return Result<bool>.Failure(
                    new Error("rag.empty_response", "RAG service returned an empty response."));

            return ParseErrorResponse<bool>(body);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call RAG delete endpoint.");
            return Result<bool>.Failure(
                new Error("rag.request_failed", $"Failed to reach RAG service: {ex.GetType().Name}"));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddApiKeyHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.InternalApiKey))
            request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", _options.InternalApiKey);
    }

    private Result<T> ParseErrorResponse<T>(string body)
    {
        try
        {
            var error = JsonSerializer.Deserialize<RagErrorResponseDto>(body, _jsonOptions);
            if (error is not null && !string.IsNullOrWhiteSpace(error.ErrorCode))
            {
                var code = $"rag.{error.ErrorCode.ToLowerInvariant()}";
                _logger.LogWarning("RAG service returned error. Code: {Code}, Message: {Message}", code, error.Message);
                return Result<T>.Failure(new Error(code, error.Message, error.ErrorSubcode));
            }
        }
        catch (JsonException) { /* fall through to generic error */ }

        _logger.LogWarning("RAG service returned unrecognised error body: {Snippet}",
            body.Length > 200 ? body[..200] : body);

        return Result<T>.Failure(new Error("rag.unexpected_response", "RAG service returned an unexpected error."));
    }

    // ── Internal payload models ───────────────────────────────────────────────

  

    
}
  