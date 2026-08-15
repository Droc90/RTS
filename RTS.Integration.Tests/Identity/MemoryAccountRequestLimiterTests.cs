using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RTS.Application.Identity;
using RTS.Infrastructure.Identity;

namespace RTS.Integration.Tests.Identity;

public sealed class MemoryAccountRequestLimiterTests
{
    [Fact]
    public void TryAcquire_AllowsConfiguredNumberOfRequests()
    {
        using var cache = new MemoryCache(
            new MemoryCacheOptions());

        var limiter = CreateLimiter(cache, permitLimit: 3);

        Assert.True(limiter.TryAcquire(
            AccountRequestType.PasswordReset,
            "user@example.com"));

        Assert.True(limiter.TryAcquire(
            AccountRequestType.PasswordReset,
            "user@example.com"));

        Assert.True(limiter.TryAcquire(
            AccountRequestType.PasswordReset,
            "user@example.com"));

        Assert.False(limiter.TryAcquire(
            AccountRequestType.PasswordReset,
            "user@example.com"));
    }

    [Fact]
    public void TryAcquire_MaintainsSeparateRequestTypeCounters()
    {
        using var cache = new MemoryCache(
            new MemoryCacheOptions());

        var limiter = CreateLimiter(cache, permitLimit: 1);

        Assert.True(limiter.TryAcquire(
            AccountRequestType.EmailConfirmation,
            "user@example.com"));

        Assert.False(limiter.TryAcquire(
            AccountRequestType.EmailConfirmation,
            "user@example.com"));

        Assert.True(limiter.TryAcquire(
            AccountRequestType.PasswordReset,
            "user@example.com"));

        Assert.False(limiter.TryAcquire(
            AccountRequestType.PasswordReset,
            "user@example.com"));
    }

    [Fact]
    public void TryAcquire_NormalizesEmailAddress()
    {
        using var cache = new MemoryCache(
            new MemoryCacheOptions());

        var limiter = CreateLimiter(cache, permitLimit: 2);

        Assert.True(limiter.TryAcquire(
            AccountRequestType.PasswordReset,
            "user@example.com"));

        Assert.True(limiter.TryAcquire(
            AccountRequestType.PasswordReset,
            " USER@EXAMPLE.COM "));

        Assert.False(limiter.TryAcquire(
            AccountRequestType.PasswordReset,
            "User@Example.com"));
    }

    private static MemoryAccountRequestLimiter CreateLimiter(
        IMemoryCache cache,
        int permitLimit)
    {
        var options = Options.Create(
            new AccountRequestLimitOptions
            {
                PermitLimit = permitLimit,
                WindowMinutes = 15
            });

        return new MemoryAccountRequestLimiter(
            cache,
            options);
    }
}