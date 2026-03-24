using AISEP.Application.Configuration;
using AISEP.Domain.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

using System.Numerics;
using System.Text.Json;

namespace AISEP.Infrastructure.Services;

/// <summary>
/// Real Ethereum blockchain service targeting the Sepolia testnet.
/// Interacts with the deployed DocumentRegistry smart contract.
/// </summary>
public class EthereumBlockchainService : IBlockchainService
{
    private readonly Web3 _web3;
    private readonly string _contractAddress;
    private readonly BlockchainSettings _settings;
    private readonly ILogger<EthereumBlockchainService> _logger;

    // ABI for DocumentRegistry contract — only the functions we call from backend
    private const string ContractAbi = """
    [
        {
            "inputs":[
                {"name":"_fileHash","type":"bytes32"},
                {"name":"_metadata","type":"string"}
            ],
            "name":"registerDocument",
            "outputs":[],
            "stateMutability":"nonpayable",
            "type":"function"
        },
        {
            "inputs":[{"name":"_fileHash","type":"bytes32"}],
            "name":"existsDocument",
            "outputs":[{"name":"","type":"bool"}],
            "stateMutability":"view",
            "type":"function"
        },
        {
            "anonymous":false,
            "inputs":[
                {"indexed":true,"name":"fileHash","type":"bytes32"},
                {"indexed":true,"name":"submitter","type":"address"},
                {"indexed":false,"name":"timestamp","type":"uint256"},
                {"indexed":false,"name":"metadata","type":"string"}
            ],
            "name":"DocumentRegistered",
            "type":"event"
        }
    ]
    """;

    public EthereumBlockchainService(
        IOptions<BlockchainSettings> settings,
        ILogger<EthereumBlockchainService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _contractAddress = _settings.ContractAddress;

        if (string.IsNullOrWhiteSpace(_settings.RpcUrl))
            throw new InvalidOperationException("Blockchain:RpcUrl is not configured.");
        if (string.IsNullOrWhiteSpace(_settings.ContractAddress))
            throw new InvalidOperationException("Blockchain:ContractAddress is not configured.");
        if (string.IsNullOrWhiteSpace(_settings.PrivateKey))
            throw new InvalidOperationException("Blockchain:PrivateKey is not configured.");

        var account = new Account(_settings.PrivateKey, _settings.ChainId);
        _web3 = new Web3(account, _settings.RpcUrl);
    }

    public async Task<string> SubmitHashAsync(
        string fileHash, BlockchainSubmitMeta metadata, CancellationToken ct = default)
    {
        var hashBytes = ConvertToBytes32(fileHash);
        var contract = _web3.Eth.GetContract(ContractAbi, _contractAddress);
        var registerFunction = contract.GetFunction("registerDocument");

        // Build metadata JSON for on-chain storage
        var metadataJson = JsonSerializer.Serialize(new
        {
            documentId = metadata.DocumentID,
            startupId = metadata.StartupID,
            documentType = metadata.DocumentType,
            fileName = metadata.FileName
        });

        _logger.LogInformation(
            "Submitting hash to Sepolia: DocID={DocumentID}, Hash={FileHash}",
            metadata.DocumentID, fileHash);

        try
        {
            // Estimate gas with buffer
            var gasEstimate = await registerFunction.EstimateGasAsync(
                _web3.TransactionManager.Account.Address,
                null, null,
                hashBytes, metadataJson);
            var gasWithBuffer = new BigInteger((double)gasEstimate.Value * _settings.GasEstimateMultiplier);

            // Send transaction
            var txHash = await registerFunction.SendTransactionAsync(
                _web3.TransactionManager.Account.Address,
                new Nethereum.Hex.HexTypes.HexBigInteger(gasWithBuffer),
                null, // value (no ETH sent)
                hashBytes, metadataJson);

            _logger.LogInformation(
                "Transaction submitted: TxHash={TxHash}, DocID={DocumentID}",
                txHash, metadata.DocumentID);

            return txHash;
        }
        catch (Nethereum.JsonRpc.Client.RpcResponseException ex)
            when (ex.Message.Contains("HashAlreadyRegistered") || ex.Message.Contains("Already"))
        {
            _logger.LogWarning(
                "Hash already registered on-chain: DocID={DocumentID}, Hash={FileHash}",
                metadata.DocumentID, fileHash);

            throw new InvalidOperationException("Hash is already registered on-chain.", ex);
        }
    }

    public async Task<bool> VerifyHashAsync(string fileHash, CancellationToken ct = default)
    {
        var hashBytes = ConvertToBytes32(fileHash);
        var contract = _web3.Eth.GetContract(ContractAbi, _contractAddress);
        var existsFunction = contract.GetFunction("existsDocument");

        var result = await existsFunction.CallAsync<bool>(hashBytes);

        _logger.LogInformation(
            "Verify hash on Sepolia: Hash={FileHash}, Exists={Result}",
            fileHash, result);

        return result;
    }

    public async Task<BlockchainTxStatusResult> GetTxStatusAsync(
        string txHash, CancellationToken ct = default)
    {
        var receipt = await PollForReceiptAsync(txHash, ct);

        if (receipt == null)
        {
            return new BlockchainTxStatusResult
            {
                Status = "Pending",
                BlockNumber = null,
                ConfirmedAt = null
            };
        }

        var succeeded = receipt.Status?.Value == 1;

        return new BlockchainTxStatusResult
        {
            Status = succeeded ? "Confirmed" : "Failed",
            BlockNumber = receipt.BlockNumber?.Value.ToString(),
            ConfirmedAt = succeeded ? DateTime.UtcNow : null
        };
    }

    // ================================================================
    // Private helpers
    // ================================================================

    /// <summary>
    /// Convert a hex string (e.g. "ab12cd...") to a 32-byte array for bytes32 parameter.
    /// </summary>
    private static byte[] ConvertToBytes32(string hexHash)
    {
        var clean = hexHash.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? hexHash[2..]
            : hexHash;

        var bytes = ("0x" + clean).HexToByteArray();

        if (bytes.Length != 32)
            throw new ArgumentException(
                $"File hash must be exactly 32 bytes (SHA-256). Got {bytes.Length} bytes.", nameof(hexHash));

        return bytes;
    }

    /// <summary>
    /// Poll for transaction receipt with timeout.
    /// Returns null if still pending after timeout.
    /// </summary>
    private async Task<TransactionReceipt?> PollForReceiptAsync(string txHash, CancellationToken ct)
    {
        var timeout = TimeSpan.FromMilliseconds(_settings.ConfirmationTimeoutMs);
        var interval = TimeSpan.FromMilliseconds(_settings.ConfirmationPollingIntervalMs);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt
                .SendRequestAsync(txHash);

            if (receipt != null)
            {
                _logger.LogInformation(
                    "Tx receipt received: TxHash={TxHash}, Block={Block}, Status={Status}",
                    txHash, receipt.BlockNumber?.Value, receipt.Status?.Value);
                return receipt;
            }

            await Task.Delay(interval, ct);
        }

        _logger.LogWarning("Tx receipt poll timed out: TxHash={TxHash}", txHash);
        return null;
    }
}
