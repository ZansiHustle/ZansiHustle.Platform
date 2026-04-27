using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Agents.AgentMappings;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Communication.Email.Interfaces;
using ZansiHustle.Application.Persistence.SellerLeads;
using ZansiHustle.Application.Persistence.Users;
using ZansiHustle.Application.SellerLeads.Dtos;
using ZansiHustle.Application.Users;
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
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserService _userService;
        private readonly IMerchantEmailService _merchantEmailService;
        private readonly IAgentMappingService _agentMappingService;
        /// <summary>
        /// Creates a new instance of the <see cref="SellerLeadService"/> class.
        /// </summary>
        public SellerLeadService(ISellerLeadRepository sellerLeadRepository, IUserRepository agentRepository, ICurrentUserService currentUserService, IUserService userService,
                                 IMerchantEmailService merchantEmailService, IAgentMappingService agentMappingService)
        {
            _sellerLeadRepository = sellerLeadRepository;
            _userRepository = agentRepository;
            _currentUserService = currentUserService;
            _userService = userService;
            _merchantEmailService = merchantEmailService;
            _agentMappingService = agentMappingService;
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
        public async Task<Result<List<SellerLeadDetailsDto>>> GetMineAsync()
        {
            try
            {
                if (!_currentUserService.UserId.HasValue)
                {
                    return Result<List<SellerLeadDetailsDto>>.Failure("Authenticated user was not found.");
                }

                var leads = await _sellerLeadRepository.GetByUserIdAsync(_currentUserService.UserId.Value);
                var data = leads.Select(MapToDetailsDto).ToList();

                return Result<List<SellerLeadDetailsDto>>.Success(data, "Seller leads retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<SellerLeadDetailsDto>>.Failure($"An error occurred while retrieving your seller leads. {ex.Message}");
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

                // BusinessName is optional under the seller-first onboarding model:
                // the user becomes a seller account first, then later applies for a
                // shop/storefront. When omitted, fall back to ContactName so the
                // entity stays consistent with the public-create path above.
                if (!_currentUserService.UserId.HasValue)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Authenticated user was not found.");
                }

                var entity = new SellerLead
                {
                    Id = Guid.NewGuid(),
                    Code = $"SLD-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    ContactName = request.ContactName.Trim(),
                    BusinessName = string.IsNullOrWhiteSpace(request.BusinessName)
                        ? request.ContactName.Trim()
                        : request.BusinessName.Trim(),
                    LeadType = request.LeadType,
                    Category = request.Category?.Trim(),
                    Subcategory = request.Subcategory?.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    Email = request.Email?.Trim(),
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    SocialHandleOrLink = request.SocialHandleOrLink?.Trim(),
                    SourceType = request.SourceType?.Trim(),
                    AssignedUserId = _currentUserService.UserId.Value,
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
        public async Task<Result<SellerLeadDetailsDto>> CreatePublicAsync(CreatePublicSellerLeadRequestDto request)
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

                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    return Result<SellerLeadDetailsDto>.Failure("Phone number is required.");
                }

                // Look up referrer user if provided
                Guid? assignedUserId = null;
                string referrerNotes = string.Empty;

                // Check if referrerId is provided (affiliate code)
                if (!string.IsNullOrWhiteSpace(request.ReferrerId))
                {
                    // Try to parse as GUID directly first
                    if (Guid.TryParse(request.ReferrerId, out var directUserId))
                    {
                        assignedUserId = directUserId;
                        referrerNotes = $"Referred by User ID: {request.ReferrerId}";
                       // _logger.LogInformation("Referrer ID {ReferrerId} used directly as User ID", request.ReferrerId);
                    }
                    else
                    {
                        // Use the agent mapping
                        var agentMapping = await _agentMappingService.GetByAffiliateCodeAsync(request.ReferrerId);

                        if (agentMapping != null)
                        {
                            assignedUserId = agentMapping.UserId;
                            referrerNotes = $"Referred by Agent Code: {request.ReferrerId} (Agent: {agentMapping.UserFullName})";
                            //_logger.LogInformation("Referrer code {ReferrerId} mapped to user {UserId}",
                                //request.ReferrerId, agentMapping.UserId);
                        }
                        else
                        {
                            referrerNotes = $"Referred by Agent Code: {request.ReferrerId} (No mapping found)";
                            //_logger.LogWarning("No mapping found for referrer code: {ReferrerId}", request.ReferrerId);
                        }
                    }
                }
                // Fall back to name search if no referrerId provided
                else if (!string.IsNullOrWhiteSpace(request.ReferrerName))
                {
                    var searchResult = await _userService.SearchByNameAsync(request.ReferrerName);

                    if (searchResult.IsSuccess && searchResult.Data != null)
                    {
                        assignedUserId = searchResult.Data;
                        referrerNotes = $"Referred by: {request.ReferrerName} (Matched to user: {request.ReferrerName})";
                    }
                    else
                    {
                        referrerNotes = $"Referred by: {request.ReferrerName} (No matching user found)";
                    }
                }

                var entity = new SellerLead
                {
                    Id = Guid.NewGuid(),
                    Code = $"SLD-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    ContactName = request.ContactName.Trim(),
                    BusinessName = string.IsNullOrWhiteSpace(request.BusinessName) ? request.ContactName.Trim() : request.BusinessName.Trim(),
                    LeadType = request.LeadType,
                    Category = request.Category?.Trim(),
                    Subcategory = request.Subcategory?.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    Email = request.Email?.Trim(),
                    Province = request.Province?.Trim(),
                    City = request.City?.Trim(),
                    SocialHandleOrLink = request.SocialHandleOrLink?.Trim(),
                    SourceType = request.SourceType ?? "website_become_hustler",
                    AssignedUserId = assignedUserId,
                    Notes = BuildNotes(request.Notes, referrerNotes),
                    SubmittedAtUtc = DateTime.UtcNow,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = null,
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

                // Send emails asynchronously (don't await)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(entity.Email))
                        {
                            var firstName = entity.ContactName.Split(' ')[0];
                            await _merchantEmailService.SendLeadWelcomeEmailAsync(entity.Email, firstName, entity.BusinessName);
                        }

                        await _merchantEmailService.SendNewLeadNotificationAsync(
                            entity.ContactName,
                            entity.PhoneNumber ?? "Not provided",
                            entity.Email,
                            entity.Category,
                            entity.Province,
                            request.ReferrerName ?? request.ReferrerId);
                    }
                    catch (Exception ex)
                    {
                        //_logger.LogError(ex, "Failed to send emails for lead {LeadId}", entity.Id);
                    }
                });

                return Result<SellerLeadDetailsDto>.Success(MapToDetailsDto(createdEntity), "Seller lead created successfully.");
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return Result<SellerLeadDetailsDto>.Failure($"An error occurred while creating the seller lead. {innerMessage}");
            }
        }

        private string BuildNotes(string? userNotes, string? referrerNotes)
        {
            var notes = new List<string>();

            if (!string.IsNullOrWhiteSpace(userNotes))
                notes.Add(userNotes.Trim());

            if (!string.IsNullOrWhiteSpace(referrerNotes))
                notes.Add(referrerNotes);

            return notes.Any() ? string.Join(" | ", notes) : string.Empty;
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

                // BusinessName is optional — see CreateAsync. Falls back to ContactName.
                if (!_currentUserService.UserId.HasValue)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Authenticated user was not found.");
                }

                var sellerLead = await _sellerLeadRepository.GetByIdAsync(id);

                if (sellerLead is null)
                {
                    return Result<SellerLeadDetailsDto>.Failure("Seller lead not found.");
                }

                sellerLead.ContactName = request.ContactName.Trim();
                sellerLead.BusinessName = string.IsNullOrWhiteSpace(request.BusinessName)
                    ? request.ContactName.Trim()
                    : request.BusinessName.Trim();
                sellerLead.LeadType = request.LeadType;
                sellerLead.Category = request.Category?.Trim();
                sellerLead.Subcategory = request.Subcategory?.Trim();
                sellerLead.PhoneNumber = request.PhoneNumber?.Trim();
                sellerLead.Email = request.Email?.Trim();
                sellerLead.Province = request.Province?.Trim();
                sellerLead.City = request.City?.Trim();
                sellerLead.SocialHandleOrLink = request.SocialHandleOrLink?.Trim();
                sellerLead.SourceType = request.SourceType?.Trim();
                sellerLead.AssignedUserId = _currentUserService.UserId;
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

        // Resolves the display name for the user who submitted/owns the
        // lead. Prefers the joined `AssignedUser` (full name); falls back
        // to a "Website-Lead" marker when the lead came in via the
        // public become-a-hustler form with no agent referrer; otherwise
        // null so the UI can show its own placeholder.
        private static string? ResolveAssignedUserName(SellerLead sellerLead)
        {
            if (sellerLead.AssignedUser != null)
            {
                var first = sellerLead.AssignedUser.FirstName?.Trim();
                var last = sellerLead.AssignedUser.LastName?.Trim();
                var full = string.Join(" ", new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(full)) return full;
            }
            if (sellerLead.SourceType == "website_become_hustler") return "Website-Lead";
            return null;
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
                Subcategory = sellerLead.Subcategory,
                PhoneNumber = sellerLead.PhoneNumber,
                Email = sellerLead.Email,
                Province = sellerLead.Province,
                City = sellerLead.City,
                SocialHandleOrLink = sellerLead.SocialHandleOrLink,
                SourceType = sellerLead.SourceType,
                AssignedUserId = sellerLead.AssignedUserId,
                AssignedUserName = ResolveAssignedUserName(sellerLead),
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
                AssignedUserId = sellerLead.AssignedUserId,
                AssignedUserName = ResolveAssignedUserName(sellerLead),
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

