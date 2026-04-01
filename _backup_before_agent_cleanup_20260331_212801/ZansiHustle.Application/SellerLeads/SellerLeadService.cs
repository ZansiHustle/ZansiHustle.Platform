using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Persistence.Agents;
using ZansiHustle.Application.Persistence.SellerLeads;
using ZansiHustle.Application.SellerLeads.Dtos;
using ZansiHustle.Domain.SellerLeads;
using ZansiHustle.Shared.Enums.SellerLeads;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.SellerLeads
{
    /// <summary>
    /// Provides business logic for seller lead operations.
    /// </summary>
    public class SellerLeadService : ISellerLeadService
    {
        private readonly ISellerLeadRepository _sellerLeadRepository;
        private readonly IAgentRepository _agentRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="SellerLeadService"/> class.
        /// </summary>
        public SellerLeadService(ISellerLeadRepository sellerLeadRepository, IAgentRepository agentRepository)
        {
            _sellerLeadRepository = sellerLeadRepository;
            _agentRepository = agentRepository;
        }

        /// <inheritdoc />
        public async Task<Result<List<SellerLeadListItemDto>>> GetAllAsync()
        {
            try
            {
                var sellerLeads = await _sellerLeadRepository.GetAllAsync();

                var data = sellerLeads.Select(MapToListItemDto).ToList();

                return Result<List<SellerLeadListItemDto>>.Success(data, "Seller leads retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<SellerLeadListItemDto>>.Failure($"An error occurred while retrieving seller leads. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<SellerLeadDetailsDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var sellerLead = await _sellerLeadRepository.GetByIdAsync(id);

                if (sellerLead is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Seller lead not found.");
                }

                return Result<SellerLeadDetailsDto>.Success(MapToDetailsDto(sellerLead), "Seller lead retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<SellerLeadDetailsDto>.Failure($"An error occurred while retrieving the seller lead. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<SellerLeadDetailsDto>> CreateAsync(CreateSellerLeadRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.ContactName))
                {
                    return Result<SellerLeadDetailsDto>.Failure("Contact name is required.");
                }

                if (string.IsNullOrWhiteSpace(request.BusinessName))
                {
                    return Result<SellerLeadDetailsDto>.Failure("Business name is required.");
                }

                if (request.AgentId.HasValue)
                {
                    var agent = await _agentRepository.GetByIdAsync(request.AgentId.Value);

                    if (agent is null)
                    {
                        return Result<SellerLeadDetailsDto>.Failure("Selected agent was not found.");
                    }
                }

                var entity = new SellerLead
                {
                    Id = Guid.NewGuid(),
                    Code = $"SLD-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    ContactName = request.ContactName.Trim(),
                    BusinessName = request.BusinessName.Trim(),
                    LeadType = request.LeadType,
                    Category = request.Category?.Trim(),
                    Subcategory = request.Subcategory?.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    Email = request.Email?.Trim(),
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    SocialHandleOrLink = request.SocialHandleOrLink?.Trim(),
                    SourceType = request.SourceType?.Trim(),
                    AgentId = request.AgentId,
                    Notes = request.Notes?.Trim(),
                    SubmittedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow,
                    VerificationStatus = VerificationStatus.Pending,
                    ApprovalStatus = ApprovalStatus.Pending
                };

                await _sellerLeadRepository.AddAsync(entity);

                var saved = await _sellerLeadRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Failed to create seller lead.");
                }

                var createdEntity = await _sellerLeadRepository.GetByIdAsync(entity.Id) ?? entity;

                return Result<SellerLeadDetailsDto>.Success(MapToDetailsDto(createdEntity), "Seller lead created successfully.");
            }
            catch (Exception ex)
            {
                return Result<SellerLeadDetailsDto>.Failure($"An error occurred while creating the seller lead. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<SellerLeadDetailsDto>> UpdateAsync(Guid id, UpdateSellerLeadRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.ContactName))
                {
                    return Result<SellerLeadDetailsDto>.Failure("Contact name is required.");
                }

                if (string.IsNullOrWhiteSpace(request.BusinessName))
                {
                    return Result<SellerLeadDetailsDto>.Failure("Business name is required.");
                }

                if (request.AgentId.HasValue)
                {
                    var agent = await _agentRepository.GetByIdAsync(request.AgentId.Value);

                    if (agent is null)
                    {
                        return Result<SellerLeadDetailsDto>.Failure("Selected agent was not found.");
                    }
                }

                var sellerLead = await _sellerLeadRepository.GetByIdAsync(id);

                if (sellerLead is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Seller lead not found.");
                }

                sellerLead.ContactName = request.ContactName.Trim();
                sellerLead.BusinessName = request.BusinessName.Trim();
                sellerLead.LeadType = request.LeadType;
                sellerLead.Category = request.Category?.Trim();
                sellerLead.Subcategory = request.Subcategory?.Trim();
                sellerLead.PhoneNumber = request.PhoneNumber?.Trim();
                sellerLead.Email = request.Email?.Trim();
                sellerLead.Province = request.Province?.Trim();
                sellerLead.City = request.City?.Trim();
                sellerLead.SocialHandleOrLink = request.SocialHandleOrLink?.Trim();
                sellerLead.SourceType = request.SourceType?.Trim();
                sellerLead.AgentId = request.AgentId;
                sellerLead.Notes = request.Notes?.Trim();
                sellerLead.UpdatedAtUtc = DateTime.UtcNow;

                _sellerLeadRepository.Update(sellerLead);

                var saved = await _sellerLeadRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Failed to update seller lead.");
                }

                var updatedEntity = await _sellerLeadRepository.GetByIdAsync(sellerLead.Id) ?? sellerLead;

                return Result<SellerLeadDetailsDto>.Success(MapToDetailsDto(updatedEntity), "Seller lead updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<SellerLeadDetailsDto>.Failure($"An error occurred while updating the seller lead. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<SellerLeadDetailsDto>> ReviewAsync(Guid id, ReviewSellerLeadRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Request is required.");
                }

                var sellerLead = await _sellerLeadRepository.GetByIdAsync(id);

                if (sellerLead is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Seller lead not found.");
                }

                sellerLead.ApprovalStatus = request.ApprovalStatus;
                sellerLead.ReviewedByUserId = request.ReviewedByUserId;
                sellerLead.ReviewedAtUtc = DateTime.UtcNow;
                sellerLead.UpdatedAtUtc = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    sellerLead.Notes = request.Notes.Trim();
                }

                _sellerLeadRepository.Update(sellerLead);

                var saved = await _sellerLeadRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Failed to review seller lead.");
                }

                var updatedEntity = await _sellerLeadRepository.GetByIdAsync(sellerLead.Id) ?? sellerLead;

                return Result<SellerLeadDetailsDto>.Success(MapToDetailsDto(updatedEntity), "Seller lead reviewed successfully.");
            }
            catch (Exception ex)
            {
                return Result<SellerLeadDetailsDto>.Failure($"An error occurred while reviewing the seller lead. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<SellerLeadDetailsDto>> VerifyAsync(Guid id, VerifySellerLeadRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Request is required.");
                }

                var sellerLead = await _sellerLeadRepository.GetByIdAsync(id);

                if (sellerLead is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Seller lead not found.");
                }

                sellerLead.VerificationStatus = request.VerificationStatus;
                sellerLead.UpdatedAtUtc = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    sellerLead.Notes = request.Notes.Trim();
                }

                _sellerLeadRepository.Update(sellerLead);

                var saved = await _sellerLeadRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Failed to verify seller lead.");
                }

                var updatedEntity = await _sellerLeadRepository.GetByIdAsync(sellerLead.Id) ?? sellerLead;

                return Result<SellerLeadDetailsDto>.Success(MapToDetailsDto(updatedEntity), "Seller lead verification updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<SellerLeadDetailsDto>.Failure($"An error occurred while verifying the seller lead. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<SellerLeadDetailsDto>> ConvertAsync(Guid id, ConvertSellerLeadRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Request is required.");
                }

                if (request.ConvertedSellerId == Guid.Empty)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Converted seller id is required.");
                }

                var sellerLead = await _sellerLeadRepository.GetByIdAsync(id);

                if (sellerLead is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Seller lead not found.");
                }

                sellerLead.ConvertedSellerId = request.ConvertedSellerId;
                sellerLead.ApprovalStatus = ApprovalStatus.Approved;
                sellerLead.UpdatedAtUtc = DateTime.UtcNow;

                _sellerLeadRepository.Update(sellerLead);

                var saved = await _sellerLeadRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Failed to convert seller lead.");
                }

                var updatedEntity = await _sellerLeadRepository.GetByIdAsync(sellerLead.Id) ?? sellerLead;

                return Result<SellerLeadDetailsDto>.Success(MapToDetailsDto(updatedEntity), "Seller lead converted successfully.");
            }
            catch (Exception ex)
            {
                return Result<SellerLeadDetailsDto>.Failure($"An error occurred while converting the seller lead. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var sellerLead = await _sellerLeadRepository.GetByIdAsync(id);

                if (sellerLead is null)
                {
                    return Result.Failure("Seller lead not found.");
                }

                _sellerLeadRepository.Delete(sellerLead);

                var saved = await _sellerLeadRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result.Failure("Failed to delete seller lead.");
                }

                return Result.Success("Seller lead deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"An error occurred while deleting the seller lead. {ex.Message}");
            }
        }

        private static SellerLeadListItemDto MapToListItemDto(SellerLead sellerLead)
        {
            return new SellerLeadListItemDto
            {
                Id = sellerLead.Id,
                Code = sellerLead.Code,
                ContactName = sellerLead.ContactName,
                BusinessName = sellerLead.BusinessName,
                LeadType = sellerLead.LeadType,
                Category = sellerLead.Category,
                Province = sellerLead.Province,
                City = sellerLead.City,
                SourceType = sellerLead.SourceType,
                AgentId = sellerLead.AgentId,
                AgentName = sellerLead.Agent?.FullName,
                VerificationStatus = sellerLead.VerificationStatus,
                ApprovalStatus = sellerLead.ApprovalStatus,
                SubmittedAtUtc = sellerLead.SubmittedAtUtc
            };
        }

        private static SellerLeadDetailsDto MapToDetailsDto(SellerLead sellerLead)
        {
            return new SellerLeadDetailsDto
            {
                Id = sellerLead.Id,
                Code = sellerLead.Code,
                ContactName = sellerLead.ContactName,
                BusinessName = sellerLead.BusinessName,
                LeadType = sellerLead.LeadType,
                Category = sellerLead.Category,
                Subcategory = sellerLead.Subcategory,
                PhoneNumber = sellerLead.PhoneNumber,
                Email = sellerLead.Email,
                Province = sellerLead.Province,
                City = sellerLead.City,
                SocialHandleOrLink = sellerLead.SocialHandleOrLink,
                SourceType = sellerLead.SourceType,
                AgentId = sellerLead.AgentId,
                AgentName = sellerLead.Agent?.FullName,
                VerificationStatus = sellerLead.VerificationStatus,
                ApprovalStatus = sellerLead.ApprovalStatus,
                Notes = sellerLead.Notes,
                ReviewedByUserId = sellerLead.ReviewedByUserId,
                ReviewedAtUtc = sellerLead.ReviewedAtUtc,
                ConvertedSellerId = sellerLead.ConvertedSellerId,
                SubmittedAtUtc = sellerLead.SubmittedAtUtc,
                CreatedAtUtc = sellerLead.CreatedAtUtc,
                UpdatedAtUtc = sellerLead.UpdatedAtUtc
            };
        }
    }
}
