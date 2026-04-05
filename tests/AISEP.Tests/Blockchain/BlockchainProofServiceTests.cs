using AISEP.Application.Configuration;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Domain.Interfaces;
using AISEP.Infrastructure.Data;
using AISEP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AISEP.Tests.Blockchain;

public class BlockchainProofServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly StubBlockchainService _stubBlockchain;
    private readonly Mock<IAuditService> _auditMock;
    private readonly BlockchainProofService _sut;

    // Test data
    private const int UserId = 100;
    private const int StartupId = 10;
    private const int DocumentId = 1;

    public BlockchainProofServiceTests()
    {
        // InMemory DB — unique name per test instance to avoid cross-contamination
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _stubBlockchain = new StubBlockchainService(
            new Mock<ILogger<StubBlockchainService>>().Object);
        _auditMock = new Mock<IAuditService>();
        // Audit mock accepts any call
        _auditMock.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var blockchainSettings = Options.Create(new BlockchainSettings
        {
            Provider = "Stub",
            NetworkName = "TestNet"
        });

        _sut = new BlockchainProofService(
            _context,
            _stubBlockchain,
            _auditMock.Object,
            new Mock<ILogger<BlockchainProofService>>().Object,
            blockchainSettings);

        SeedData();
    }

    private void SeedData()
    {
        var user = new User { UserID = UserId, Email = "startup@test.com" };
        _context.Users.Add(user);

        var startup = new Startup
        {
            StartupID = StartupId,
            UserID = UserId,
            CompanyName = "Test Startup",
            OneLiner = "Test",
            FullNameOfApplicant = "Test User",
            RoleOfApplicant = "CEO",
            ContactEmail = "startup@test.com",
            BusinessCode = "BC001",
            User = user
        };
        _context.Startups.Add(startup);

        // Document with a real accessible URL for hash computation (we'll test the flow, not actual HTTP)
        var doc = new Document
        {
            DocumentID = DocumentId,
            StartupID = StartupId,
            DocumentType = DocumentType.Pitch_Deck,
            Title = "Test Pitch Deck",
            FileURL = "https://example.com/test.pdf",
            UploadedAt = DateTime.UtcNow,
            Startup = startup
        };
        _context.Documents.Add(doc);

        // Second document with pre-existing proof/hash
        var doc2 = new Document
        {
            DocumentID = 2,
            StartupID = StartupId,
            DocumentType = DocumentType.Bussiness_Plan,
            Title = "Business Plan",
            FileURL = "https://example.com/plan.pdf",
            UploadedAt = DateTime.UtcNow,
            Startup = startup
        };
        _context.Documents.Add(doc2);

        var existingProof = new DocumentBlockchainProof
        {
            ProofID = 100,
            DocumentID = 2,
            FileHash = "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890",
            HashAlgorithm = "SHA-256",
            ProofStatus = ProofStatus.HashComputed,
            AnchoredBy = UserId
        };
        _context.DocumentBlockchainProofs.Add(existingProof);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // ═══════════════════════════════════════════
    // ComputeHashAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ComputeHash_DocumentNotFound_ReturnsError()
    {
        var result = await _sut.ComputeHashAsync(999, UserId);

        Assert.False(result.Success);
        Assert.Contains("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task ComputeHash_WrongUser_ReturnsError()
    {
        var result = await _sut.ComputeHashAsync(DocumentId, 999); // wrong user

        Assert.False(result.Success);
        Assert.Contains("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task ComputeHash_ExistingProof_ReturnsExistingHash()
    {
        // Document 2 already has a proof with hash
        var result = await _sut.ComputeHashAsync(2, UserId);

        Assert.True(result.Success);
        Assert.Equal("SHA-256", result.Data!.Algorithm);
        Assert.Equal("abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890", result.Data.FileHash);
        Assert.Equal(2, result.Data.DocumentID);
    }

    // ═══════════════════════════════════════════
    // SubmitToChainAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task SubmitToChain_DocumentNotFound_ReturnsError()
    {
        var result = await _sut.SubmitToChainAsync(999, UserId);

        Assert.False(result.Success);
        Assert.Contains("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task SubmitToChain_WithExistingHash_SubmitsSuccessfully()
    {
        // Document 2 has existing hash — should submit without needing to download file
        var result = await _sut.SubmitToChainAsync(2, UserId);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.DocumentID);
        Assert.StartsWith("0x", result.Data.TransactionHash);
        Assert.Equal("Pending", result.Data.Status);
        Assert.Equal("abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890", result.Data.FileHash);

        // Verify DB was updated
        var proof = await _context.DocumentBlockchainProofs.FirstAsync(p => p.DocumentID == 2);
        Assert.Equal(ProofStatus.Pending, proof.ProofStatus);
        Assert.NotNull(proof.TransactionHash);
        Assert.Equal("TestNet", proof.BlockchainNetwork);
    }

    // ═══════════════════════════════════════════
    // VerifyOnChainAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task VerifyOnChain_DocumentNotFound_ReturnsError()
    {
        var result = await _sut.VerifyOnChainAsync(999, UserId);

        Assert.False(result.Success);
    }

    // ═══════════════════════════════════════════
    // GetTxStatusAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task GetTxStatus_NoProofSubmitted_ReturnsError()
    {
        // Document 2 has hash but no tx submitted yet
        var result = await _sut.GetTxStatusAsync(2, UserId);

        Assert.False(result.Success);
        Assert.Contains("NOT_SUBMITTED", result.Error!.Code);
    }

    [Fact]
    public async Task GetTxStatus_AfterSubmit_ReturnsConfirmed()
    {
        // First submit to chain
        var submitResult = await _sut.SubmitToChainAsync(2, UserId);
        Assert.True(submitResult.Success);

        // Then check tx status
        var statusResult = await _sut.GetTxStatusAsync(2, UserId);

        Assert.True(statusResult.Success);
        Assert.Equal("Confirmed", statusResult.Data!.Status);
        Assert.NotNull(statusResult.Data.BlockNumber);
        Assert.NotNull(statusResult.Data.ConfirmedAt);
        Assert.Equal(2, statusResult.Data.DocumentID);

        // DB should be updated to Anchored
        var proof = await _context.DocumentBlockchainProofs.FirstAsync(p => p.DocumentID == 2);
        Assert.Equal(ProofStatus.Anchored, proof.ProofStatus);
    }

    // ═══════════════════════════════════════════
    // StaffVerifyHashAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task StaffVerifyHash_DocumentNotFound_ReturnsError()
    {
        var result = await _sut.StaffVerifyHashAsync(999, UserId);

        Assert.False(result.Success);
        Assert.Contains("NOT_FOUND", result.Error!.Code);
    }

    // ═══════════════════════════════════════════
    // Full flow: ComputeHash -> Submit -> TxStatus
    // ═══════════════════════════════════════════

    [Fact]
    public async Task FullFlow_ComputeHash_Submit_CheckStatus()
    {
        // Step 1: ComputeHash (uses existing hash from doc 2)
        var hashResult = await _sut.ComputeHashAsync(2, UserId);
        Assert.True(hashResult.Success);
        var fileHash = hashResult.Data!.FileHash;

        // Step 2: Submit to blockchain
        var submitResult = await _sut.SubmitToChainAsync(2, UserId);
        Assert.True(submitResult.Success);
        Assert.Equal(fileHash, submitResult.Data!.FileHash);
        Assert.Equal("Pending", submitResult.Data.Status);
        var txHash = submitResult.Data.TransactionHash;

        // Step 3: Check transaction status
        var statusResult = await _sut.GetTxStatusAsync(2, UserId);
        Assert.True(statusResult.Success);
        Assert.Equal("Confirmed", statusResult.Data!.Status);
        Assert.Equal(txHash, statusResult.Data.TransactionHash);
        Assert.NotNull(statusResult.Data.BlockNumber);

        // Verify final DB state
        var proof = await _context.DocumentBlockchainProofs.FirstAsync(p => p.DocumentID == 2);
        Assert.Equal(ProofStatus.Anchored, proof.ProofStatus);
        Assert.Equal(txHash, proof.TransactionHash);
        Assert.Equal("TestNet", proof.BlockchainNetwork);
        Assert.NotNull(proof.BlockNumber);
    }

    [Fact]
    public async Task SubmitToChain_WrongUser_ReturnsError()
    {
        var result = await _sut.SubmitToChainAsync(2, 999);

        Assert.False(result.Success);
        Assert.Contains("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task GetTxStatus_WrongUser_ReturnsError()
    {
        var result = await _sut.GetTxStatusAsync(2, 999);

        Assert.False(result.Success);
    }
}
