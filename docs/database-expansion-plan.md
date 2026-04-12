# SqlSpace — Database Expansion Plan

A per-database breakdown of what it takes to add support for 11 new databases beyond the current PostgreSQL / SQL Server / MySQL trio.

## Current Architecture Summary

Every new database provider requires touching **5 touch-points**:

| # | File | What changes |
|---|------|--------------|
| 1 | `SqlSpace.Domain/Enums/DbProviders.cs` | Add enum value + extension method cases (`GetDefaultPort`, `GetDefaultSchema`, `SupportsSchemas`) |
| 2 | `SqlSpace.Infrastructure/Connection/ConnectionStringBuilderHelper.cs` | Add template, SSL param string, and parameter name tuple |
| 3 | `SqlSpace.Infrastructure/Connection/DbConnectionFactory.cs` | Add `new XyzConnection(connectionString)` case in switch |
| 4 | `SqlSpace.Infrastructure/integration/SchemaExtractor.cs` | Add provider-specific schema introspection SQL |
| 5 | `SqlSpace.Infrastructure/SqlSpace.Infrastructure.csproj` | Add the NuGet ADO.NET driver package |

Optionally: the frontend `ConnectionsPage` / `NewConnectionPage` provider dropdown needs the new enum value exposed via the API.

---

## Effort Scale

| Label | Meaning |
|-------|---------|
| **XS** | Copy-paste of existing SQL provider pattern. 1–2 hours. |
| **S** | Minor structural differences (schema-less, different port convention). Half a day. |
| **M** | Non-standard INFORMATION_SCHEMA or driver-level quirks. 1–2 days. |
| **L** | Fundamentally different query model (document, columnar, NoSQL). Requires architectural additions. 3–5 days. |
| **XL** | Outside ADO.NET paradigm; new abstraction layer needed. 1–2 weeks. |

---

## 1. SQLite

**Why add it?**
SQLite is the default embedded database for local development, prototyping, testing, and single-file apps. Huge developer audience. Zero server setup.

**How to implement:**
- NuGet: `Microsoft.Data.Sqlite`
- `DbConnection` subclass: `SqliteConnection`
- Connection string: `Data Source=/path/to/db.sqlite` (file path instead of host/port/database)
- Schema introspection: SQLite has no `INFORMATION_SCHEMA`. Use `PRAGMA table_list` + `PRAGMA table_info(table_name)` + `PRAGMA foreign_key_list(table_name)` in a multi-step fetch loop.

**Backend change scope:**
- `DbProviders.cs`: add `SQLite = 4`, no default port needed (file-based).
- `ConnectionStringBuilderHelper.cs`: template is just `Data Source={0}` — the `host/port/database` model doesn't map cleanly. The `BuildConnectionString` method needs an optional **bypass mode** for file-path-based databases (or treat `host` field as the file path).
- `DbConnectionFactory.cs`: one new `case` with `SqliteConnection`.
- `SchemaExtractor.cs`: custom multi-query PRAGMA loop — cannot reuse `INFORMATION_SCHEMA` path. Needs a new code path in `GetSchemaQuery` that returns sentinel + a second method for PRAGMA-based extraction.
- UI: connection form needs to show a "File path" field instead of host/port.

**Technical cost: S–M**
The driver and enum addition is trivial (XS), but the schema extraction and connection string model mismatch bumps this to S–M. The file-path model is a notable deviation from the host:port:database abstraction.

---

## 2. MariaDB

**Why add it?**
MariaDB is a drop-in MySQL fork used by thousands of self-hosted apps (WordPress, Joomla, Nextcloud, etc.). If you already support MySQL, you support ~80% of MariaDB's syntax for free.

**How to implement:**
- NuGet: **same `MySqlConnector`** — `MySqlConnector` explicitly supports MariaDB.
- `DbConnection` subclass: `MySqlConnection` (identical to MySQL path).
- Connection string: identical to MySQL.
- Schema introspection: MariaDB supports `INFORMATION_SCHEMA` identically to MySQL. The MySQL query in `SchemaExtractor.cs` works without modification.

