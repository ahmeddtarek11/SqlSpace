using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SqlSpace.Application.DTOs.RAG;

  public sealed class RagQueryPayload
    {
        [JsonPropertyName("tenant_id")]  public string TenantId { get; set; } = string.Empty;
        [JsonPropertyName("user_role")]  public string UserRole  { get; set; } = string.Empty;
        [JsonPropertyName("query")]      public string Query     { get; set; } = string.Empty;
        [JsonPropertyName("top_k")]      public int    TopK      { get; set; } = 5;
        [JsonPropertyName("filters")]    public RagQueryFilters Filters { get; set; } = new();
    }

    public sealed class RagQueryFilters
    {
        [JsonPropertyName("file_ids")]   public string[] FileIds { get; set; } = [];
    }