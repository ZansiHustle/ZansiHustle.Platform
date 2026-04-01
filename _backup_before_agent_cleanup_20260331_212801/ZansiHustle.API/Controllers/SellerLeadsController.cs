using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.SellerLeads;
using ZansiHustle.Application.SellerLeads.Dtos;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for seller lead management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SellerLeadsController : ControllerBase
    {
        private readonly ISellerLeadService _sellerLeadService;

        /// <summary>
        /// Creates a new instance of the <see cref="SellerLeadsController"/> class.
        /// </summary>
        public SellerLeadsController(ISellerLeadService sellerLeadService)
        {
            _sellerLeadService = sellerLeadService;
        }

        /// <summary>
        /// Gets all seller leads.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _sellerLeadService.GetAllAsync();

            return Ok(result);
        }

        /// <summary>
        /// Gets a seller lead by identifier.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _sellerLeadService.GetByIdAsync(id);

            return Ok(result);
        }

        /// <summary>
        /// Creates a new seller lead.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.CreateAsync(request);

            return Ok(result);
        }

        /// <summary>
        /// Updates an existing seller lead.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.UpdateAsync(id, request);

            return Ok(result);
        }

        /// <summary>
        /// Reviews a seller lead.
        /// </summary>
        [HttpPut("{id:guid}/review")]
        public async Task<IActionResult> Review(Guid id, [FromBody] ReviewSellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.ReviewAsync(id, request);

            return Ok(result);
        }

        /// <summary>
        /// Updates seller lead verification status.
        /// </summary>
        [HttpPut("{id:guid}/verify")]
        public async Task<IActionResult> Verify(Guid id, [FromBody] VerifySellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.VerifyAsync(id, request);

            return Ok(result);
        }

        /// <summary>
        /// Converts a seller lead to a live seller reference.
        /// </summary>
        [HttpPut("{id:guid}/convert")]
        public async Task<IActionResult> Convert(Guid id, [FromBody] ConvertSellerLeadRequestDto request)
        {
            var result = await _sellerLeadService.ConvertAsync(id, request);

            return Ok(result);
        }

        /// <summary>
        /// Deletes a seller lead.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _sellerLeadService.DeleteAsync(id);

            return Ok(result);
        }
    }
}
