Ranked Trading System Application Architecture

Purpose

RTS is the Ranked Trading System product application. It provides shared authentication, authorization, user administration, configuration, auditing, notifications, and file storage while supporting multiple ranked trading models and product workflows within one system.

The initial implementation uses a .NET 10 Blazor Web App with Interactive Server rendering. The architecture preserves the option to introduce a separate web API and client-side rendering later without rewriting the domain or application layers.

Technology Baseline

.NET 10 (net10.0) for every project

ASP.NET Core Blazor Web App

Global Interactive Server rendering initially

ASP.NET Core Identity mapped to the existing RTSDB schema

Entity Framework Core with SQL Server

SQL Server 2025 with database compatibility level 170

HTTPS and the .dev.localhost development TLD

Clean Architecture with explicit dependency boundaries

Solution Structure

RTS
|-- RTS.Domain
|-- RTS.Application
|-- RTS.Contracts
|-- RTS.Infrastructure
|-- RTS.Web
|-- RTS.Application.Tests
|-- RTS.Integration.Tests
|-- database
|-- docs
`-- .github

RTS.Domain

Contains the core business model and rules that do not depend on UI, persistence, ASP.NET Core, or infrastructure frameworks.

Expected contents include:

Domain entities

Value objects

Domain enums and constants

Domain exceptions

Business invariants and domain services

RTS.Domain has no project dependencies.

RTS.Contracts

Contains transport-safe models shared at application boundaries.

Expected contents include:

Request and response records

Data transfer objects

Pagination and result models

Public service contracts that may later cross an HTTP boundary

Contracts must not expose EF Core entities, Identity persistence models, DbContext, or infrastructure-specific types.

RTS.Contracts has no project dependencies.

RTS.Application

Contains application use cases and coordinates domain behavior.

Expected contents include:

Application service interfaces and implementations

Commands, queries, and use cases

Validation and authorization rules

Persistence and external-service abstractions

Mapping between domain objects and contract models

RTS.Application references:

RTS.Domain

RTS.Contracts

It must not reference RTS.Infrastructure or RTS.Web.

RTS.Infrastructure

Contains implementations that interact with databases, ASP.NET Core Identity, file systems, email providers, logging systems, or other external resources.

Expected contents include:

EF Core DbContext implementations

Entity and Identity mappings

Repository implementations

Identity stores and supporting services

SQL Server configuration

Email, notification, auditing, and file-storage implementations

RTS.Infrastructure references:

RTS.Application

RTS.Domain

RTS.Web

Hosts the ASP.NET Core application and contains the Blazor user interface.

Expected contents include:

Razor components and layouts

Registration and account-management pages

Login and logout endpoints

User and role administration screens

Dependency-injection composition

Authentication and authorization configuration

Application startup and middleware

RTS.Web references:

RTS.Application

RTS.Contracts

RTS.Infrastructure

Razor components must not access EF Core, DbContext, UserManager, RoleManager, or SQL Server directly. Components communicate through application-level services or UI client abstractions.

RTS.Application.Tests

Contains fast, isolated tests for domain and application behavior.

Expected contents include:

Application service and use-case tests

Validation and authorization-rule tests

Domain behavior tests when a separate domain test project is unnecessary

Tests using fakes or mocks for application abstractions

RTS.Application.Tests references:

RTS.Application

RTS.Domain

RTS.Contracts

RTS.Integration.Tests

Contains tests that verify infrastructure and application integration against realistic dependencies.

Expected contents include:

EF Core mapping tests

RTSDB integration tests

ASP.NET Core Identity store tests

Authentication and authorization integration tests

Application startup and endpoint tests when the Web project is later referenced

RTS.Integration.Tests initially references:

RTS.Infrastructure

RTS.Application

RTS.Domain

RTS.Contracts

It may reference RTS.Web later when full-host or endpoint testing is implemented.

Dependency Direction

Dependencies point inward toward the domain and application layers:

RTS.Web ---------> RTS.Application ---------> RTS.Domain
     |                       |
     |                       `---------------> RTS.Contracts
     |
     `--------------> RTS.Infrastructure
                              |
                              `-------------> RTS.Application

Infrastructure implements interfaces defined by the application layer. Application and domain code must remain usable without the Blazor host.

Rendering and Service Strategy

The initial UI uses global Interactive Server rendering. UI components execute on the server and call application services in-process.

This is an implementation choice, not a permanent architectural dependency. Application operations use contracts and interfaces that can later be exposed through a separate RTS.Api host.

If a remote service boundary becomes necessary:

Add a RTS.Api project.

Reference the existing Application, Contracts, Domain, and Infrastructure projects.

Expose selected application operations as authenticated API endpoints.

Implement HTTP-based UI clients behind the same UI-facing abstractions.

Move compatible components to Interactive WebAssembly or Interactive Auto only when there is a demonstrated requirement.

The application will not add a separate API merely to simulate architectural separation during the initial server-rendered implementation.

Target Runtime Architecture

RTS is a modular monolith. It is deployed initially as one application, but its business capabilities are separated by project and application boundaries so they can evolve independently.