**Backend change scope:**
- `DbProviders.cs`: add `MariaDb = 5`, default port `3306`, no schema support (same as MySQL).
- `ConnectionStringBuilderHelper.cs`: reuse MySQL template and SSL param verbatim.
- `DbConnectionFactory.cs`: `new MySqlConnection(connectionString)` — identical case body to MySQL.
- `SchemaExtractor.cs`: reuse MySQL query string verbatim — point `MariaDb` case at the same SQL.

**Technical cost: XS**
This is the cheapest expansion possible. Every code change is a one-liner pointing to existing MySQL implementations.

---

## 3. CockroachDB

**Why add it?**
CockroachDB is a distributed SQL database that speaks the PostgreSQL wire protocol. It's popular in cloud-native architectures that need global distribution and horizontal scaling without sacrificing SQL.

**How to implement:**
- NuGet: **same `Npgsql`** — CockroachDB uses the PostgreSQL wire protocol natively.
- Connection string: identical to PostgreSQL format. Default port: `26257`.
- Schema introspection: CockroachDB supports `INFORMATION_SCHEMA` and is highly compatible with the PostgreSQL schema query. Minor caveat: `pg_catalog` system tables behave differently, but the current PostgreSQL query (which filters out `pg_catalog` and `information_schema`) already handles this correctly.

**Backend change scope:**
- `DbProviders.cs`: add `CockroachDb = 6`, default port `26257`, default schema `public`.
- `ConnectionStringBuilderHelper.cs`: reuse PostgreSQL template and SSL param.
- `DbConnectionFactory.cs`: `new NpgsqlConnection(connectionString)` — identical case body to PostgreSQL.
- `SchemaExtractor.cs`: reuse PostgreSQL query. At most filter `crdb_internal` system schema.

**Technical cost: XS**
Even cheaper than MariaDB in one sense — you're literally reusing the Npgsql driver. The only "work" is wiring the enum and the 26257 port.

---

## 4. PlanetScale

**Why add it?**
PlanetScale is a serverless MySQL-compatible database built on Vitess. It's popular for scaling MySQL workloads on modern cloud infrastructure (especially in the Rails/Next.js ecosystem).

**How to implement:**
- NuGet: **same `MySqlConnector`** — PlanetScale speaks MySQL protocol.
- Connection string: MySQL format. Default port `3306` (or 443 over HTTP driver). SSL is **mandatory** on PlanetScale.
- Schema introspection: PlanetScale supports `INFORMATION_SCHEMA`, but **foreign keys are not enforced** (Vitess disables them). The FK section of the MySQL schema query will return empty results — this is expected, not a bug.
- Note: PlanetScale also offers an HTTP-based driver (`@planetscale/database`) but that is a Node.js construct; the MySQL TCP driver works fine from .NET.

**Backend change scope:**
- `DbProviders.cs`: add `PlanetScale = 7`, default port `3306`, no schema support.
- `ConnectionStringBuilderHelper.cs`: reuse MySQL template; force `SslMode=Required` regardless of `useSSL` parameter (PlanetScale requires it).
- `DbConnectionFactory.cs`: `new MySqlConnection(connectionString)`.
- `SchemaExtractor.cs`: reuse MySQL query (FK results will be empty — acceptable).

**Technical cost: XS–S**
The forced SSL and missing FK metadata are the only quirks. Everything else delegates to the MySQL path.

---

## 5. Supabase

**Why add it?**
Supabase is a hosted PostgreSQL platform with a large developer following. It is literally PostgreSQL under the hood — every Supabase project is a standard Postgres instance.

**How to implement:**
- NuGet: **same `Npgsql`**.
- Connection string: standard PostgreSQL format. Supabase connection strings include a `?pgbouncer=true` parameter when using the connection pooler. Default port `5432` (direct) or `6543` (pooler).
- Schema introspection: the existing PostgreSQL schema query works perfectly. Supabase adds system schemas like `auth`, `storage`, `realtime` — the `WHERE c.table_schema NOT IN ('pg_catalog', 'information_schema')` filter should be extended to also exclude `auth`, `storage`, `realtime`, `graphql`, `graphql_public`, `supabase_migrations`.

**Backend change scope:**
- `DbProviders.cs`: add `Supabase = 8`, default port `5432`, default schema `public`.
- `ConnectionStringBuilderHelper.cs`: reuse PostgreSQL template; optionally hard-code SSL required.
- `DbConnectionFactory.cs`: `new NpgsqlConnection(connectionString)`.
- `SchemaExtractor.cs`: reuse PostgreSQL query with extended schema exclusion list for Supabase system schemas.

