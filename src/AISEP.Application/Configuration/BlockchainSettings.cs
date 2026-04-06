namespace AISEP.Application.Configuration;

public class BlockchainSettings
{
    /// <summary>"Stub" for mock, "Ethereum" for real Sepolia testnet.</summary>
    public string Provider { get; set; } = "Stub";

    /// <summary>Network display name stored in DocumentBlockchainProof.</summary>
    public string NetworkName { get; set; } = "Sepolia";

    /// <summary>JSON-RPC endpoint (e.g. Infura/Alchemy Sepolia URL).</summary>
    public string RpcUrl { get; set; } = "";

    /// <summary>Deployed DocumentAnchor contract address.</summary>
    public string ContractAddress { get; set; } = "";

    /// <summary>Wallet private key — set via .env only, never in appsettings.</summary>
    public string PrivateKey { get; set; } = "";

    /// <summary>Ethereum chain ID (Sepolia = 11155111).</summary>
    public int ChainId { get; set; } = 11155111;

    /// <summary>Interval in ms between tx receipt polls.</summary>
    public int ConfirmationPollingIntervalMs { get; set; } = 3000;

    /// <summary>Max wait time in ms for tx confirmation.</summary>
    public int ConfirmationTimeoutMs { get; set; } = 120_000;

    /// <summary>Multiplier applied to estimated gas (e.g. 1.2 = 20% buffer).</summary>
    public double GasEstimateMultiplier { get; set; } = 1.2;

    /// <summary>Etherscan base URL for building transaction links.</summary>
    public string EtherscanBaseUrl { get; set; } = "https://sepolia.etherscan.io";
}
