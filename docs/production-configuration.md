# Production Configuration

RTS uses layered ASP.NET Core configuration. Safe defaults are committed in
`appsettings.json` and `appsettings.Production.json`. Environment-specific
values, credentials, and secrets must be supplied by the deployment
environment.

Do not commit production connection strings, passwords, API keys,
certificates, or administrator email addresses.

## Required Production Settings

The following settings are required before RTS can start in Production:

| Setting | Purpose |
|---|---|
| `AllowedHosts` | Restricts accepted HTTP host headers |
| `Application__DataProtectionKeysPath` | Persists encryption keys across restarts and deployments |
| `ConnectionStrings__DefaultConnection` | Connects RTS to its SQL Server database |
| `Email__Smtp__Host` | Identifies the production SMTP server |
| `Email__Smtp__FromAddress` | Sets the sender email address |
| `Email__Smtp__FromName` | Sets the sender display name |

Production startup fails when `AllowedHosts` is missing or contains only `*`,
when the Data Protection key path is missing or relative, or when required
SMTP settings are missing or invalid.

## Environment Variables

ASP.NET Core maps double underscores in environment-variable names to
configuration section separators.

Example production values:

```text
ASPNETCORE_ENVIRONMENT=Production
AllowedHosts=rts.example.com
Application__DataProtectionKeysPath=C:\ProgramData\RTS\DataProtectionKeys
ConnectionStrings__DefaultConnection=<provided by the production secret store>
Email__Smtp__Host=smtp.example.com
Email__Smtp__FromAddress=no-reply@example.com
Email__Smtp__FromName=Ranked Trading System
```

For certificate-based Data Protection, also provide:

```text
Application__DataProtectionCertificatePath=<absolute path to a PKCS #12 certificate>
Application__DataProtectionCertificatePassword=<provided by the production secret store>
```

Multiple allowed hosts are separated with semicolons:

```text
AllowedHosts=rts.example.com;www.rts.example.com
```

On Linux or in a container, use an absolute mounted path:

```text
Application__DataProtectionKeysPath=/var/lib/rts/data-protection-keys
```

## SQL Server Connection

The production connection string must be supplied through an environment
variable or the deployment platform's secret-management service:

```text
ConnectionStrings__DefaultConnection
```

Do not add the production connection string to any committed
`appsettings*.json` file.

The configured database must already exist. RTS does not use EF Core
migrations or `EnsureCreated()` to create or modify its schema.

The database must use SQL Server compatibility level 170.

## Data Protection Keys

RTS uses ASP.NET Core Data Protection for authentication cookies and
security tokens. Production keys are persisted at the absolute path supplied
through:

```text
Application__DataProtectionKeysPath
```

The deployment identity must have read and write access to this directory.
The directory must be stored outside temporary application deployment files
and must survive restarts and replacements.

On Windows, RTS protects persisted production keys with Windows DPAPI when
no certificate is configured. DPAPI protection is tied to the Windows
deployment identity and is appropriate for a single Windows host with a
stable application identity.

Outside Windows, production requires a PKCS #12 certificate through:

```text
Application__DataProtectionCertificatePath
Application__DataProtectionCertificatePassword
```

The certificate path must be absolute. The certificate password is a secret
and must be supplied through an environment variable or secret store.

A certificate can also be used on Windows when keys must be shared across
hosts. For multiple application instances, every instance must use the same
key repository, certificate, and `Application:DataProtectionName`.

Restrict access to the key directory to the application identity. Production
hosting should also protect persisted keys at rest through operating-system,
certificate, managed key-vault, or hosting-platform protection appropriate to
the target environment.

Do not delete or replace active keys during a deployment. Doing so can
invalidate authentication cookies and outstanding Identity tokens.

## Host Filtering

The base development configuration permits all hosts for local convenience:

```json
{
  "AllowedHosts": "*"
}
```

Production must override this value with explicit host names. The application
will reject production startup when the effective value remains unrestricted.

Host filtering does not replace DNS, firewall, reverse-proxy, or web-server
security configuration.

## HTTPS and HSTS

RTS always redirects HTTP requests to HTTPS. Outside Development, it also
enables HSTS.

