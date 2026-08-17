# Ranked Trading System (RTS)

The Ranked Trading System is a secure .NET 10 and Blazor platform for ranked trading models, analysis, and operational workflows. Individual models are modules within the wider system rather than the identity of the application itself.

The solution currently provides the shared platform capabilities needed for continued RTS development: ASP.NET Core Identity, user and application-error administration, user profiles, security auditing, responsive Blazor UI, and SQL Server persistence behind clean application-layer contracts.

## Current Status

The shared application foundation is operational:

- User registration
- Confirmation email generation after registration
- Email-confirmation links backed by ASP.NET Core Identity tokens
- Resend-confirmation workflow with account-enumeration protection
- Confirmed email required before login
- Forgot-password and password-reset workflow
- Time-limited Identity password-reset tokens
- Used and invalid password-reset token rejection
- Automatic invalidation of active sessions after password reset
- Login and logout
- Secure, application-specific authentication cookies
- Password policy and account lockout
- Standard `Administrator` and `User` roles
- Automatic `User` role assignment during registration
- One-time initial administrator provisioning
- Named authenticated-user and administrator authorization policies
- Centralized policy registration in the Blazor web host
- Administrator-only route authorization through reusable policies
- Searchable and paginated administrator user list
- User account detail and security-status view
- Account activation and deactivation with confirmation
- Administrator promotion and demotion with confirmation
- Self-deactivation, self-demotion, and last-administrator safeguards
- Policy-based administrator navigation
- Protected routes using named `[Authorize]` policies
- Safe return-to-page behavior after login
- Inactive and soft-deleted user checks
- Five-minute authentication-state revalidation for active Blazor sessions
- Five-minute Identity cookie security-stamp validation
- Automatic session invalidation for inactive, deleted, or security-stamp-invalidated accounts
- User profile viewing and editing
- Concurrency-safe profile updates using SQL Server `rowversion`
- Profile fields for display name, first name, last name, time zone, locale, and preferred date format
- Graceful recovery when account creation succeeds but confirmation-email delivery fails
- Configurable request throttling for resend-confirmation and forgot-password workflows
- Normalized, privacy-preserving request-limit keys with separate counters by workflow
- Persistent login and logout security history
- Administrator account-change auditing with JSON before-and-after values
- Actor, target, result, correlation, and request-context audit data
- Centralized persistence of unhandled HTTP request exceptions
- Blazor component and circuit exception logging through a shared error boundary
- Safe error pages with database-linked correlation references
- Private diagnostic details separated from user-facing error messages
- Searchable, filterable, and paginated application-error administration
- Protected application-error details with private diagnostic information
- New, Investigating, Resolved, and Ignored resolution workflows
- Resolution notes, administrator attribution, timestamps, and concurrency-safe updates
- Responsive desktop and mobile layouts
- Keyboard navigation, semantic page structure, visible focus states, and skip navigation
- Accessible account, administration, profile, confirmation, error, access-denied, and not-found pages
- EF Core mappings for the existing Identity schema
- SQL Server connection and Identity mapping integration tests
- Automated request-limiter tests covering permit limits, workflow isolation, and email normalization
- Automated audit-writing integration tests with transaction rollback
- Automated application-error persistence coverage
- Automated anonymous, authenticated-user, and administrator policy coverage
- Automated registration, email-confirmation, password-recovery, profile, user-administration, and application-error administration coverage
- Production SMTP email delivery through MailKit
- Strict STARTTLS or implicit-TLS transport enforcement
- Startup validation for SMTP settings and credentials
- Anonymous JSON liveness and database-readiness endpoints
- Safe health responses without exception or diagnostic disclosure
- Clean Release build with no warnings or errors
- No vulnerable, deprecated, or outdated NuGet packages
- Sixty passing automated tests

The shared RTS foundation is complete and ready for product-specific trading modules.

## Technology

- .NET 10
- Blazor Web App
- Interactive Server rendering
- ASP.NET Core Identity
- Entity Framework Core 10.0.11
- SQL Server 2025 Developer Edition
- SQL Server compatibility level 170
- xUnit v3 3.2.2
- Bootstrap 5.3.8
- Radzen.Blazor 11.2.5
- MailKit 4.17.0
- Microsoft.Extensions.Diagnostics.HealthChecks 10.0.11

