using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RTS.Application.Identity;

namespace RTS.Infrastructure.Identity;

public sealed class MemoryAccountRequestLimiter(
    IMemoryCache cache,
    IOptions<AccountRequestLimitOptions> options)
    : IAccountRequestLimiter
{
    private readonly object _syncRoot = new();

    public bool TryAcquire(
        AccountRequestType requestType,
        string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var permitLimit =
            Math.Max(1, options.Value.PermitLimit);

        var window =
            TimeSpan.FromMinutes(
                Math.Max(1, options.Value.WindowMinutes));

        var cacheKey = CreateCacheKey(
            requestType,
            identifier);

        lock (_syncRoot)
        {
            if (!cache.TryGetValue<RequestWindow>(
                    cacheKey,
                    out var requestWindow) ||
                requestWindow is null)
            {
                requestWindow = new RequestWindow();

                cache.Set(
                    cacheKey,
                    requestWindow,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            window
                    });
            }

            if (requestWindow.RequestCount >= permitLimit)
            {
                return false;
            }

            requestWindow.RequestCount++;
            return true;
        }
    }

    private static string CreateCacheKey(
        AccountRequestType requestType,
        string identifier)
    {
        var normalizedIdentifier =
            identifier.Trim().ToUpperInvariant();

        var identifierBytes =
            Encoding.UTF8.GetBytes(normalizedIdentifier);

        var identifierHash =
            SHA256.HashData(identifierBytes);

        return
            $"account-request:{requestType}:" +
            Convert.ToHexString(identifierHash);
    }

    private sealed class RequestWindow
    {
        public int RequestCount { get; set; }
    }
}