The committed production defaults are:

```json
{
  "Security": {
    "HstsMaxAgeDays": 365,
    "HstsIncludeSubDomains": true,
    "HstsPreload": false
  }
}
```

Only enable HSTS preload after confirming that the primary domain and all
subdomains are permanently HTTPS-enabled and satisfy browser preload
requirements.

Authentication cookies are configured as:

- Secure
- HTTP-only
- SameSite Lax
- Eight-hour lifetime
- Sliding expiration enabled

## Production Email Delivery

Development uses `DevelopmentEmailSender`, which writes confirmation and
password-reset messages to application logging.

Production uses a provider-neutral MailKit SMTP implementation. Configure it
with environment variables or the deployment platform's secret store:

```text
Email__Smtp__Host=smtp.example.com
Email__Smtp__Port=587
Email__Smtp__SocketOptions=StartTls
Email__Smtp__Username=<provided by the production secret store>
Email__Smtp__Password=<provided by the production secret store>
Email__Smtp__FromAddress=no-reply@example.com
Email__Smtp__FromName=Ranked Trading System
Email__Smtp__TimeoutSeconds=30
```

`SocketOptions` must be `StartTls` or `SslOnConnect`. Username and password
must either both be configured or both be omitted for a trusted SMTP relay.

RTS validates SMTP configuration at startup. This prevents registration,
confirmation, and password recovery from appearing available while email
delivery is misconfigured. Provider credentials must never be committed to
source control.

## Initial Administrator

The first administrator can be provisioned temporarily with:

```text
InitialAdministrator__Email=admin@example.com
```

The account must already exist and be active. Provisioning only operates when
no administrator currently exists.

Remove this setting immediately after the first administrator has been
successfully promoted. Additional administrators should be managed through
the protected user-administration interface.

## Logging

Production logging defaults to Warning while retaining application lifecycle
messages at Information:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

Do not log passwords, authentication tokens, connection strings, email API
keys, or private configuration values.

Application exception diagnostics are stored in
`Audit.ApplicationErrors` and are accessible only through the protected
administrator workflow.

## Health Monitoring

RTS exposes two anonymous, machine-readable health endpoints:

```text
/health/live
/health/ready
```

`/health/live` reports whether the application process is running.
`/health/ready` verifies that the application can connect to SQL Server and
returns HTTP 503 when the database is unavailable.

Both endpoints return JSON containing the overall status, individual check
statuses, and durations. Responses intentionally exclude exception messages,
stack traces, connection strings, and other diagnostic details.

Configure the hosting platform, load balancer, or monitoring service to use
`/health/live` for process restarts and `/health/ready` for routing decisions.
External access may be restricted at the network or hosting layer when public
health endpoints are not required.

## Reverse Proxies

When RTS is deployed behind IIS, Azure Application Gateway, Nginx, a load
balancer, or another reverse proxy, forwarded-header handling must be
configured for that hosting environment.

Only trust known proxies or known networks. Do not clear the default
forwarded-header trust restrictions or accept forwarded headers from every
source.

HTTPS redirection, secure cookies, correlation data, and client-address
auditing depend on the proxy forwarding the original request information
correctly.

## Production Verification Checklist

Before the first production release:

1. Set `ASPNETCORE_ENVIRONMENT` to `Production`.
2. Configure explicit `AllowedHosts`.
3. Provide `ConnectionStrings__DefaultConnection` through a secret store.
4. Configure an absolute, persistent Data Protection key path.
5. Restrict access to the Data Protection key repository.
6. Confirm Windows DPAPI protection or configure a production certificate.
7. Configure and test the production SMTP provider.
8. Confirm HTTPS redirection and the HSTS response header.
9. Confirm registration and email confirmation.
10. Confirm forgot-password and password-reset delivery.
11. Confirm authentication survives an application restart.
12. Confirm administrator authorization.
13. Confirm application-error recording and administration.
14. Verify `/health/live` and `/health/ready` from the monitoring environment.
15. Remove `InitialAdministrator__Email` after provisioning.
16. Run all automated tests.
17. Verify that no secrets were committed to source control.
