using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Services;
using AISEP.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AISEP.Tests.Services;

public class AuditServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IHttpContextAccessor> _httpAccessor = new();
    private readonly AuditService _sut;

    public AuditServiceTests()
    {
        _db = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<AuditService>>();
        _sut = new AuditService(_db, _httpAccessor.Object, logger.Object);
    }

    [Fact]
    public async Task LogAsync_FullParams_PersistsAuditLog()
    {
        await _sut.LogAsync(userId: 1, "CREATE", "Startup", 10, "Created startup", "127.0.0.1", "TestAgent");

        _db.AuditLogs.Should().ContainSingle(a =>
            a.UserID == 1 &&
            a.ActionType == "CREATE" &&
            a.EntityType == "Startup" &&
            a.EntityID == 10 &&
            a.IPAddress == "127.0.0.1" &&
            a.UserAgent == "TestAgent");
    }

    [Fact]
    public async Task LogAsync_FullParams_WithNullUserId_PersistsLog()
    {
        await _sut.LogAsync(userId: null, "SYSTEM_EVENT", "System", null, "heartbeat", "0.0.0.0", "cron");

        _db.AuditLogs.Should().ContainSingle(a => a.UserID == null && a.ActionType == "SYSTEM_EVENT");
    }

    [Fact]
    public async Task LogAsync_ShortParams_ExtractsUserFromHttpContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "42")
        }));
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.1");
        ctx.Request.Headers["User-Agent"] = "Mozilla/5.0";
        _httpAccessor.Setup(h => h.HttpContext).Returns(ctx);

        await _sut.LogAsync("UPDATE", "Investor", 5, "Updated profile");

        var log = _db.AuditLogs.Single();
        log.UserID.Should().Be(42);
        log.IPAddress.Should().Be("10.0.0.1");
        log.UserAgent.Should().Be("Mozilla/5.0");
        log.ActionType.Should().Be("UPDATE");
    }

    [Fact]
    public async Task LogAsync_ShortParams_WhenNoHttpContext_UsesDefaults()
    {
        _httpAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        await _sut.LogAsync("DELETE", "Document", 3, "Removed doc");

        var log = _db.AuditLogs.Single();
        log.UserID.Should().BeNull();
        log.IPAddress.Should().Be("unknown");
        log.UserAgent.Should().Be("unknown");
    }

    [Fact]
    public async Task LogAsync_ShortParams_WhenSubClaimNotParseable_UserIdIsNull()
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "not-a-number")
        }));
        _httpAccessor.Setup(h => h.HttpContext).Returns(ctx);

        await _sut.LogAsync("VIEW", "Report", 1, null);

        _db.AuditLogs.Single().UserID.Should().BeNull();
    }

    [Fact]
    public async Task LogAsync_FullParams_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow;

        await _sut.LogAsync(1, "TEST", "Entity", 1, null, "ip", "ua");

        var log = _db.AuditLogs.Single();
        log.CreatedAt.Should().BeOnOrAfter(before);
        log.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
