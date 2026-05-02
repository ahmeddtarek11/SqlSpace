# SqlSpace

AI-powered business intelligence platform for small business owners. Connect your database or store, ask questions in plain language, and get insights, charts, and reports — no SQL knowledge required.

> **⚠️ Work in Progress**: This project is **not finished**. It is under continuous development — many features are still being built, some functionality is incomplete, and there will be bugs. Expect breaking changes as the codebase evolves.

## What It Does

- **Natural Language Queries** — Ask business questions in plain English, get SQL-backed answers with AI explanations
- **Auto Analytics** — Connects to your database schema and suggests relevant charts and insights automatically
- **Multi-Database Support** — PostgreSQL, MySQL, SQL Server
- **Role-Based Access Control** — Connection-scoped permissions with full audit logging
- **Query History & Saved Queries** — Track and reuse past analyses

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18, TypeScript, Tailwind CSS, Chart.js |
| Backend API | ASP.NET Core (.NET 10), Clean Architecture |
| AI Service | Python, FastAPI, Google Gemini 2.5 Flash |
| SQL Validation | sqlglot (parse, validate, and dialect-translate SQL) |
| Database | PostgreSQL, EF Core |
| Auth | JWT + Refresh Tokens |

## Project Structure

```
SqlSpace/
├── src/
│   ├── SqlSpace.Api/              # REST API controllers, middleware, config
│   ├── SqlSpace.Application/      # Business logic, services, DTOs
│   ├── SqlSpace.Domain/           # Entities, enums, domain models
│   └── SqlSpace.Infrastructure/   # EF Core, external service clients
├── tests/
│   └── SqlSpace.Application.Tests/
├── FrontEnd/
│   └── sqlspace-frontend/         # React + TypeScript SPA
├── AI-based_DSS/                  # Python AI microservice (gitignored — separate repo)
│   └── ai_services/
│       ├── txt_to_sql/            # Text-to-SQL generation + validation
│       ├── schema_to_analytics/   # Auto chart suggestion engine
│       └── rag/                   # RAG knowledge base (in progress)
└── ROADMAP.md                     # Full product roadmap
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 18+
- Python 3.11+
- PostgreSQL 15+

### Backend (.NET)

```bash
cd src/SqlSpace.Api
# Configure appsettings.Development.json with your DB connection and JWT secret
dotnet run
```

### Frontend (React)

```bash
cd FrontEnd/sqlspace-frontend
npm install
npm run dev
```



## Configuration

Copy the example configs and fill in your values:

- **Backend**: `src/SqlSpace.Api/appsettings.Development.json` — DB connection string, JWT secret
- **AI Service**: `AI-based_DSS/.env` — `GEMINI_API_KEY`
- **Frontend**: `FrontEnd/sqlspace-frontend/.env` — API base URL






