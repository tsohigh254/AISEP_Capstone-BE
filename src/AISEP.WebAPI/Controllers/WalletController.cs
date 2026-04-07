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

    [HttpGet("me")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyWallet()
    {
        var userId = GetCurrentUserId();
        var result = await _walletService.GetWalletByAdvisorAsync(userId);
        return result.ToActionResult();
    }

    [HttpGet("{walletId:int}/transactions")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<TransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(int walletId, [FromQuery] WalletTransactionQueryParams queryParams)
    {
        var result = await _walletService.GetTransactionsAsync(walletId, queryParams);
        return result.ToPagedEnvelope();
    }
}