```text
Blazor Web UI
      |
Application use cases
      |
Domain and configurable rule engine
      |
Infrastructure adapters
  |-- SQL Server
  |-- Market-data providers
  |-- Chart-data providers
  `-- Background job queue

Planned RTS.Worker
  |-- Market-data ingestion
  |-- Candidate screening
  `-- Chart calculation and scoring
```

The initial deployment must not be divided into microservices. New process or network boundaries are introduced only when scale, reliability, deployment independence, or an external client creates a demonstrated need.

Blazor UI Boundary

Blazor is the presentation layer for authenticated forms, configuration, administration, tables, dashboards, and workflow. Razor components may collect input and display progress or results, but they must not contain screening algorithms, scoring rules, market-data access, persistence logic, or long-running job execution.

Interactive Server rendering is appropriate for the initial authenticated application. Its persistent SignalR circuits must not be treated as durable execution contexts. A screening or scoring operation must continue independently if the initiating browser disconnects.

If concurrent-user or geographic-scale requirements outgrow Interactive Server rendering, the application boundaries must allow the UI to adopt Interactive Auto, WebAssembly, or another client through authenticated APIs without rewriting domain rules or application use cases.

Configurable Rule Engine

Candidate screening and chart scoring use a shared configuration approach but remain separate rule categories. Rules and strategy profiles belong to users, support validated operators and typed values, and must be evaluated in the Domain or Application layers rather than in UI components.

Every persisted screening or scoring result must retain a snapshot or version reference for the criteria used. Historical results must not change meaning when a user later edits a strategy profile.

RTS is a multi-model platform. A versioned trading-model definition owns its supported asset types, timeframes, bar intervals, market-session behavior, indicators, parameters, evaluation rules, weights, and score aggregation. The first methodology is a system template, not a hard-coded application boundary. Users may copy templates into user-owned models and customize supported values without changing application code.

Candidate Discovery and Approval

Manual entry, deterministic screening, and AI-assisted catalyst discovery feed one Candidate Inbox. Discovery records provenance, evidence, data timestamps, strategy and model versions, eligibility results, risks, and status.

Candidates do not enter full chart evaluation automatically. The user selects which candidates advance. Selection creates persisted evaluation jobs and provides a human approval boundary between AI discovery and resource-consuming evaluation.

External Provider Boundaries

Market data, historical price data, and other external services are Infrastructure concerns implemented behind interfaces defined by the Application layer. Provider-specific request models, credentials, rate limits, and failure behavior must not leak into Domain entities or Razor components.

This adapter boundary permits a provider to be replaced or supplemented without rewriting screening and scoring behavior. Provider responses should be normalized into RTS-owned contracts before entering the rule engine.

Background Processing

Long-running or retryable work must be queued instead of executing inside a Blazor event handler or request. This includes market-data ingestion, broad candidate screens, chart-indicator calculations, and batch scoring.

An in-process hosted service may support early development, provided jobs and status are persisted durably. As workload and operational requirements grow, add an `RTS.Worker` host that references the existing Application, Contracts, Domain, and Infrastructure projects. The worker must use the same application use cases and must not duplicate business rules.

Queued work should support cancellation, bounded concurrency, retry policies, observable progress, failure recording, and safe restart behavior. UI pages submit work and observe persisted status rather than owning the execution lifetime.

Charting Strategy

Radzen supplies general UI components and standard charts. Detailed financial charting may use a specialized JavaScript chart library wrapped behind an RTS-owned Blazor component and JavaScript module. The wrapper must keep chart-library types out of the Application and Domain layers so the charting library can be changed without affecting business logic.

The initial trading model renders 5-day charts with five-minute bars and 1-month and 3-month charts with daily bars. Each view includes volume, Bollinger Bands, SMA50, MACD, and RSI. Chart definitions and indicator parameters come from the trading-model version rather than Razor-component constants.

Regular trading hours are the initial default. Retrieval, display, indicator calculation, and scoring treatment of extended-hours bars are separate versioned settings. The market-data layer retrieves sufficient warm-up history, normalizes timestamps and corporate actions, and supplies deterministic calculations before the visible time window is rendered.

Users do not upload chart screenshots during the normal workflow. RTS generates interactive charts from normalized OHLCV data and may generate controlled images internally for a vision model. Calculated values remain authoritative.

Canonical Evaluation and Adaptive Presentation

Every evaluation produces one immutable canonical result containing evidence, timeframe findings, indicator values, rule results, scores, risks, uncertainty, provenance, citations, and all relevant versions. Presentation cannot change its facts, scores, or conclusions.

Approved presentation modes arrange the same canonical content as a Narrative Report, Interactive Dashboard, or Analyst Workbench. Experience controls analytical controls and optional educational assistance. Age range, generational preference, explicit choices, accessibility settings, and opt-in behavioral learning may influence the initial layout and information architecture.

Users can inspect, edit, disable, and reset learned preferences. Presentation preferences are stored separately from strategies, trading models, investor profiles, and risk settings and must never silently alter an evaluation or infer risk tolerance.