## Architecture

RTS follows Clean Architecture principles and keeps UI, application behavior, domain logic, contracts, and persistence concerns separated.

```text
RTS
├── RTS.Domain
├── RTS.Contracts
├── RTS.Application
├── RTS.Infrastructure
├── RTS.Web
├── RTS.Application.Tests
└── RTS.Integration.Tests
```

### Project Responsibilities

| Project | Responsibility |
|---|---|
| `RTS.Domain` | Core domain entities, value objects, and domain rules |
| `RTS.Contracts` | Transport-safe requests, responses, and shared DTOs |
| `RTS.Application` | Application interfaces and use-case orchestration |
| `RTS.Infrastructure` | EF Core, ASP.NET Core Identity stores, SQL Server access, and external implementations |
| `RTS.Web` | Blazor UI, HTTP endpoints, authentication configuration, and application composition |
| `RTS.Application.Tests` | Unit tests for application and domain behavior |
| `RTS.Integration.Tests` | Database and infrastructure integration tests |

The intended dependency direction is:

```text
RTS.Web ────────────────┐
                         ▼
RTS.Infrastructure → RTS.Application → RTS.Domain
                              │
                              ▼
                       RTS.Contracts
```

Razor components do not access EF Core, `DbContext`, `UserManager`, or SQL directly. UI components call application-layer interfaces implemented by Infrastructure.

### Runtime Strategy

RTS is a modular monolith. Blazor is the presentation layer; configurable screening rules, chart-scoring logic, and provider-independent use cases remain in the Domain and Application projects.

Long-running market-data ingestion, candidate screening, and chart scoring will execute as durable queued work rather than inside an Interactive Server circuit. Early development may use an in-process hosted service, with a separate `RTS.Worker` host added when workload or operational requirements justify it.

Detailed financial charts may use a specialized JavaScript charting library through an RTS-owned Blazor wrapper. This allows RTS to retain Radzen for general UI components without coupling application logic to Radzen or a particular chart vendor.

See [the architecture document](docs/architecture.md) for the complete boundaries and evolution strategy.

## Database Design

`RTSDB` is the authoritative database definition. The database is created from the SQL script in the `database` directory rather than from EF Core migrations.

Key conventions include:

- Internal primary keys use `bigint IDENTITY`.
- Public identifiers use `uniqueidentifier`.
- Application timestamps are stored in UTC.
- Concurrency-sensitive tables use SQL Server `rowversion`.
- Soft deletion is used where appropriate.
- SQL Server compatibility level 170 is required.
- EF Core migrations and `EnsureCreated()` are intentionally disabled unless the database strategy is explicitly changed.

### Identity Tables

ASP.NET Core Identity is mapped to clean, schema-qualified names:

```text
Identity.Users
Identity.Roles
Identity.UserRoles
Identity.UserClaims
Identity.RoleClaims
Identity.UserLogins
Identity.UserTokens
```

The application does not create or use `AspNetUsers`, `AspNetRoles`, or other `AspNet*` tables.

### Audit Tables

Security and administrative events use the existing schema-qualified audit tables:

```text
Audit.LoginHistory
Audit.AuditLogs
Audit.ApplicationErrors
```

All three audit tables are mapped and operational. `ApplicationErrors` stores unhandled HTTP request and Blazor component exceptions with safe correlation references and private diagnostic details. Administrators can search, review, annotate, and resolve these records through protected application-error administration pages.

## Authentication

The current authentication implementation includes:

