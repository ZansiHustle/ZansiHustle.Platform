using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Wallets;
using ZansiHustle.Application.Wallets.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Customer wallet — balance + ledger. Read-only in this pass (credits come
    /// from the rejection/refund flows; no wallet-as-payment-method or
    /// withdrawals yet).
    /// </summary>
    [Route("api/wallet")]
    [Authorize]
    public class WalletController : BaseController
    {
        private readonly IWalletService _walletService;
        private readonly ICurrentUserService _currentUserService;

        public WalletController(IWalletService walletService, ICurrentUserService currentUserService)
        {
            _walletService = walletService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<WalletDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWallet()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<WalletDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _walletService.GetWalletAsync(userId.Value));
        }

        [HttpGet("transactions")]
        [ProducesResponseType(typeof(Result<List<WalletTransactionDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactions([FromQuery] int take = 50)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<List<WalletTransactionDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _walletService.GetTransactionsAsync(userId.Value, take));
        }

        /// <summary>Create a manual withdrawal request — holds the amount against the
        /// wallet balance. No automated payout; reviewed manually.</summary>
        [HttpPost("withdrawals")]
        [ProducesResponseType(typeof(Result<WithdrawalRequestDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RequestWithdrawal([FromBody] CreateWithdrawalRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<WithdrawalRequestDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _walletService.RequestWithdrawalAsync(userId.Value, request ?? new CreateWithdrawalRequestDto()));
        }

        /// <summary>The current user's withdrawal requests (account number masked).</summary>
        [HttpGet("withdrawals")]
        [ProducesResponseType(typeof(Result<List<WithdrawalRequestDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWithdrawals([FromQuery] int take = 50)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<List<WithdrawalRequestDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _walletService.GetWithdrawalsAsync(userId.Value, take));
        }
    }
}
