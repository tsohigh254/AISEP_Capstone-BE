using AISEP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

/// <summary>
/// Stub / mock blockchain service for MVP development.
/// TODO: Replace with real RPC integration (Ethereum / Polygon / Hyperledger) for production.
///
/// Current behaviour:
/// - SubmitHashAsync: generates a fake tx hash and returns immediately.
/// - VerifyHashAsync: always returns true (simulates hash found on-chain).
/// - GetTxStatusAsync: simulates confirmed after first call.
///
/// To integrate a real blockchain:
/// 1. Inject RPC client (e.g. Nethereum for Ethereum).
/// 2. Call the smart-contract's anchor/verify functions.
/// 3. Handle nonce management, gas estimation, retries.
/// </summary>
public class StubBlockchainService : IBlockchainService
{
    private readonly ILogger<StubBlockchainService> _logger;

    // In-memory ledger for stub simulation
    private static readonly Dictionary<string, (string TxHash, string FileHash, DateTime Timestamp)> _ledger = new();
    private static readonly object _lock = new();

    public StubBlockchainService(ILogger<StubBlockchainService> logger)
    {
        _logger = logger;
    }

    public Task<string> SubmitHashAsync(string fileHash, BlockchainSubmitMeta metadata, CancellationToken ct = default)
    {
        // Generate a deterministic-looking fake tx hash
        var txHash = "0x" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")[..32];

        lock (_lock)
        {
            _ledger[fileHash] = (txHash, fileHash, DateTime.UtcNow);
        }

        _logger.LogInformation(
            "[STUB] Blockchain submit: DocID={DocumentID}, Hash={FileHash}, TxHash={TxHash}",
            metadata.DocumentID, fileHash, txHash);

        return Task.FromResult(txHash);
    }

    public Task<bool> VerifyHashAsync(string fileHash, CancellationToken ct = default)
    {
        bool exists;
        lock (_lock)
        {
            exists = _ledger.ContainsKey(fileHash);
        }

        _logger.LogInformation("[STUB] Blockchain verify: Hash={FileHash}, Found={Found}", fileHash, exists);
        return Task.FromResult(exists);
    }

    public Task<BlockchainTxStatusResult> GetTxStatusAsync(string txHash, CancellationToken ct = default)
    {
        // Stub: simulate confirmed status for any known tx
        bool found;
        lock (_lock)
        {
            found = _ledger.Values.Any(v => v.TxHash == txHash);
        }

        var result = new BlockchainTxStatusResult
        {
            Status = found ? "Confirmed" : "Failed",
            BlockNumber = found ? new Random().Next(1_000_000, 99_999_999).ToString() : null,
            ConfirmedAt = found ? DateTime.UtcNow : null
        };

        _logger.LogInformation(
            "[STUB] Blockchain tx-status: TxHash={TxHash}, Status={Status}, Block={Block}",
            txHash, result.Status, result.BlockNumber);

        return Task.FromResult(result);
    }
}
