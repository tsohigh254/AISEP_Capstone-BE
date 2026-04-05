using AISEP.Domain.Enums;
using AISEP.Domain.Interfaces;
using AISEP.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AISEP.Tests.Blockchain;

public class StubBlockchainServiceTests
{
    private readonly StubBlockchainService _sut;

    public StubBlockchainServiceTests()
    {
        var logger = new Mock<ILogger<StubBlockchainService>>();
        _sut = new StubBlockchainService(logger.Object);
    }

    // ─── SubmitHashAsync ───

    [Fact]
    public async Task SubmitHashAsync_ReturnsValidTxHash()
    {
        var meta = new BlockchainSubmitMeta
        {
            DocumentID = 1,
            StartupID = 1,
            DocumentType = DocumentType.Pitch_Deck,
            FileName = "test.pdf"
        };

        var txHash = await _sut.SubmitHashAsync("abc123hash", meta);

        Assert.NotNull(txHash);
        Assert.StartsWith("0x", txHash);
        Assert.Equal(66, txHash.Length); // 0x + 64 hex chars
    }

    [Fact]
    public async Task SubmitHashAsync_DifferentHashes_ReturnDifferentTxHashes()
    {
        var meta = new BlockchainSubmitMeta
        {
            DocumentID = 1,
            StartupID = 1,
            DocumentType = DocumentType.Pitch_Deck,
            FileName = "test.pdf"
        };

        var tx1 = await _sut.SubmitHashAsync("hash_one", meta);
        var tx2 = await _sut.SubmitHashAsync("hash_two", meta);

        Assert.NotEqual(tx1, tx2);
    }

    // ─── VerifyHashAsync ───

    [Fact]
    public async Task VerifyHashAsync_AfterSubmit_ReturnsTrue()
    {
        var meta = new BlockchainSubmitMeta
        {
            DocumentID = 1,
            StartupID = 1,
            DocumentType = DocumentType.Pitch_Deck,
            FileName = "test.pdf"
        };

        await _sut.SubmitHashAsync("known_hash", meta);

        var result = await _sut.VerifyHashAsync("known_hash");

        Assert.True(result);
    }

    [Fact]
    public async Task VerifyHashAsync_UnknownHash_ReturnsFalse()
    {
        var result = await _sut.VerifyHashAsync("never_submitted_hash_" + Guid.NewGuid());

        Assert.False(result);
    }

    // ─── GetTxStatusAsync ───

    [Fact]
    public async Task GetTxStatusAsync_KnownTx_ReturnsConfirmed()
    {
        var meta = new BlockchainSubmitMeta
        {
            DocumentID = 1,
            StartupID = 1,
            DocumentType = DocumentType.Pitch_Deck,
            FileName = "test.pdf"
        };

        var txHash = await _sut.SubmitHashAsync("status_test_hash", meta);

        var status = await _sut.GetTxStatusAsync(txHash);

        Assert.Equal("Confirmed", status.Status);
        Assert.NotNull(status.BlockNumber);
        Assert.NotNull(status.ConfirmedAt);
    }

    [Fact]
    public async Task GetTxStatusAsync_UnknownTx_ReturnsFailed()
    {
        var status = await _sut.GetTxStatusAsync("0xunknown_tx_hash");

        Assert.Equal("Failed", status.Status);
        Assert.Null(status.BlockNumber);
        Assert.Null(status.ConfirmedAt);
    }
}