**Technical cost: XS**
Pure routing exercise. The schema exclusion tweak is one string addition.

---

## 6. DuckDB

**Why add it?**
DuckDB is an in-process analytical database (OLAP) that runs locally with no server. It's extremely fast for analytical queries on CSV/Parquet files and is gaining rapid adoption in the data engineering space.

**How to implement:**
- NuGet: `DuckDB.NET.Data` (official .NET ADO.NET binding)
- `DbConnection` subclass: `DuckDBConnection`
- Connection string: `Data Source=/path/to/db.duckdb` or `Data Source=:memory:` (file-based like SQLite).
- Schema introspection: DuckDB has `INFORMATION_SCHEMA` support as of v0.9+. The PostgreSQL-style query mostly works. Use:
  ```sql
  SELECT table_schema, table_name, table_type, column_name, data_type,
         is_nullable, character_maximum_length, NULL, NULL, NULL, NULL, NULL
  FROM information_schema.columns
  JOIN information_schema.tables USING (table_schema, table_name)
  WHERE table_schema NOT IN ('information_schema', 'pg_catalog')
  ORDER BY table_schema, table_name, ordinal_position
  ```
  FK introspection via `INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS` is not available in DuckDB — FK columns return NULL (acceptable).

**Backend change scope:**
- `DbProviders.cs`: add `DuckDb = 9`, no port (file-based), default schema `main`.
- `ConnectionStringBuilderHelper.cs`: file-path model like SQLite (same bypass issue as SQLite).
- `DbConnectionFactory.cs`: `new DuckDBConnection(connectionString)`.
- `SchemaExtractor.cs`: new query case with no FK columns.

**Technical cost: S**
Same file-path model challenge as SQLite. Schema query needs a custom case but is simpler than SQLite's PRAGMA approach since DuckDB has partial INFORMATION_SCHEMA support.

---

## 7. Amazon Redshift

**Why add it?**
Redshift is AWS's managed data warehouse. It's PostgreSQL 8.x-compatible and widely used for BI workloads. Many teams run their analytics data in Redshift and want to query it with natural language.

