using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.SellerCategories.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.SellerCategories
{
    public interface ISellerCategoryService
    {
        Task<Result<List<SellerCategoryDto>>> GetAllAsync(bool activeOnly = false);
        Task<Result<SellerCategoryDto>> GetByIdAsync(Guid id);

        Task<Result<SellerCategoryDto>> CreateAsync(CreateSellerCategoryRequestDto request);
        Task<Result<SellerCategoryDto>> UpdateAsync(Guid id, UpdateSellerCategoryRequestDto request);
        Task<Result> DeleteAsync(Guid id);

        Task<Result<SellerSubcategoryDto>> CreateSubcategoryAsync(CreateSellerSubcategoryRequestDto request);
        Task<Result<SellerSubcategoryDto>> UpdateSubcategoryAsync(Guid id, UpdateSellerSubcategoryRequestDto request);
        Task<Result> DeleteSubcategoryAsync(Guid id);
    }
}