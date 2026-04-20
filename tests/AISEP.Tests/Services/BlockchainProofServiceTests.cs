using AISEP.Application.Configuration;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Domain.Interfaces;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Services;
using AISEP.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AISEP.Tests.Services;

public class BlockchainProofServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<IBlockchainService> _chain = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly Mock<ICloudinaryService> _cloud = new();
    private readonly BlockchainProofService _sut;

    public BlockchainProofServiceTests()
    {
        _db = TestDbContextFactory.Create();
        var settings = Options.Create(new BlockchainSettings
        {
            NetworkName = "Sepolia",
            EtherscanBaseUrl = "https://sepolia.etherscan.io"
        });
        var logger = new Mock<ILogger<BlockchainProofService>>();
        _sut = new BlockchainProofService(_db, _chain.Object, _audit.Object, _cloud.Object, logger.Object, settings);
    }

    private (Startup startup, Document doc) SeedDocument(int userId = 1, bool archived = false, string? fileHash = null, ProofStatus? status = null)
    {
        var startup = new Startup
        {
            UserID = userId,
            CompanyName = "Acme",
            OneLiner = "test",
            CreatedAt = DateTime.UtcNow
        };
        _db.Startups.Add(startup);
        _db.SaveChanges();

        var doc = new Document
        {
            StartupID = startup.StartupID,
            DocumentType = DocumentType.Pitch_Deck,
            FileURL = "https://res.cloudinary.com/test/raw/upload/v1/doc.pdf",
            UploadedAt = DateTime.UtcNow,
            IsArchived = archived
        };
        _db.Documents.Add(doc);
        _db.SaveChanges();

        if (fileHash != null)
        {
            _db.DocumentBlockchainProofs.Add(new DocumentBlockchainProof
            {
                DocumentID = doc.DocumentID,
                FileHash = fileHash,
                HashAlgorithm = "SHA-256",
                ProofStatus = status ?? ProofStatus.HashComputed,
                AnchoredBy = userId
            });
            _db.SaveChanges();
        }

        return (startup, doc);
    }

    [Fact]
    public async Task ComputeHashAsync_WhenDocumentNotOwned_ReturnsNotFound()
    {
        SeedDocument(userId: 1);

        var result = await _sut.ComputeHashAsync(documentId: 1, userId: 999);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("DOCUMENT_NOT_FOUND");
    }

    [Fact]
    public async Task ComputeHashAsync_WhenDocumentArchived_ReturnsArchivedError()
    {
        var (_, doc) = SeedDocument(userId: 1, archived: true);

        var result = await _sut.ComputeHashAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("DOCUMENT_ARCHIVED");
    }

    [Fact]
    public async Task ComputeHashAsync_WhenHashAlreadyExists_ReusesIt_WithoutDownloadingFile()
    {
        var seededHash = new string('a', 64);
        var (_, doc) = SeedDocument(userId: 1, fileHash: seededHash);

        var result = await _sut.ComputeHashAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeTrue();
        result.Data!.FileHash.Should().Be(seededHash);
        result.Data.Algorithm.Should().Be("SHA-256");
        _cloud.Verify(c => c.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ComputeHashAsync_WhenNoHash_DownloadsFileAndComputesSha256()
    {
        var (_, doc) = SeedDocument(userId: 1);
        var content = System.Text.Encoding.UTF8.GetBytes("hello world");
        var expectedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)).ToLowerInvariant();

        _cloud.Setup(c => c.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var result = await _sut.ComputeHashAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeTrue();
        result.Data!.FileHash.Should().Be(expectedHash);
        _db.DocumentBlockchainProofs.Should().ContainSingle(p => p.FileHash == expectedHash);
    }

    [Fact]
    public async Task SubmitToChainAsync_WhenAlreadyAnchored_ReturnsAlreadySubmitted()
    {
        var (_, doc) = SeedDocument(userId: 1, fileHash: "aa", status: ProofStatus.Anchored);

        var result = await _sut.SubmitToChainAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("PROOF_ALREADY_SUBMITTED");
        _chain.Verify(c => c.SubmitHashAsync(It.IsAny<string>(), It.IsAny<BlockchainSubmitMeta>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitToChainAsync_OnSuccess_StoresTxHashAndMarksPending()
    {
        var (_, doc) = SeedDocument(userId: 1, fileHash: "deadbeef", status: ProofStatus.HashComputed);

        _chain.Setup(c => c.SubmitHashAsync("deadbeef", It.IsAny<BlockchainSubmitMeta>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("0x1234567890abcdef1234567890abcdef");

        var result = await _sut.SubmitToChainAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeTrue();
        result.Data!.TransactionHash.Should().StartWith("0x");
        result.Data.EtherscanUrl.Should().Contain("sepolia.etherscan.io/tx/");
        var proof = _db.DocumentBlockchainProofs.Single();
        proof.ProofStatus.Should().Be(ProofStatus.Pending);
        proof.TransactionHash.Should().StartWith("0x");
    }

    [Fact]
    public async Task SubmitToChainAsync_WhenChainThrowsAlreadyRegistered_ReturnsHashAlreadyExists()
    {
        var (_, doc) = SeedDocument(userId: 1, fileHash: "deadbeef", status: ProofStatus.HashComputed);

        _chain.Setup(c => c.SubmitHashAsync(It.IsAny<string>(), It.IsAny<BlockchainSubmitMeta>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Hash already registered on chain"));

        var result = await _sut.SubmitToChainAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("HASH_ALREADY_EXISTS");
    }

    [Fact]
    public async Task VerifyOnChainAsync_WhenNoProof_ReturnsNotFoundStatus()
    {
        var (_, doc) = SeedDocument(userId: 1);

        var result = await _sut.VerifyOnChainAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("NotFound");
        result.Data.OnChainVerified.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyOnChainAsync_WhenOnChain_ReturnsVerifiedAndUpdatesStatus()
    {
        var (_, doc) = SeedDocument(userId: 1, fileHash: "cafe", status: ProofStatus.Pending);

        _chain.Setup(c => c.VerifyHashAsync("cafe", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.VerifyOnChainAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeTrue();
        result.Data!.OnChainVerified.Should().BeTrue();
        result.Data.Status.Should().Be("Verified");
        _db.DocumentBlockchainProofs.Single().ProofStatus.Should().Be(ProofStatus.Anchored);
    }

    [Fact]
    public async Task VerifyOnChainAsync_WhenChainThrows_ReturnsBlockchainError()
    {
        var (_, doc) = SeedDocument(userId: 1, fileHash: "cafe", status: ProofStatus.Pending);

        _chain.Setup(c => c.VerifyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("rpc down"));

        var result = await _sut.VerifyOnChainAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("BLOCKCHAIN_ERROR");
    }

    [Fact]
    public async Task GetTxStatusAsync_WhenDocumentNotOwned_ReturnsNotFound()
    {
        SeedDocument(userId: 1);

        var result = await _sut.GetTxStatusAsync(documentId: 1, userId: 999);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("DOCUMENT_NOT_FOUND");
    }

    [Fact]
    public async Task GetTxStatusAsync_WhenNoTransactionSubmitted_ReturnsProofNotSubmitted()
    {
        var (_, doc) = SeedDocument(userId: 1, fileHash: "aa", status: ProofStatus.HashComputed);

        var result = await _sut.GetTxStatusAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("PROOF_NOT_SUBMITTED");
    }

    [Fact]
    public async Task GetTxStatusAsync_WhenConfirmed_UpdatesProofToAnchored()
    {
        var (_, doc) = SeedDocument(userId: 1, fileHash: "cafe", status: ProofStatus.Pending);
        var proof = _db.DocumentBlockchainProofs.Single();
        proof.TransactionHash = "0xabc";
        await _db.SaveChangesAsync();

        _chain.Setup(c => c.GetTxStatusAsync("0xabc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlockchainTxStatusResult
            {
                Status = "Confirmed",
                BlockNumber = "12345",
                ConfirmedAt = DateTime.UtcNow
            });

        var result = await _sut.GetTxStatusAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("Confirmed");
        _db.DocumentBlockchainProofs.Single().ProofStatus.Should().Be(ProofStatus.Anchored);
    }

    [Fact]
    public async Task GetTxStatusAsync_WhenChainThrows_ReturnsBlockchainError()
    {
        var (_, doc) = SeedDocument(userId: 1, fileHash: "cafe", status: ProofStatus.Pending);
        var proof = _db.DocumentBlockchainProofs.Single();
        proof.TransactionHash = "0xabc";
        await _db.SaveChangesAsync();

        _chain.Setup(c => c.GetTxStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("rpc down"));

        var result = await _sut.GetTxStatusAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("BLOCKCHAIN_ERROR");
    }

    [Fact]
    public async Task StaffVerifyHashAsync_WhenDocumentNotFound_ReturnsNotFound()
    {
        var result = await _sut.StaffVerifyHashAsync(documentId: 9999, staffUserId: 1);

        result.Success.Should().BeFalse();
        result.Error!.Code.Should().Be("DOCUMENT_NOT_FOUND");
    }

    [Fact]
    public async Task StaffVerifyHashAsync_WhenVerified_LogsAudit()
    {
        var (_, doc) = SeedDocument(userId: 1, fileHash: "cafe", status: ProofStatus.Pending);

        _chain.Setup(c => c.VerifyHashAsync("cafe", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.StaffVerifyHashAsync(doc.DocumentID, staffUserId: 7);

        result.Success.Should().BeTrue();
        result.Data!.OnChainVerified.Should().BeTrue();
        _audit.Verify(a => a.LogAsync("STAFF_VERIFY_HASH", "DocumentBlockchainProof", doc.DocumentID, It.IsAny<string>()), Times.Once);
    }

    // ── Boundary Tests ────────────────────────────────────────────

    [Fact]
    public async Task ComputeHashAsync_WithLargeFile50MB_ComputesSha256AtUpperBoundary()
    {
        // Boundary: document at the 50MB upload cap declared in nginx-docker.conf.
        // Verifies SHA-256 is computed correctly for maximum-size input.
        var (_, doc) = SeedDocument(userId: 1);
        var content = new byte[50 * 1024 * 1024]; // 50 MB
        new Random(42).NextBytes(content);
        var expectedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)).ToLowerInvariant();

        _cloud.Setup(c => c.DownloadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        var result = await _sut.ComputeHashAsync(doc.DocumentID, userId: 1);

        result.Success.Should().BeTrue();
        result.Data!.FileHash.Should().Be(expectedHash);
        result.Data.Algorithm.Should().Be("SHA-256");
    }
}
