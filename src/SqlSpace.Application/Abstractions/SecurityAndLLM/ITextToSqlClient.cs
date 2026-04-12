using SqlSpace.Application.DTOs.AI;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.AI;

/// <summary>
/// Defines the outbound contract for communicating with the FastAPI LLM service that generates SQL.
/// </summary>
/// <remarks>
/// Usage:
/// - Called by the application use case that handles natural-language query requests.
/// - Implemented in Infrastructure using <see cref="HttpClient"/>.
///
/// When:
/// - After schema context is prepared and filtered for the current user's permissions.
///
/// Why:
/// - To isolate external AI integration details (HTTP route, serialization, retries) from business logic.
///
/// Where:
/// - Interface belongs to the abstraction layer consumed by application workflows.
/// - Implemented in Infrastructure.
///
/// How:
/// - Build request payload from prompt + filtered schema + provider.
/// - Send HTTP POST to configured FastAPI endpoint.
/// - Parse response and return normalized SQL generation result.
/// </remarks>
public interface ITextToSqlClient
{
    /// <summary>
    /// Sends one SQL-generation request to FastAPI and returns the generated SQL payload.
    /// </summary>
    /// <param name="request">The prompt context containing user input, filtered schema JSON, and DB provider.</param>
    /// <param name="cancellationToken">Cancellation token propagated from the API/application pipeline.</param>
    /// <returns>Structured SQL generation response including SQL and optional model/token metadata.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate that required input fields are present (UserPrompt, SchemaContext, DatabaseProvider).
    /// 2. Serialize the request body to JSON.
    /// 3. Send HTTP POST request to the configured FastAPI route (e.g., http://localhost:8000/process-prompt).
    /// 4. Apply timeout (30 seconds) and retry policy for transient failures (Polly library).
    /// 5. Parse the response body JSON.
    /// 6. Return normalized SqlGenerationResponse or throw for unrecoverable errors.
    /// </remarks>
    Task<Result<SqlGenerationResponse>> SendSqlGenerationRequestAsync(
        SqlGenerationRequest request, 
        CancellationToken cancellationToken);
}
