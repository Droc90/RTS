GitHub Copilot Instructions for RTS

Project Purpose

RTS is a reusable .NET application foundation. Keep it product-neutral. Do not add RTS, trading, or other product-specific features to this solution.

Before proposing architectural or database changes, consult:

docs/architecture.md

database/RTSDB_Create.sql

Required Technology

Target .NET 10 (net10.0) in every project.

Use modern C# supported by .NET 10.

Use the ASP.NET Core Blazor Web App model.

Use global Interactive Server rendering initially.

Use ASP.NET Core Identity and Entity Framework Core for SQL Server.

Do not change the rendering model or add a separate API unless explicitly requested.

Clean Architecture Boundaries

Maintain these project responsibilities and dependency rules:

RTS.Domain: Domain entities, value objects, invariants, enums, and domain rules. No dependencies on other RTS projects or infrastructure frameworks.

RTS.Contracts: Transport-safe request, response, DTO, pagination, and result models. Do not expose EF Core or Identity persistence types.

RTS.Application: Use cases, application services, validation, authorization rules, and abstractions. May reference only RTS.Domain and RTS.Contracts.

RTS.Infrastructure: EF Core, Identity persistence, SQL Server, repositories, and external-service implementations. May reference RTS.Application and RTS.Domain.

RTS.Web: Blazor UI, ASP.NET Core hosting, endpoints, middleware, and dependency-injection composition. May reference RTS.Application, RTS.Contracts, and RTS.Infrastructure.

Never add references from Domain or Application to Infrastructure or Web.

Razor components must not directly access:

DbContext

EF Core entity sets

UserManager or RoleManager

SQL Server

File systems or external providers

UI components must call application-level services or UI client abstractions.

Database Rules

RTSDB already exists and has been validated. Treat database/RTSDB_Create.sql as the authoritative physical database definition.

SQL Server compatibility level must remain 170.

The database contains 19 tables across 6 schemas.

Internal primary keys use SQL bigint and C# long.

External identifiers use SQL uniqueidentifier and C# Guid.

Preserve all existing table names, column definitions, relationships, constraints, defaults, and indexes.

Map database names explicitly when CLR naming differs.

Do not hard-code the physical database name in application logic.

Do not create, replace, or mutate the database automatically at application startup.

Do not generate or apply EF Core migrations unless explicitly requested.

Do not call Database.EnsureCreated() or Database.EnsureDeleted().

Do not modify the SQL creation script unless explicitly requested.

Existing schemas are:

Identity

Profile

Administration

Audit

Communication

Storage

Identity Rules

Configure ASP.NET Core Identity manually against the existing Identity schema.

Use long as the Identity key type.

Map Identity models to the existing clean table names.

Never create AspNetUsers, AspNetRoles, or any other AspNet* table.

Preserve tables such as Identity.Users, Identity.Roles, and Identity.UserRoles.

Keep authentication records separate from Profile data.

Do not use template-generated Identity migrations as the database definition.

Do not weaken password, lockout, cookie, antiforgery, or authorization security to simplify implementation.

Coding Standards

Enable and respect nullable reference types.

Prefer async APIs for I/O operations and propagate CancellationToken where practical.

Use dependency injection; avoid service-location and global mutable state.

Keep public contracts explicit and small.

Do not expose persistence entities directly to the UI or a future API.

Validate input at the application boundary.

Use configuration and options classes for application identity, connection strings, cookie names, data-protection names, and provider settings.

Never commit secrets, passwords, API keys, production connection strings, or administrator credentials.

Do not add a package, abstraction, design pattern, or service unless it solves a current requirement.

Preserve existing code style and avoid unrelated rewrites.

Reusability Rules

Treat RTS as a replaceable template prefix.

Keep display names and deployment-specific values configuration-driven.

Keep the database connection string configuration-driven.

Give each derived application a unique authentication cookie name and data-protection application name.

Avoid embedding RTS, RTSDB, environment names, machine paths, or deployment URLs in business logic.

Ensure product-specific projects can reuse Domain, Application, Contracts, and Infrastructure behavior or rename the solution predictably.

Change and Verification Expectations

When implementing a change:

Identify the appropriate project before adding code.

Preserve the dependency direction.

Verify mappings against the authoritative SQL script rather than assuming default Identity or EF conventions.

Build the entire solution.

Run relevant tests.

Report any unverified assumptions, especially database assumptions.

Do not silently introduce schema changes, migrations, new hosting models, or cross-layer dependencies.