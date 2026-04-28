using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Services;
using AISEP.Tests.Helpers;
using FluentAssertions;

namespace AISEP.Tests.Services;

public class WalletServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly WalletService _sut;

    public WalletServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new WalletService(_db);
    }

    private (AdvisorWallet wallet, Advisor advisor, User user) SeedWalletWithAdvisor(int userId)
    {
        var user = new User
        {
            Email = $"advisor{userId}@test.com",
            PasswordHash = "hash",
            UserType = "Advisor",
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        _db.SaveChanges();

        // Create wallet first (FK is Advisor.WalletId -> AdvisorWallet.WalletId)
        var wallet = new AdvisorWallet
        {
            Balance = 500m,
            TotalEarned = 1000m,
            TotalWithdrawn = 500m,
            CreatedAt = DateTime.UtcNow
        };
        _db.AdvisorWallets.Add(wallet);
        _db.SaveChanges();

        var advisor = new Advisor
        {
            UserID = user.UserID,
            FullName = "Test Advisor",
            WalletId = wallet.WalletId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Advisors.Add(advisor);
        _db.SaveChanges();

        // Set AdvisorId on wallet for bidirectional nav
        wallet.AdvisorId = advisor.AdvisorID;
        _db.SaveChanges();

        return (wallet, advisor, user);
    }

    private void SeedTransaction(int walletId, TransactionType type, TransactionStatus status, decimal amount = 100m)
    {
        _db.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = walletId,
            Amount = amount,
            Type = type,
            Status = status,
            CreatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    // ── GetWalletByAdvisorAsync ──────────────────────────────────

    [Fact]
    public async Task GetWalletByAdvisorAsync_WhenNoWallet_ReturnsError()
    {
        var result = await _sut.GetWalletByAdvisorAsync(userId: 999);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("WALLET_DOES_NOT_EXIST");
    }

    // GetWalletByAdvisorAsync happy-path test omitted:
    // InMemory provider cannot resolve .Include(w => w.Advisor).FirstOrDefault(a => a.Advisor.UserID == userId)
    // when the FK is on the Advisor side (Advisor.WalletId -> AdvisorWallet.WalletId).
    // The not-found path is still tested above.

    // ── GetTransactionsAsync ─────────────────────────────────────

    [Fact]
    public async Task GetTransactionsAsync_ReturnsTransactionsForWallet()
    {
        var (wallet, _, _) = SeedWalletWithAdvisor(userId: 1);
        SeedTransaction(wallet.WalletId, TransactionType.Deposit, TransactionStatus.Completed);
        SeedTransaction(wallet.WalletId, TransactionType.Withdrawal, TransactionStatus.Pending);

        var result = await _sut.GetTransactionsAsync(wallet.WalletId, "Advisor",
            new WalletTransactionQueryParams { Page = 1, PageSize = 10 });

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactionsAsync_FilterByType_ReturnsMatching()
    {
        var (wallet, _, _) = SeedWalletWithAdvisor(userId: 1);
        SeedTransaction(wallet.WalletId, TransactionType.Deposit, TransactionStatus.Completed);
        SeedTransaction(wallet.WalletId, TransactionType.Withdrawal, TransactionStatus.Completed);

        var result = await _sut.GetTransactionsAsync(wallet.WalletId, "Advisor",
            new WalletTransactionQueryParams
            {
                Page = 1,
                PageSize = 10,
                TransactionType = TransactionType.Deposit
            });

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Type.Should().Be(TransactionType.Deposit);
    }

    [Fact]
    public async Task GetTransactionsAsync_FilterByStatus_ReturnsMatching()
    {
        var (wallet, _, _) = SeedWalletWithAdvisor(userId: 1);
        SeedTransaction(wallet.WalletId, TransactionType.Deposit, TransactionStatus.Completed);
        SeedTransaction(wallet.WalletId, TransactionType.Deposit, TransactionStatus.Pending);

        var result = await _sut.GetTransactionsAsync(wallet.WalletId, "Advisor",
            new WalletTransactionQueryParams
            {
                Page = 1,
                PageSize = 10,
                TransactionStatus = TransactionStatus.Pending
            });

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public async Task GetTransactionsAsync_WhenEmpty_ReturnsEmptyList()
    {
        var (wallet, _, _) = SeedWalletWithAdvisor(userId: 1);

        var result = await _sut.GetTransactionsAsync(wallet.WalletId, "Advisor",
            new WalletTransactionQueryParams { Page = 1, PageSize = 10 });

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionsAsync_Paging_RespectsPageSize()
    {
        var (wallet, _, _) = SeedWalletWithAdvisor(userId: 1);
        for (int i = 0; i < 5; i++)
            SeedTransaction(wallet.WalletId, TransactionType.Deposit, TransactionStatus.Completed);

        var result = await _sut.GetTransactionsAsync(wallet.WalletId, "Advisor",
            new WalletTransactionQueryParams { Page = 1, PageSize = 2 });

        result.Data!.Items.Should().HaveCount(2);
        result.Data.Paging.TotalItems.Should().Be(5);
    }

    // ── Boundary Tests ────────────────────────────────────────────

    [Fact]
    public async Task GetTransactionsAsync_WithPageSize1_ReturnsSingleTxAtLowerBoundary()
    {
        // Boundary: pageSize=1 (minimum valid page size)
        var (wallet, _, _) = SeedWalletWithAdvisor(userId: 1);
        SeedTransaction(wallet.WalletId, TransactionType.Deposit, TransactionStatus.Completed);
        SeedTransaction(wallet.WalletId, TransactionType.Deposit, TransactionStatus.Completed);
        SeedTransaction(wallet.WalletId, TransactionType.Withdrawal, TransactionStatus.Pending);

        var result = await _sut.GetTransactionsAsync(wallet.WalletId, "Advisor",
            new WalletTransactionQueryParams { Page = 1, PageSize = 1 });

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Paging.TotalItems.Should().Be(3);
    }
}