- Unique email enforcement
- Email used as the initial username
- Lowercase email storage
- ASP.NET Core Identity password hashing
- Configurable password policy
- Five-attempt account lockout
- Fifteen-minute default lockout period
- Secure and HTTP-only cookies
- Eight-hour cookie lifetime with sliding expiration
- Antiforgery protection for login and logout
- Local return-URL validation
- Inactive and soft-deleted account blocking
- Confirmed email required before login
- Idempotent email-confirmation processing
- Public resend-confirmation workflow with generic responses
- Public forgot-password workflow with account-enumeration protection
- Password reset using Identity tokens and public external user IDs
- Password-policy enforcement during reset
- Safe invalid or expired reset-token responses
- Security-stamp invalidation after password changes
- Provider-independent email delivery abstraction
- Development email output for local confirmation testing
- Periodic validation of active Interactive Server sessions
- Coordinated five-minute validation of Blazor sessions and Identity cookies
- Registration recovery when confirmation-email delivery fails
- Configurable fixed-window throttling for confirmation resend and password-reset requests
- Separate request counters for email-confirmation and password-reset workflows
- Case-insensitive, whitespace-normalized email identifiers stored as SHA-256 hashes in the limiter cache
- Persistent login success, login failure, lockout, and logout events
- Private login failure codes while browser responses remain account-enumeration safe
- SHA-256 login-identifier hashes instead of plaintext attempted email addresses
- Login audit context including IP address, user agent, correlation ID, and UTC timestamp
- Last-login timestamp updates
- Automatic assignment of newly registered accounts to the `User` role
- Idempotent creation of the `Administrator` and `User` system roles
- One-time promotion of an existing active account as the initial administrator

Email confirmation is required. Development builds write confirmation and password-reset messages with directly copyable links to the application output. Production uses a provider-neutral MailKit SMTP sender configured through environment variables or a deployment secret store.

### Account Request Throttling

Public confirmation-resend and forgot-password requests are limited independently by normalized email address. The default configuration permits three requests per workflow during each fifteen-minute window:

```json
{
  "AccountRequestLimits": {
    "PermitLimit": 3,
    "WindowMinutes": 15
  }
}
```

The current implementation uses the application process's memory cache. Counters therefore reset when the application restarts and are not shared between multiple application instances. This is appropriate for local development and the initial single-server RTS deployment. A scaled production deployment should replace it with distributed caching or infrastructure-level rate limiting.

### Security Audit Logging

Authentication activity is recorded in `Audit.LoginHistory`. Supported events include successful login, failed login, account lockout, and successful logout. Failure records preserve an internal reason such as invalid credentials, inactive account, deleted account, disallowed sign-in, or unknown user without exposing that reason in the public login response.

Administrator account changes are recorded in `Audit.AuditLogs`. The current actions are:

- User activation and deactivation
- Administrator promotion and demotion

Each successful administrator action identifies the acting user and target account, records the previous and new security state as valid JSON, and includes a correlation identifier. Administrator role changes and their audit records are committed within the same SQL transaction.

### Application Error Logging

Unhandled HTTP request exceptions are captured by application middleware and persisted in `Audit.ApplicationErrors`. Interactive Blazor component and circuit exceptions are captured by a shared error boundary because they occur after the original HTTP request has completed.

Recorded information includes:

- Authenticated user when available
- Correlation ID
- Exception type and private diagnostic details
- Safe, nontechnical message
- Exception source
- Request path and HTTP method when available
- HTTP status code
- UTC occurrence time
- Initial resolution status

Users see only a safe error message and correlation reference. Exception messages and stack traces remain private in the database. Error recording uses a fresh dependency-injection scope so a failed request's EF Core context does not prevent persistence, and logging failures are contained so they cannot replace the original exception.

The Blazor error boundary's **Return home** action performs a full-page navigation to establish a fresh Interactive Server circuit after a fatal component error.

### Application Error Administration

Administrators can manage persisted errors through a protected workflow that provides:

- Newest-first, paginated error results
- Search by error type, error code, source, request path, external ID, or correlation ID
- Filtering by resolution status
- Detailed request, user, correlation, and diagnostic information
- Resolution status changes among `New`, `Investigating`, `Resolved`, and `Ignored`
- Resolution notes of up to 2,000 characters
- Automatic resolved timestamps and administrator attribution for terminal statuses
- SQL Server `rowversion` protection against conflicting updates

Private exception details are available only to authorized administrators. Public error pages continue to expose only a safe message and correlation reference.

## User Profiles

Authenticated users can view and update their own profile at `/account/profile`. Profile data is stored separately from the Identity account and includes:

- Display name
- First and last name
- Time zone
- Locale
- Preferred date format

Updates use SQL Server `rowversion` optimistic concurrency. If the profile changes between loading and saving, the stale update is rejected rather than overwriting newer data.

