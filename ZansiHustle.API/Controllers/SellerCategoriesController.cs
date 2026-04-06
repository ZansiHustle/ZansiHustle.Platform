using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.SellerCategories;
using ZansiHustle.Application.SellerCategories.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Exposes endpoints for managing seller categories and subcategories.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SellerCategoriesController : BaseController
    {
        private readonly ISellerCategoryService _sellerCategoryService;

        public SellerCategoriesController(ISellerCategoryService sellerCategoryService)
        {
            _sellerCategoryService = sellerCategoryService;
        }

        /// <summary>
        /// Gets all seller categories with their subcategories.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<List<SellerCategoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
        {
            var result = await _sellerCategoryService.GetAllAsync(activeOnly);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets a seller category by its identifier.
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<SellerCategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _sellerCategoryService.GetByIdAsync(id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Creates a new seller category.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result<SellerCategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateSellerCategoryRequestDto request)
        {
            var result = await _sellerCategoryService.CreateAsync(request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Updates an existing seller category.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result<SellerCategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSellerCategoryRequestDto request)
        {
            var result = await _sellerCategoryService.UpdateAsync(id, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Deletes a seller category.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _sellerCategoryService.DeleteAsync(id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Creates a new subcategory under a seller category.
        /// </summary>
        [HttpPost("subcategories")]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result<SellerSubcategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSubcategory([FromBody] CreateSellerSubcategoryRequestDto request)
        {
            var result = await _sellerCategoryService.CreateSubcategoryAsync(request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Updates an existing subcategory.
        /// </summary>
        [HttpPut("subcategories/{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result<SellerSubcategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateSubcategory(Guid id, [FromBody] UpdateSellerSubcategoryRequestDto request)
        {
            var result = await _sellerCategoryService.UpdateSubcategoryAsync(id, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Deletes a subcategory.
        /// </summary>
        [HttpDelete("subcategories/{id:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,TeamManager")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteSubcategory(Guid id)
        {
            var result = await _sellerCategoryService.DeleteSubcategoryAsync(id);
            return ToActionResult(result);
        }
    }
}