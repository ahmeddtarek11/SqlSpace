# Part A — Rich Reports: Implementation Tasks

**Goal:** Add a Reports feature to the analytics page. One NL prompt → LLM plans sections → C# runs each SQL → LLM writes narrative grounded in real data. Draft is ephemeral; user clicks Save to persist. Saved reports are re-runnable.

**Context files to read before continuing:**
- Plan/architecture doc: [docs/reports-and-extended-rag.md](docs/reports-and-extended-rag.md)
- Entity pattern: [src/SqlSpace.Domain/Models/SavedChart.cs](src/SqlSpace.Domain/Models/SavedChart.cs)
- AI client pattern: [src/SqlSpace.Infrastructure/AI/AnalyticsAiClient.cs](src/SqlSpace.Infrastructure/AI/AnalyticsAiClient.cs)
- Service pattern: [src/SqlSpace.Application/Services/Analytics/ChartService.cs](src/SqlSpace.Application/Services/Analytics/ChartService.cs)
- Controller pattern: [src/SqlSpace.Api/Controllers/AnalyticsController.cs](src/SqlSpace.Api/Controllers/AnalyticsController.cs)
- DI (Application): [src/SqlSpace.Application/DependencyInjection.cs](src/SqlSpace.Application/DependencyInjection.cs)
- DI (Infrastructure): [src/SqlSpace.Infrastructure/DependencyInjection.cs](src/SqlSpace.Infrastructure/DependencyInjection.cs)
- Frontend analytics: [FrontEnd/sqlspace-frontend/src/pages/AnalyticsPage.tsx](FrontEnd/sqlspace-frontend/src/pages/AnalyticsPage.tsx)

**Key patterns from codebase:**
- Services inject: `IApplicationDbContext`, `IAccessControlService`, `ISchemaContextService` (for filtered schema), `IDatabaseExecutor` (for running SQL), `ILogger`.
- `IDatabaseExecutor.ExecuteQueryAsync(ConnectedDatabase, sql, ct)` → `DatabaseQueryResult` (`Success`, `ResultsJson`, `RowsReturned`, `ExecutionTimeMs`, `ErrorMessage`).
- `ISchemaContextService.GetFilteredSchemaForPromptAsync(connectionId, userId, null, ct)` → filtered schema JSON string.
- ChartType enum is in `SqlSpace.Domain.Enums`. Stored as string in EF with `HasConversion<string>()`.
- `ConnectedDatabase` navigation: uses `WithMany()` (no collection nav) for entities added later (see `KnowledgeChatMessageConfiguration`).
- AI client HTTP calls use `IOptions<llmApi>` (BaseLink, ApiKey, TimeoutSeconds).
- Controller base: `ApiController` → `ToApiResponse(result, statusCode, message)`.
- `Result<T>` pattern: `Result<T>.Success(value)`, `Result<T>.Failure(new Error("code","msg"))`.
- All services registered as `AddScoped`, singleton only for `ISqlValidator`.

---

## Backend Tasks

### [x] DONE: 1. Domain entities
- [x] `src/SqlSpace.Domain/Models/Report.cs`
- [x] `src/SqlSpace.Domain/Models/ReportSection.cs`

### [x] DONE: 2. EF Configuration
- [x] `src/SqlSpace.Infrastructure/Data/Configuration/ReportConfiguration.cs`
- [x] `src/SqlSpace.Infrastructure/Data/Configuration/ReportSectionConfiguration.cs`

### [x] DONE: 3. DbContext registration
- [x] Add `DbSet<Report>` and `DbSet<ReportSection>` to `IApplicationDbContext`
- [x] Add `DbSet<Report>` and `DbSet<ReportSection>` to `ApplicationDbContext`