**How to implement:**
- NuGet: `Npgsql` works via the PostgreSQL wire protocol. Amazon also publishes `Amazon.Redshift.Jdbc42.Driver` but that is Java. For .NET use Npgsql.
- Connection string: PostgreSQL format. Default port `5439`.
- Schema introspection: Redshift supports `INFORMATION_SCHEMA` but has quirks:
  - `information_schema.referential_constraints` exists but may be empty (Redshift doesn't enforce FKs).
  - Some system schemas to exclude: `pg_catalog`, `pg_toast`, `pg_internal`.
  - The current PostgreSQL query works with minor schema exclusion adjustments.

**Backend change scope:**
- `DbProviders.cs`: add `Redshift = 10`, default port `5439`, default schema `public`.
- `ConnectionStringBuilderHelper.cs`: reuse PostgreSQL template; SSL typically required (`SSL Mode=Require`).
- `DbConnectionFactory.cs`: `new NpgsqlConnection(connectionString)`.
- `SchemaExtractor.cs`: reuse PostgreSQL query with Redshift system schema exclusions.

**Technical cost: XS–S**
Almost identical to CockroachDB/Supabase. The non-standard port (5439) and Redshift-specific schema exclusions are the only meaningful deltas.

---

## 8. Snowflake

**Why add it?**
Snowflake is the dominant cloud data warehouse for enterprise data teams. Supporting Snowflake would make SqlSpace viable for data analysts at large companies.

**How to implement:**
- NuGet: `Snowflake.Data` (official Snowflake .NET ADO.NET driver)
- `DbConnection` subclass: `SnowflakeDbConnection`
- Connection string: `account=myaccount.us-east-1;user=myuser;password=mypass;db=mydb;schema=public;warehouse=mywarehouse`
  - The `account` identifier replaces `host` and encodes the region.
  - No port concept — always HTTPS.
  - `warehouse` is an additional required parameter.
- Schema introspection: Snowflake has full `INFORMATION_SCHEMA` support. The PostgreSQL query structure is reusable with adaptations:
  - Replace `table_schema NOT IN ('pg_catalog', 'information_schema')` with Snowflake's system databases filter.
  - FK detection works via `INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS`.
  - Column names are the same as standard SQL.

**Backend change scope:**
- `DbProviders.cs`: add `Snowflake = 11`, no port, default schema `PUBLIC`.
- `ConnectionStringBuilderHelper.cs`: entirely new template. The `host/port/database` abstraction partially maps: `account` ≈ host, database maps cleanly, warehouse is an extra required field. The `BuildConnectionString` signature needs an optional `warehouse` or the user provides it via `AdditionalParameters`.
- `DbConnectionFactory.cs`: `new SnowflakeDbConnection(connectionString)`.
- `SchemaExtractor.cs`: new query case, mostly reusing INFORMATION_SCHEMA SQL.

**Technical cost: M**
The account-based connection model and mandatory warehouse parameter are meaningful deviations. The connection string builder needs thought. Schema extraction is straightforward. Snowflake.Data package is large (~15 MB) and has transitive dependencies.

---

## 9. BigQuery

**Why add it?**
BigQuery is Google Cloud's serverless analytics engine. It stores petabytes of data and is used by data teams at companies of all sizes. Supporting it positions SqlSpace for data analysts, not just developers.

**How to implement:**
- NuGet: `Google.Cloud.BigQuery.V2` (Google's official .NET client) — **this is not an ADO.NET driver**.
- Alternative: `BigQueryAdo` (community) or `Simba ODBC` driver wrapped via ODBC. None of these are first-class ADO.NET.
- Authentication: service account JSON key or Application Default Credentials (ADC) — **not username/password**.

**Architectural impact:**
BigQuery fundamentally breaks the current ADO.NET abstraction:
1. `IDbConnectionFactory` returns `DbConnection` — BigQuery has no `DbConnection` subclass in the official SDK.
2. `DatabaseExecutor` uses `DbCommand` / `DbDataReader` — BigQuery uses `BigQueryClient.ExecuteQuery()` returning `BigQueryResults`.
3. Credentials are a service account JSON blob, not a username+password — `EncryptedPassword` would store the entire JSON key, and the UI form would need a JSON upload.
4. Schema introspection uses BigQuery `INFORMATION_SCHEMA` tables which require the dataset context: `SELECT * FROM myproject.mydataset.INFORMATION_SCHEMA.COLUMNS`.

**Required new abstraction:**
To cleanly support BigQuery (and similarly non-ADO providers), introduce:
- `IQueryExecutor` interface separate from the ADO.NET path
- `BigQueryExecutor : IQueryExecutor` using `Google.Cloud.BigQuery.V2`
- A factory that returns the right executor based on provider
- The credential model needs a `CredentialType` concept (user/pass vs. service account JSON vs. OAuth token)

**Technical cost: L–XL**
This is the hardest database on this list. The ADO.NET paradigm doesn't stretch to cover BigQuery natively. Plan for 1–2 weeks of backend work and a UI form redesign for credential input.

---

## 10. MongoDB

**Why add it?**
MongoDB is the most popular document database. Many teams store operational data in MongoDB and want natural-language querying — but this use case is fundamentally different from SQL.

**How to implement:**
- NuGet: `MongoDB.Driver` (official .NET driver) — **not ADO.NET**.
- Query language: MongoDB Query Language (MQL) or aggregation pipelines. Not SQL.
- Schema: MongoDB is schemaless. Schema extraction requires sampling documents and inferring field types — there is no `INFORMATION_SCHEMA`.

**Architectural impact:**
MongoDB is the deepest change on this list because:
1. It requires the LLM to generate **MQL** (JSON-like), not SQL — the `TextToSqlClient` AI prompt and output parsing must be extended to support a second query language.
2. `DatabaseExecutor` and `SchemaExtractor` need MongoDB-specific implementations that don't use `DbConnection`.
3. Schema "extraction" is document sampling — run `db.collection.findOne()` per collection and infer types.
4. Result sets are documents (BSON/JSON), not tabular rows — `DatabaseExecutor` returns differently shaped data.
5. The frontend query result table would display nested JSON instead of flat rows.

**Required new abstractions:**
- `IQueryExecutor` (as above for BigQuery) with a MongoDB implementation
- `ISchemaExtractor` MongoDB implementation that samples collections
- A second query-language mode in the LLM prompting pipeline
- Frontend changes for displaying document results

**Technical cost: XL**
MongoDB is 2–3 weeks of backend work minimum and requires AI prompt engineering for a second query language. Strongly recommend this as a **phase 2** feature with a dedicated spike sprint.

---

## 11. Cassandra

**Why add it?**
Apache Cassandra is a distributed wide-column database used for high-throughput write workloads. Companies like Netflix, Apple, and Discord use it. CQL (Cassandra Query Language) is SQL-like but fundamentally different.

**How to implement:**
- NuGet: `CassandraCSharpDriver` (official DataStax .NET driver) — **not ADO.NET**.
- Query language: CQL — similar SQL syntax but no JOINs, no subqueries, partition-key-centric.
- Schema introspection: Cassandra has a `system_schema` keyspace with `system_schema.tables`, `system_schema.columns`, `system_schema.indexes` — no `INFORMATION_SCHEMA` but similarly structured.

**Architectural impact:**
1. No ADO.NET `DbConnection` — DataStax driver uses `ISession` / `ICluster` model.
2. No JOINs in CQL — the LLM would need to know not to generate JOINs.
3. Schema extraction is feasible via `system_schema` queries but needs a custom implementation.
4. Results are `RowSet` objects from the DataStax driver — needs translation to the common tabular format.
5. The LLM prompt must convey Cassandra's constraints (partition keys, no JOINs) for the generated CQL to be valid.

**Required new abstractions:**
- `IQueryExecutor` with a Cassandra implementation using `ISession`
- Custom schema extractor querying `system_schema.columns`
- LLM prompt mode awareness: SQL vs. CQL

**Technical cost: L**
Less complex than MongoDB (schema is structured) but still outside the ADO.NET model. Plan for 1 week of backend work plus AI prompt tuning.

---

## Summary Table

| Database | Driver Package | ADO.NET? | Connection Model | Schema Source | Effort |
|----------|---------------|----------|-----------------|---------------|--------|
| **MariaDB** | MySqlConnector (existing) | Yes | host:port | INFORMATION_SCHEMA | **XS** |
| **CockroachDB** | Npgsql (existing) | Yes | host:port | INFORMATION_SCHEMA | **XS** |
| **Supabase** | Npgsql (existing) | Yes | host:port | INFORMATION_SCHEMA | **XS** |
| **PlanetScale** | MySqlConnector (existing) | Yes | host:port | INFORMATION_SCHEMA | **XS** |
| **SQLite** | Microsoft.Data.Sqlite | Yes | file path | PRAGMA tables | **S** |
| **DuckDB** | DuckDB.NET.Data | Yes | file path | INFORMATION_SCHEMA (partial) | **S** |
| **Amazon Redshift** | Npgsql (existing) | Yes | host:port (5439) | INFORMATION_SCHEMA | **S** |
| **Snowflake** | Snowflake.Data | Yes | account string | INFORMATION_SCHEMA | **M** |
| **Cassandra** | CassandraCSharpDriver | No | host:port | system_schema | **L** |
| **BigQuery** | Google.Cloud.BigQuery.V2 | No | project/dataset | INFORMATION_SCHEMA | **L–XL** |
| **MongoDB** | MongoDB.Driver | No | host:port | document sampling | **XL** |

---

## Recommended Rollout Order

**Phase 1 — Zero-cost wins (1–2 days total):**
1. MariaDB
2. CockroachDB
3. Supabase
4. PlanetScale
5. Amazon Redshift

These five databases require no new NuGet packages (reuse existing drivers), no new schema query logic, and are pure enum + routing additions.

**Phase 2 — File-based databases (2–3 days total):**
6. SQLite
7. DuckDB

These require small architectural accommodations for the file-path connection model but are straightforward.

**Phase 3 — New driver, same paradigm (3–5 days):**
8. Snowflake

Needs a new driver and some connection string model adjustments but stays within ADO.NET.

**Phase 4 — New execution model (2–4 weeks, requires architectural work):**
9. Cassandra
10. BigQuery
11. MongoDB

These break the ADO.NET abstraction and require a new `IQueryExecutor` interface, new credential models, and for MongoDB/Cassandra: AI prompt engineering for non-SQL query languages. These are best treated as a separate project milestone.
