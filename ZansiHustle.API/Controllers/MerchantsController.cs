using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Merchants;
using ZansiHustle.Application.Merchants.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes merchant management endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MerchantsController : ControllerBase
    {
        private readonly IMerchantService _merchantService;

        /// <summary>
        /// Creates a new instance of the <see cref="MerchantsController"/> class.
        /// </summary>
        public MerchantsController(IMerchantService merchantService)
        {
            _merchantService = merchantService;
        }

        /// <summary>
        /// Gets all merchants.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<List<MerchantDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _merchantService.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets a merchant by identifier.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _merchantService.GetByIdAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Creates a merchant.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateMerchantRequestDto request)
        {
            var result = await _merchantService.CreateAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Updates a merchant.
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMerchantRequestDto request)
        {
            var result = await _merchantService.UpdateAsync(id, request);
            return Ok(result);
        }

        /// <summary>
        /// Deletes a merchant.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _merchantService.DeleteAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Verifies merchant KYC.
        /// </summary>
        [HttpPost("{id:guid}/verify-kyc")]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyKyc(Guid id)
        {
            var result = await _merchantService.VerifyKycAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Updates merchant payout eligibility.
        /// </summary>
        [HttpPatch("{id:guid}/payout-eligible")]
        [ProducesResponseType(typeof(Result<MerchantDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePayoutEligibility(Guid id, [FromBody] UpdateMerchantPayoutEligibilityRequestDto request)
        {
            var result = await _merchantService.UpdatePayoutEligibilityAsync(id, request.Eligible);
            return Ok(result);
        }
    }
}