## Responsive UI and Accessibility

The RTS UI provides a consistent responsive layout for public, authenticated-user, and administrator workflows. Navigation adapts to the current authentication state and exposes administrative links only to authorized users.

Accessibility work includes:

- Semantic header, navigation, and main-content regions
- A keyboard-accessible skip-to-main-content link
- Visible keyboard focus states
- Responsive forms, cards, tables, and diagnostic panels
- Safe, readable validation and error messages
- Mobile layouts verified at a 390-pixel viewport

Lighthouse Accessibility validation produced scores from 95 to 100 across the home, profile, user-administration, and application-error pages. The final application-error list and details pages scored 97 and 96 respectively, with no reported accessibility errors.

## Authorization Policies

Authorization requirements are registered centrally in `RTS.Web` and referenced through stable policy-name constants from `RTS.Application`. This avoids scattering role-name strings throughout Razor pages and keeps future authorization changes localized.

The current policies are:

| Policy | Requirement |
|---|---|
| `RequireAuthenticatedUser` | A successfully authenticated user |
| `RequireAdministrator` | An authenticated user in the `Administrator` role |

Profile management uses `RequireAuthenticatedUser`. User administration, application-error administration, and administrator navigation visibility use `RequireAdministrator`. Anonymous users are redirected to login with a safe return URL; authenticated users who lack administrator access are sent to the access-denied page.

### Initial Administrator Provisioning

The first administrator can be provisioned by supplying an existing account email through configuration:

```text
InitialAdministrator__Email=admin@example.com
```

For production, provide this value through protected deployment configuration rather than committed JSON. Provisioning runs only while no administrator exists. After the first administrator receives the role, remove the setting. Additional administrators should be managed through the protected administration UI.

## Configuration

Application-specific identity values are stored in `RTS.Web/appsettings.json`:

```json
{
  "Application": {
    "Name": "RTS",
    "DisplayName": "Ranked Trading System",
    "CookieName": ".RTS.Auth",
    "DataProtectionName": "RTS"
  },
  "AccountRequestLimits": {
    "PermitLimit": 3,
    "WindowMinutes": 15
  }
}
```

