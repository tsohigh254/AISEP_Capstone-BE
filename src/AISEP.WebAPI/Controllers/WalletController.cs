using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Notification;
using AISEP.Application.DTOs.Wallet;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

[ApiController]
[Route("api/wallets")]
[Tags("Wallets")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private string GetCurrentUserType()
        => User.FindFirst("userType")?.Value ?? string.Empty;

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyWallet()
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();

        ApiResponse<WalletDto> result;
        if (userType == "Advisor")
            result = await _walletService.GetWalletByAdvisorAsync(userId);
        else if (userType == "Startup")
            result = await _walletService.GetWalletByStartupAsync(userId);
        else
            return Unauthorized();

        return result.ToActionResult();
    }

    [HttpGet("{walletId:int}/transactions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<TransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(int walletId, [FromQuery] WalletTransactionQueryParams queryParams)
    {
        var result = await _walletService.GetTransactionsAsync(walletId, GetCurrentUserType(), queryParams);
        return result.ToActionResult();
    }

    [HttpPut("bank-info")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBankInfo([FromBody] UpdateBankInfoDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _walletService.UpdateBankInfoAsync(userId, GetCurrentUserType(), request);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWallet([FromBody] CreateWalletDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _walletService.CreateWalletAsync(userId, GetCurrentUserType(), request);
        return result.ToActionResult();
    }
}