### [x] DONE: 4. DTOs
- [x] `src/SqlSpace.Application/DTOs/Reports/ReportSectionDto.cs`
- [x] `src/SqlSpace.Application/DTOs/Reports/ReportDraftDto.cs`
- [x] `src/SqlSpace.Application/DTOs/Reports/ReportDto.cs`
- [x] `src/SqlSpace.Application/DTOs/Reports/CreateReportRequest.cs`
- [x] `src/SqlSpace.Application/DTOs/Reports/PlanReportResponseDto.cs` (AI wire shapes)

### [x] DONE: 5. IReportAiClient interface + ReportAiClient implementation
- [x] `src/SqlSpace.Application/Abstractions/Analytics/IReportAiClient.cs`
- [x] `src/SqlSpace.Infrastructure/AI/ReportAiClient.cs`
- Calls Python `/plan-report` and `/narrate-section`
- Registered with `AddHttpClient<IReportAiClient, ReportAiClient>` in Infrastructure DI

### [x] DONE: 6. IReportService interface + ReportService implementation
- [x] `src/SqlSpace.Application/Abstractions/Reports/IReportService.cs`
- [x] `src/SqlSpace.Application/Services/Reports/ReportService.cs`
- Methods: DraftAsync, SaveAsync, GetAsync, ListAsync, RefreshAsync, DeleteAsync
- Registered in Application DI

### [x] DONE: 7. ReportsController
- [x] `src/SqlSpace.Api/Controllers/ReportsController.cs`
- Route: `api/connections/{connectionId:guid}/reports`
- Endpoints: POST /draft, POST /, GET /, GET /{id}, POST /{id}/refresh, DELETE /{id}

### [ ] TODO: 8. EF Migration
- Run: `dotnet ef migrations add AddReports --project src/SqlSpace.Infrastructure --startup-project src/SqlSpace.Api`
- Then: `dotnet ef database update --project src/SqlSpace.Infrastructure --startup-project src/SqlSpace.Api`
- **Blocked**: requires API process to be stopped first (DLL lock issue from prior sessions)

---

## Frontend Tasks

### [x] DONE: 9. TypeScript types
- [x] Add `ReportSectionDto`, `ReportDraftDto`, `ReportDto`, `CreateReportRequest` to `src/types/index.ts`

### [x] DONE: 10. API client
- [x] `FrontEnd/sqlspace-frontend/src/api/reports.ts`
- Methods: draft, save, list, get, refresh, remove

### [x] DONE: 11. Zustand store
- [x] `FrontEnd/sqlspace-frontend/src/stores/reports-store.ts`

### [x] DONE: 12. Components
- [x] `FrontEnd/sqlspace-frontend/src/components/analytics/reports/ReportPromptInput.tsx`
- [x] `FrontEnd/sqlspace-frontend/src/components/analytics/reports/ReportView.tsx`
- [x] `FrontEnd/sqlspace-frontend/src/components/analytics/reports/ReportToolbar.tsx`
- [x] `FrontEnd/sqlspace-frontend/src/components/analytics/reports/ReportsTab.tsx`

### [x] DONE: 13. Wire Reports tab into AnalyticsPage
- [x] Add tab switcher (Charts | Reports) to `AnalyticsPage.tsx`
- [x] Mount `<ReportsTab>` when Reports tab active

### [ ] TODO: 14. Type-check frontend
- `cd FrontEnd/sqlspace-frontend && npx tsc --noEmit`
- Fix any type errors before marking done

---

## Python Tasks (to be done separately — out of scope for this agent)

- [ ] Add `POST /plan-report` endpoint
  - Input: `{ db_type, role_schema, user_prompt, max_sections (default 5) }`
  - Output: `{ title, summary, sections: [{ heading, sql, chart_type, chart_config }] }`
- [ ] Add `POST /narrate-section` endpoint
  - Input: `{ heading, user_prompt, sql, sample_rows_json }`
  - Output: `{ narrative }` — 2-4 sentences grounded in real data
- [ ] Both endpoints use the same LLM as other endpoints

---

## Status Legend
- `[x] DONE` — completed
- `[ ] TODO` — not started
- `[~] IN PROGRESS` — currently being worked on