Local database configuration belongs in `RTS.Web/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=RTSDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

Do not commit production credentials or environment-specific secrets. Use environment variables, user secrets, or the deployment platform's secret-management system outside local development.

## Getting Started

### Prerequisites

- Visual Studio 2026 with the ASP.NET and web development workload
- .NET 10 SDK
- SQL Server 2025 Developer Edition or a compatible Azure SQL environment
- SQL Server Management Studio, recommended for local database management

### Local Setup

1. Clone the repository.
2. Run the SQL creation script from the `database` directory against the local SQL Server instance.
3. Confirm that `RTSDB` uses compatibility level 170.
4. Add the local connection string to `RTS.Web/appsettings.Development.json`.
5. Set `RTS.Web` as the startup project.
6. Rebuild the solution.
7. Run all tests.
8. Start the application and browse to the generated HTTPS `.dev.localhost` address.

### Current Routes

| Route | Purpose |
|---|---|
| `/account/register` | Create a user account |
| `/account/login` | Log in |
| `/account/confirm-email` | Confirm an email address using an Identity token |
| `/account/resend-confirmation` | Request another confirmation message |
| `/account/forgot-password` | Request a password-reset message |
| `/account/reset-password` | Reset a password using an Identity token |
| `/account/profile` | View and update the authenticated user's profile |
| `/account/access-denied` | Display an authorization failure |
| `/admin/users` | Search and manage user accounts |
| `/admin/users/{externalId}` | View account details and manage status or administrator access |
| `/admin/errors` | Search, filter, and page through application errors |
| `/admin/errors/{externalId}` | Review diagnostics and update an application error's resolution |
| `/health/live` | Report whether the application process is running |
| `/health/ready` | Report whether required database connectivity is available |

Login and logout submissions use dedicated HTTP POST endpoints so ASP.NET Core can safely issue or remove authentication cookies outside the interactive Blazor circuit.

## Testing

Run the complete 60-test suite through Visual Studio Test Explorer.

The unit and integration suites currently verify:

- Connectivity to the local `RTSDB` database
- Queries against all seven mapped Identity tables
- Compatibility between the EF Core model and the existing Identity schema
- Enforcement of configured account-request permit limits
- Independent counters for confirmation and password-reset workflows
- Case-insensitive and whitespace-normalized request identifiers
- Queries against `Audit.LoginHistory`, `Audit.AuditLogs`, and `Audit.ApplicationErrors`
- Persistent login-history event creation and identifier hashing
- Persistent administrator audit-event creation and JSON state values
- Persistent application-error creation with safe and diagnostic information
- SQL Server `rowversion` generation and initial error-resolution state
- Transaction rollback so automated audit tests do not leave database records behind
- Authenticated-user policy rejection of anonymous identities
- Administrator-policy rejection of anonymous and non-administrator identities
- Administrator-policy acceptance of administrator identities
- User registration, duplicate-email rejection, and automatic role assignment
- Email-confirmation and password-recovery behavior
- User search, account administration, and administrative safeguards
- User-profile retrieval, persistence, and stale `rowversion` rejection
- Application-error searching, detail retrieval, resolution, administrator attribution, and stale `rowversion` rejection
- Production application, host, Data Protection, HSTS, and SMTP configuration validation
- Authenticated and trusted-relay SMTP configuration
- Rejection of partial credentials and non-strict SMTP transport security
- Healthy database-readiness behavior with the configured RTS database
- Unhealthy database-readiness behavior when SQL Server is unavailable

Integration tests require access to the configured SQL Server instance. They use the isolated local `RTSIntegrationTests` database by default. Other development and CI environments can override it with:

```text
RTS_TEST_CONNECTION_STRING
```

## RTS Product Development

RTS is now the product application created from the reusable Shell template. New ranked models and trading capabilities should be added as modules within the existing architecture:

```text
RTS.Domain          → trading entities, value objects, and domain rules
RTS.Contracts       → requests, responses, and shared DTOs
RTS.Application     → use cases and application interfaces
RTS.Infrastructure  → persistence and external-service implementations
RTS.Web             → pages, components, and application composition
```

The `RTS` technical prefix, `RTSDB` database name, `.RTS.Auth` cookie name, and data-protection name identify the overall Ranked Trading System and should remain stable as individual models are added.

## Product Direction

RTS is being developed as a configurable, multi-model evaluation platform. The initial methodology is the first system template rather than the permanent definition of the product.

The planned workflow combines deterministic screening, AI-assisted catalyst discovery, and manual candidates in a Candidate Inbox. Users choose which candidates proceed to durable evaluation jobs. RTS then retrieves market data, generates model-driven charts, calculates indicators and scores, and produces one canonical evidence-backed evaluation.

The same canonical evaluation can be presented as a narrative report, interactive dashboard, or analyst workbench. RTS may initialize presentation using experience, age range, generational preference, accessibility, and explicit settings, then optionally learn from user interaction. Learned presentation preferences never change the underlying facts, rankings, calculations, or scores.

Generative AI assists with structured strategy drafting, cited discovery, grounded explanation, chart interpretation, and approved agentic workflows. Deterministic rules remain authoritative, material state changes require user approval, and autonomous trade execution is outside the product plan.

See [the product, AI, and adaptive-presentation design](docs/product-ai-and-presentation.md) for the complete workflow and boundaries.

## Future Enhancements

- Add durable background-job persistence and an `RTS.Worker` host as screening and scoring workloads require it
- Add provider-neutral market-data and historical-price adapters
- Add specialized financial charting behind an RTS-owned Blazor component wrapper
- Replace in-memory account-request throttling with a distributed or infrastructure-level limiter when deploying multiple instances
- Add external monitoring integration and alerting for the health endpoints
- Expand authorization policies as product-specific permissions are introduced
- Add further automated coverage as new product-specific capabilities are introduced
- Add an API host when a standalone client, mobile application, or external integration requires it

## Documentation

Additional design decisions are documented in:

```text
docs/architecture.md
docs/product-ai-and-presentation.md
docs/production-configuration.md
docs/RTS development checklist.md
.github/copilot-instructions.md
```

The repository instructions define mandatory architectural and database rules for AI-assisted development.

## License

No license has been selected yet. Until a license is added, all rights are reserved.