Generative AI and Agent Boundaries

Generative AI operates through provider-neutral Application interfaces and typed Infrastructure adapters. It may draft structured strategies, discover cited catalysts, create grounded narratives, interpret controlled chart images alongside calculated metrics, and select approved presentation arrangements.

AI cannot change deterministic scores, invent current facts, save unvalidated strategy changes, bypass candidate approval, infer financial suitability from presentation behavior, or execute trades. Material changes require explicit user authorization. Provider, model, prompt, schema, evidence, usage, cost, and outcome metadata are auditable.

The complete product rules are maintained in `docs/product-ai-and-presentation.md`.

Database Architecture

RTSDB is an existing, validated database and is the authoritative physical database definition. EF Core maps to this schema; it does not redesign or replace it.

Database characteristics:

SQL Server 2025 compatibility level 170

19 tables across 6 schemas

Internal primary keys use bigint

External identifiers use uniqueidentifier with NEWID() defaults

Foreign keys and indexes are defined by the database creation script

Storage.StoredFiles uses a persisted StorageLocationHash with a unique index

Schemas and responsibilities:

Schema

Responsibility

Identity

ASP.NET Core Identity users, roles, claims, logins, and tokens

Profile

User profiles, preferences, and agreements

Administration

Settings and feature flags

Audit

Audit records, login history, and application errors

Communication

Notifications and email history

Storage

Stored-file metadata

The database name appears only in deployment scripts and configuration-based connection strings. Application logic must not hard-code RTSDB.

EF Core migrations must not be generated or applied until the project explicitly adopts a migration strategy. The existing SQL script remains the baseline source of truth.

Identity and Security

ASP.NET Core Identity will be configured manually rather than generated by the Blazor template.

Identity requirements:

Map Identity models to clean table names such as Identity.Users, Identity.Roles, and Identity.UserRoles.

Use long/bigint for internal Identity keys.

Preserve existing column definitions, constraints, indexes, and relationships.

Keep authentication data separate from profile data.

Support registration, login, logout, account management, roles, and user administration.

Seed the first administrator through an explicit, secure initialization process.

Do not create AspNetUsers, AspNetRoles, or other AspNet* tables.

Do not store secrets or production connection strings in source control.

Cookie names and data-protection application names must be configurable and unique for each copied application.

Product Identity and Extensibility

RTS is the stable product prefix for the Ranked Trading System. Ranked models are modules within the product and must not replace the solution, namespace, database, or deployment identity.

Environment-specific values must be configuration-driven where practical:

Application name and display name

Database connection string

Authentication cookie name

Data-protection application name

Email sender identity

File-storage location

Environment and deployment identifiers

Database schema names remain organized by technical responsibility. New trading modules should follow the existing project boundaries and introduce schema changes deliberately through the authoritative database scripts.

Product-specific trading features belong in RTS and should be added without weakening the shared security, administration, auditing, and operational foundation.

Trading Configuration Foundation

Trading-model and screening-strategy definitions are separate versioned aggregates in the Domain project. Both support RTS-owned system templates, user ownership, editable drafts, immutable published versions, retirement of superseded versions, and historical version references.

Trading-model versions contain configurable timeframes, regular or extended market-session behavior, indicators, typed indicator parameters, and evaluation criteria. Screening-strategy versions contain independently configurable filter, score, and warning rules. Presentation preferences remain separate from screening and evaluation logic.

The Application project owns metric and indicator registries, whole-configuration validation, validated publication, and creation of independent user-owned copies from published system templates. Infrastructure maps these aggregates explicitly to the Trading schema. Authoritative clean-install scripts and incremental feature scripts define the same tables, constraints, foreign keys, and indexes.

Initial Delivery Sequence

Establish and document the solution architecture.

Add automated test projects and baseline test infrastructure.

Configure EF Core against the existing RTSDB database.

Implement and verify all entity and Identity mappings.

Configure registration, login, logout, and account management.

Add role-based authorization and administrator initialization.

Build user and role administration screens.

Add auditing, logging, security controls, and integration tests.

Prepare repeatable renaming and application-copy guidance.

Architectural Rules

All projects target .NET 10.

Dependencies point inward; Domain and Application never depend on Web or Infrastructure.

UI components contain presentation logic, not persistence or business logic.

Infrastructure concerns are implemented behind application abstractions.

Database models are not exposed directly as public UI or API contracts.

Existing database names and constraints are mapped explicitly.

No database migration or schema mutation occurs implicitly at application startup.

New dependencies are added only when they solve a concrete requirement.

The initial implementation favors clarity and maintainability over unnecessary abstraction.

Future API or client hosting options remain possible without burdening the first implementation.

Current Status

RTSDB has been created and validated.

Compatibility level 170 has been verified.

All 28 tables, 7 schemas, foreign keys, and storage-hash behavior have passed validation.

The seven-project .NET 10 solution builds successfully.

Application and integration test projects are configured, discovered, and passing.

The Blazor Web App runs successfully using Interactive Server rendering.

Identity and EF Core application integration are implemented and covered by automated tests.
