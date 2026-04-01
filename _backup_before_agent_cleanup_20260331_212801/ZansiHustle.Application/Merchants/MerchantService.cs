using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Merchants.Dtos;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Merchants
{
    /// <summary>
    /// Provides business logic for merchant operations.
    /// </summary>
    public class MerchantService : IMerchantService
    {
        private readonly IMerchantRepository _merchantRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="MerchantService"/> class.
        /// </summary>
        public MerchantService(IMerchantRepository merchantRepository)
        {
            _merchantRepository = merchantRepository;
        }

        /// <inheritdoc />
        public async Task<Result<List<MerchantDto>>> GetAllAsync()
        {
            try
            {
                var merchants = await _merchantRepository.GetAllAsync();
                var data = merchants.Select(MapToDto).ToList();

                return Result<List<MerchantDto>>.Success(data, "Merchants retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<MerchantDto>>.Failure($"An error occurred while retrieving merchants. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                {
                    return Result<MerchantDto>.Failure("Merchant not found.");
                }

                return Result<MerchantDto>.Success(MapToDto(merchant), "Merchant retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure($"An error occurred while retrieving the merchant. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> CreateAsync(CreateMerchantRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<MerchantDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Result<MerchantDto>.Failure("Merchant name is required.");
                }

                var entity = new Merchant
                {
                    Id = Guid.NewGuid(),
                    Code = $"MER-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Type = request.Type,
                    OwnerUserId = request.OwnerUserId,
                    ContactEmail = request.ContactEmail?.Trim(),
                    ContactPhoneNumber = request.ContactPhoneNumber?.Trim(),
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    AddressLine1 = request.AddressLine1?.Trim(),
                    WebsiteUrl = request.WebsiteUrl?.Trim(),
                    LogoUrl = request.LogoUrl?.Trim(),
                    BannerUrl = request.BannerUrl?.Trim(),
                    Rating = request.Rating,
                    Status = MerchantStatus.Pending,
                    KycStatus = MerchantKycStatus.Pending,
                    IsPayoutEligible = false,
                    FollowersCount = 0,
                    TotalOrders = 0,
                    TotalRevenue = 0m,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _merchantRepository.AddAsync(entity);

                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<MerchantDto>.Failure("Failed to create merchant.");
                }

                return Result<MerchantDto>.Success(MapToDto(entity), "Merchant created successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure($"An error occurred while creating the merchant. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> UpdateAsync(Guid id, UpdateMerchantRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<MerchantDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Result<MerchantDto>.Failure("Merchant name is required.");
                }

                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                {
                    return Result<MerchantDto>.Failure("Merchant not found.");
                }

                merchant.Name = request.Name.Trim();
                merchant.Description = request.Description?.Trim();
                merchant.Type = request.Type;
                merchant.Status = request.Status;
                merchant.ContactEmail = request.ContactEmail?.Trim();
                merchant.ContactPhoneNumber = request.ContactPhoneNumber?.Trim();
                merchant.Province = request.Province?.Trim();
                merchant.City = request.City?.Trim();
                merchant.AddressLine1 = request.AddressLine1?.Trim();
                merchant.WebsiteUrl = request.WebsiteUrl?.Trim();
                merchant.LogoUrl = request.LogoUrl?.Trim();
                merchant.BannerUrl = request.BannerUrl?.Trim();
                merchant.Rating = request.Rating;
                merchant.UpdatedAtUtc = DateTime.UtcNow;

                _merchantRepository.Update(merchant);

                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<MerchantDto>.Failure("Failed to update merchant.");
                }

                return Result<MerchantDto>.Success(MapToDto(merchant), "Merchant updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure($"An error occurred while updating the merchant. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> VerifyKycAsync(Guid id)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                {
                    return Result<MerchantDto>.Failure("Merchant not found.");
                }

                merchant.KycStatus = MerchantKycStatus.Verified;
                merchant.Status = MerchantStatus.Active;
                merchant.UpdatedAtUtc = DateTime.UtcNow;

                _merchantRepository.Update(merchant);

                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<MerchantDto>.Failure("Failed to verify merchant KYC.");
                }

                return Result<MerchantDto>.Success(MapToDto(merchant), "Merchant KYC verified successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure($"An error occurred while verifying merchant KYC. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<MerchantDto>> UpdatePayoutEligibilityAsync(Guid id, bool eligible)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                {
                    return Result<MerchantDto>.Failure("Merchant not found.");
                }

                merchant.IsPayoutEligible = eligible;
                merchant.UpdatedAtUtc = DateTime.UtcNow;

                _merchantRepository.Update(merchant);

                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<MerchantDto>.Failure("Failed to update merchant payout eligibility.");
                }

                return Result<MerchantDto>.Success(MapToDto(merchant), "Merchant payout eligibility updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<MerchantDto>.Failure($"An error occurred while updating merchant payout eligibility. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(id);

                if (merchant is null)
                {
                    return Result.Failure("Merchant not found.");
                }

                _merchantRepository.Delete(merchant);

                var saved = await _merchantRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result.Failure("Failed to delete merchant.");
                }

                return Result.Success("Merchant deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"An error occurred while deleting the merchant. {ex.Message}");
            }
        }

        private static MerchantDto MapToDto(Merchant merchant)
        {
            return new MerchantDto
            {
                Id = merchant.Id,
                Code = merchant.Code,
                Name = merchant.Name,
                Description = merchant.Description,
                Type = merchant.Type,
                Status = merchant.Status,
                KycStatus = merchant.KycStatus,
                IsPayoutEligible = merchant.IsPayoutEligible,
                OwnerUserId = merchant.OwnerUserId,
                ContactEmail = merchant.ContactEmail,
                ContactPhoneNumber = merchant.ContactPhoneNumber,
                Province = merchant.Province,
                City = merchant.City,
                AddressLine1 = merchant.AddressLine1,
                WebsiteUrl = merchant.WebsiteUrl,
                LogoUrl = merchant.LogoUrl,
                BannerUrl = merchant.BannerUrl,
                FollowersCount = merchant.FollowersCount,
                Rating = merchant.Rating,
                TotalOrders = merchant.TotalOrders,
                TotalRevenue = merchant.TotalRevenue,
                CreatedAtUtc = merchant.CreatedAtUtc,
                UpdatedAtUtc = merchant.UpdatedAtUtc
            };
        }
    }
}
