using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RTS.Application.Auditing;
using RTS.Infrastructure.Auditing;
using RTS.Infrastructure.Persistence;

namespace RTS.Integration.Tests.Auditing;

public sealed class AuditServiceTests
{
    [Fact]
    public async Task LoginHistoryService_RecordsLoginEvent()
    {
        await using var context = CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var service = new LoginHistoryService(context);
        var correlationId = Guid.NewGuid();

        await service.RecordAsync(
            new RecordLoginHistoryRequest(
                null,
                " AuditUser@Example.com ",
                LoginEventType.LoginFailed,
                false,
                LoginFailureCode.UserNotFound,
                "127.0.0.1",
                "RTS integration test",
                correlationId));

        var history = await context.LoginHistory
            .AsNoTracking()
            .SingleAsync(item =>
                item.CorrelationId == correlationId);

        Assert.Null(history.UserId);
        Assert.NotNull(history.IdentifierHash);
        Assert.Equal(32, history.IdentifierHash.Length);
        Assert.Equal(
            LoginEventType.LoginFailed.ToString(),
            history.EventType);
        Assert.False(history.IsSuccess);
        Assert.Equal(
            LoginFailureCode.UserNotFound.ToString(),
            history.FailureCode);
        Assert.Equal("127.0.0.1", history.IpAddress);
        Assert.Equal(
            "RTS integration test",
            history.UserAgent);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task AuditService_RecordsAdministratorAction()
    {
        await using var context = CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var service = new AuditService(context);
        var targetExternalId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        await service.RecordAsync(
            new RecordAuditRequest(
                null,
                AuditAction.UserDeactivated,
                "UserAdministration",
                "User",
                targetExternalId,
                new Dictionary<string, object?>
                {
                    ["IsActive"] = true
                },
                new Dictionary<string, object?>
                {
                    ["IsActive"] = false
                },
                true,
                CorrelationId: correlationId));

        var auditLog = await context.AuditLogs
            .AsNoTracking()
            .SingleAsync(item =>
                item.CorrelationId == correlationId);

        Assert.Null(auditLog.UserId);
        Assert.Equal(
            AuditAction.UserDeactivated.ToString(),
            auditLog.Action);
        Assert.Equal(
            "UserAdministration",
            auditLog.EventCategory);
        Assert.Equal("User", auditLog.EntityType);
        Assert.Equal(
            targetExternalId,
            auditLog.EntityExternalId);
        Assert.True(auditLog.IsSuccess);
        Assert.Null(auditLog.FailureReason);

        using var oldValues =
            JsonDocument.Parse(auditLog.OldValues!);

        using var newValues =
            JsonDocument.Parse(auditLog.NewValues!);

        Assert.True(
            oldValues.RootElement
                .GetProperty("IsActive")
                .GetBoolean());

        Assert.False(
            newValues.RootElement
                .GetProperty("IsActive")
                .GetBoolean());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task ApplicationErrorService_RecordsError()
    {
        await using var context = CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var service = new ApplicationErrorService(context);
        var correlationId = Guid.NewGuid();

        await service.RecordAsync(
            new RecordApplicationErrorRequest(
                null,
                correlationId,
                "InvalidOperationException",
                "TEST_ERROR",
                "An unexpected error occurred.",
                "Integration-test diagnostic details.",
                "AuditServiceTests",
                "/integration-test",
                "GET",
                500));

        var applicationError =
            await context.ApplicationErrors
                .AsNoTracking()
                .SingleAsync(error =>
                    error.CorrelationId == correlationId);

        Assert.Null(applicationError.UserId);
        Assert.Equal(
            "InvalidOperationException",
            applicationError.ErrorType);
        Assert.Equal(
            "TEST_ERROR",
            applicationError.ErrorCode);
        Assert.Equal(
            "An unexpected error occurred.",
            applicationError.SafeMessage);
        Assert.Equal(
            "Integration-test diagnostic details.",
            applicationError.DiagnosticDetails);
        Assert.Equal(
            "AuditServiceTests",
            applicationError.Source);
        Assert.Equal(
            "/integration-test",
            applicationError.RequestPath);
        Assert.Equal("GET", applicationError.HttpMethod);
        Assert.Equal(500, applicationError.StatusCode);
        Assert.Equal("New", applicationError.ResolutionStatus);
        Assert.Null(applicationError.ResolvedUtc);
        Assert.NotEmpty(applicationError.RowVersion);

        await transaction.RollbackAsync();
    }
    private static RtsDbContext CreateDbContext()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "RTS_TEST_CONNECTION_STRING")
            ?? "Server=localhost;Database=RTSIntegrationTests;" +
               "Trusted_Connection=True;" +
               "TrustServerCertificate=True;" +
               "MultipleActiveResultSets=True";

        var options =
            new DbContextOptionsBuilder<RtsDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        return new RtsDbContext(options);
    }
}