using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Events.Dtos;
using ZansiHustle.Application.Persistence.Events;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.SellerCategories;
using ZansiHustle.Domain.Events;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Shared.Enums.Events;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Events
{
    public class EventPlanService : IEventPlanService
    {
        private readonly IEventPlanRepository _repository;
        private readonly ISellerCategoryRepository _categoryRepository;
        private readonly IListingRepository _listingRepository;
        private readonly ILogger<EventPlanService> _logger;

        public EventPlanService(IEventPlanRepository repository, ISellerCategoryRepository categoryRepository, IListingRepository listingRepository, ILogger<EventPlanService> logger)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
            _listingRepository = listingRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Result<List<EventTypeTemplateItemDto>>> GetTemplateAsync(EventType eventType)
        {
            try
            {
                var rows = await _repository.GetTemplateAsync(eventType);

                var slugs = rows.Select(r => r.CategorySlug).Distinct().ToList();
                var subcategoryMap = await BuildSubcategoryMapAsync(slugs);

                var dtos = rows.Select(r =>
                {
                    subcategoryMap.TryGetValue(r.CategorySlug, out var sub);
                    return new EventTypeTemplateItemDto
                    {
                        CategorySlug = r.CategorySlug,
                        DisplayLabel = r.DisplayLabel,
                        Priority = r.Priority,
                        DisplayOrder = r.DisplayOrder,
                        SellerSubcategoryId = sub?.Id,
                        SellerCategoryId = sub?.SellerCategoryId
                    };
                }).ToList();

                return Result<List<EventTypeTemplateItemDto>>.Success(dtos, "Template retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve event template for {EventType}.", eventType);
                return Result<List<EventTypeTemplateItemDto>>.Failure(ErrorCodes.Exception, $"Failed to retrieve event template. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<EventPlanDto>> CreateAsync(Guid userId, CreateEventPlanRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                if (string.IsNullOrWhiteSpace(request.Title))
                    return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "Title is required.");

                if (request.GuestCount.HasValue && request.GuestCount.Value < 0)
                    return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "Guest count cannot be negative.");

                if (request.BudgetTotal.HasValue && request.BudgetTotal.Value < 0)
                    return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "Budget cannot be negative.");

                var plan = new EventPlan
                {
                    Id = Guid.NewGuid(),
                    Code = GenerateCode(),
                    UserId = userId,
                    EventType = request.EventType,
                    Title = request.Title.Trim(),
                    EventDate = request.EventDate,
                    GuestCount = request.GuestCount,
                    LocationArea = request.LocationArea?.Trim(),
                    BudgetTotal = request.BudgetTotal,
                    Currency = "ZAR",
                    Status = EventPlanStatus.Active,
                    Notes = request.Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                };

                // Seed the checklist from the template for this event type.
                var templateRows = await _repository.GetTemplateAsync(request.EventType);
                var slugs = templateRows.Select(r => r.CategorySlug).Distinct().ToList();
                var subcategoryMap = await BuildSubcategoryMapAsync(slugs);

                foreach (var row in templateRows)
                {
                    subcategoryMap.TryGetValue(row.CategorySlug, out var sub);

                    plan.Items.Add(new EventPlanItem
                    {
                        Id = Guid.NewGuid(),
                        EventPlanId = plan.Id,
                        SellerSubcategoryId = sub?.Id,
                        CategorySlug = row.CategorySlug,
                        CategoryLabel = row.DisplayLabel,
                        Priority = row.Priority,
                        DisplayOrder = row.DisplayOrder,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                await _repository.AddAsync(plan);
                var saved = await _repository.SaveChangesAsync();

                if (!saved)
                    return Result<EventPlanDto>.Failure(ErrorCodes.Exception, "Failed to create event plan.");

                var reloaded = await _repository.GetByIdAsync(plan.Id);
                return Result<EventPlanDto>.Success(MapToDto(reloaded ?? plan), "Event plan created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event plan create failed for user {UserId}.", userId);
                return Result<EventPlanDto>.Failure(ErrorCodes.Exception, $"Failed to create event plan. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<EventPlanListItemDto>>> GetMineAsync(Guid userId)
        {
            try
            {
                var plans = await _repository.GetByUserAsync(userId);
                var data = plans.Select(MapToListItem).ToList();

                return Result<List<EventPlanListItemDto>>.Success(data, "Your event plans retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve event plans for user {UserId}.", userId);
                return Result<List<EventPlanListItemDto>>.Failure(ErrorCodes.Exception, $"Failed to retrieve your plans. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<EventPlanDto>> GetByIdAsync(Guid userId, Guid planId)
        {
            try
            {
                var plan = await _repository.GetByIdAsync(planId);

                if (plan is null)
                    return Result<EventPlanDto>.Failure(ErrorCodes.NotFound, "Event plan not found.");

                if (plan.UserId != userId)
                    return Result<EventPlanDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to view this plan.");

                return Result<EventPlanDto>.Success(MapToDto(plan), "Event plan retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve event plan {PlanId} for user {UserId}.", planId, userId);
                return Result<EventPlanDto>.Failure(ErrorCodes.Exception, $"Failed to retrieve event plan. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<EventPlanDto>> UpdateAsync(Guid userId, Guid planId, UpdateEventPlanRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var plan = await _repository.GetByIdAsync(planId);

                if (plan is null)
                    return Result<EventPlanDto>.Failure(ErrorCodes.NotFound, "Event plan not found.");

                if (plan.UserId != userId)
                    return Result<EventPlanDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to update this plan.");

                if (!string.IsNullOrWhiteSpace(request.Title))
                    plan.Title = request.Title.Trim();

                if (request.EventDate.HasValue)
                    plan.EventDate = request.EventDate;

                if (request.GuestCount.HasValue)
                {
                    if (request.GuestCount.Value < 0)
                        return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "Guest count cannot be negative.");

                    plan.GuestCount = request.GuestCount;
                }

                if (request.LocationArea != null)
                    plan.LocationArea = request.LocationArea.Trim();

                if (request.BudgetTotal.HasValue)
                {
                    if (request.BudgetTotal.Value < 0)
                        return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "Budget cannot be negative.");

                    plan.BudgetTotal = request.BudgetTotal;
                }

                if (request.Notes != null)
                    plan.Notes = request.Notes.Trim();

                if (request.Status.HasValue)
                    plan.Status = request.Status.Value;

                plan.UpdatedAtUtc = DateTime.UtcNow;

                _repository.Update(plan);
                var saved = await _repository.SaveChangesAsync();

                if (!saved)
                    return Result<EventPlanDto>.Failure(ErrorCodes.Exception, "Failed to update event plan.");

                var reloaded = await _repository.GetByIdAsync(plan.Id);
                return Result<EventPlanDto>.Success(MapToDto(reloaded ?? plan), "Event plan updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update event plan {PlanId} for user {UserId}.", planId, userId);
                return Result<EventPlanDto>.Failure(ErrorCodes.Exception, $"Failed to update event plan. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> ArchiveAsync(Guid userId, Guid planId)
        {
            try
            {
                var plan = await _repository.GetByIdAsync(planId);

                if (plan is null)
                    return Result.Failure(ErrorCodes.NotFound, "Event plan not found.");

                if (plan.UserId != userId)
                    return Result.Failure(ErrorCodes.Forbidden, "You do not have permission to archive this plan.");

                plan.Status = EventPlanStatus.Archived;
                plan.UpdatedAtUtc = DateTime.UtcNow;

                _repository.Update(plan);
                var saved = await _repository.SaveChangesAsync();

                if (!saved)
                    return Result.Failure(ErrorCodes.Exception, "Failed to archive plan.");

                return Result.Success("Event plan archived.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive event plan {PlanId} for user {UserId}.", planId, userId);
                return Result.Failure(ErrorCodes.Exception, $"Failed to archive event plan. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<EventPlanDto>> AddItemAsync(Guid userId, Guid planId, AddEventPlanItemRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var plan = await _repository.GetByIdAsync(planId);

                if (plan is null)
                    return Result<EventPlanDto>.Failure(ErrorCodes.NotFound, "Event plan not found.");

                if (plan.UserId != userId)
                    return Result<EventPlanDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to modify this plan.");

                SellerSubcategory? subcategory = null;
                string? slug = request.CategorySlug?.Trim();
                string? label = request.CategoryLabel?.Trim();

                if (request.SellerSubcategoryId.HasValue)
                {
                    subcategory = await _categoryRepository.GetSubcategoryByIdAsync(request.SellerSubcategoryId.Value);

                    if (subcategory is null)
                        return Result<EventPlanDto>.Failure(ErrorCodes.NotFound, "Subcategory not found.");

                    slug ??= subcategory.Slug;
                    label ??= subcategory.Name;
                }

                if (string.IsNullOrWhiteSpace(slug))
                    return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "Category slug or subcategory id is required.");

                if (string.IsNullOrWhiteSpace(label))
                    label = slug;

                // Enforce uniqueness of slug within a plan (matches the DB unique index).
                if (plan.Items.Any(i => string.Equals(i.CategorySlug, slug, StringComparison.OrdinalIgnoreCase)))
                    return Result<EventPlanDto>.Failure(ErrorCodes.Conflict, "That category is already on the plan.");

                var item = new EventPlanItem
                {
                    Id = Guid.NewGuid(),
                    EventPlanId = plan.Id,
                    SellerSubcategoryId = subcategory?.Id,
                    CategorySlug = slug,
                    CategoryLabel = label,
                    Priority = request.Priority,
                    DisplayOrder = request.DisplayOrder ?? (plan.Items.Count == 0 ? 0 : plan.Items.Max(i => i.DisplayOrder) + 1),
                    Notes = request.Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _repository.AddItemAsync(item);
                plan.UpdatedAtUtc = DateTime.UtcNow;
                _repository.Update(plan);

                var saved = await _repository.SaveChangesAsync();

                if (!saved)
                    return Result<EventPlanDto>.Failure(ErrorCodes.Exception, "Failed to add item.");

                var reloaded = await _repository.GetByIdAsync(plan.Id);
                return Result<EventPlanDto>.Success(MapToDto(reloaded ?? plan), "Item added to plan.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add item to event plan {PlanId}.", planId);
                return Result<EventPlanDto>.Failure(ErrorCodes.Exception, $"Failed to add item. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<EventPlanDto>> UpdateItemAsync(Guid userId, Guid planId, Guid itemId, UpdateEventPlanItemRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var item = await _repository.GetItemByIdAsync(itemId);

                if (item is null || item.EventPlanId != planId)
                    return Result<EventPlanDto>.Failure(ErrorCodes.NotFound, "Plan item not found.");

                if (item.EventPlan?.UserId != userId)
                    return Result<EventPlanDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to modify this plan.");

                // Validate listing (if set): must exist and be active.
                if (request.ListingId.HasValue)
                {
                    var listing = await _listingRepository.GetByIdAsync(request.ListingId.Value);

                    if (listing is null)
                        return Result<EventPlanDto>.Failure(ErrorCodes.NotFound, "Listing not found.");

                    if (listing.Status != ZansiHustle.Shared.Enums.Listings.ListingStatus.Active)
                        return Result<EventPlanDto>.Failure(ErrorCodes.BadRequest, "That listing is no longer available.");

                    item.ListingId = listing.Id;
                    item.EstimatedCost = request.EstimatedCost ?? listing.Price;
                }
                else
                {
                    item.ListingId = null;

                    if (request.EstimatedCost.HasValue)
                        item.EstimatedCost = request.EstimatedCost;
                    else
                        item.EstimatedCost = null;
                }

                if (request.Notes != null)
                    item.Notes = request.Notes.Trim();

                item.UpdatedAtUtc = DateTime.UtcNow;

                _repository.UpdateItem(item);

                if (item.EventPlan is not null)
                {
                    item.EventPlan.UpdatedAtUtc = DateTime.UtcNow;
                    _repository.Update(item.EventPlan);
                }

                var saved = await _repository.SaveChangesAsync();

                if (!saved)
                    return Result<EventPlanDto>.Failure(ErrorCodes.Exception, "Failed to update item.");

                var reloaded = await _repository.GetByIdAsync(planId);
                if (reloaded is null)
                    return Result<EventPlanDto>.Failure(ErrorCodes.NotFound, "Event plan not found.");

                return Result<EventPlanDto>.Success(MapToDto(reloaded), "Plan item updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update plan item {ItemId} on plan {PlanId}.", itemId, planId);
                return Result<EventPlanDto>.Failure(ErrorCodes.Exception, $"Failed to update item. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<EventPlanDto>> RemoveItemAsync(Guid userId, Guid planId, Guid itemId)
        {
            try
            {
                var item = await _repository.GetItemByIdAsync(itemId);

                if (item is null || item.EventPlanId != planId)
                    return Result<EventPlanDto>.Failure(ErrorCodes.NotFound, "Plan item not found.");

                if (item.EventPlan?.UserId != userId)
                    return Result<EventPlanDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to modify this plan.");

                _repository.RemoveItem(item);

                if (item.EventPlan is not null)
                {
                    item.EventPlan.UpdatedAtUtc = DateTime.UtcNow;
                    _repository.Update(item.EventPlan);
                }

                var saved = await _repository.SaveChangesAsync();

                if (!saved)
                    return Result<EventPlanDto>.Failure(ErrorCodes.Exception, "Failed to remove item.");

                var reloaded = await _repository.GetByIdAsync(planId);
                if (reloaded is null)
                    return Result<EventPlanDto>.Failure(ErrorCodes.NotFound, "Event plan not found.");

                return Result<EventPlanDto>.Success(MapToDto(reloaded), "Item removed from plan.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove plan item {ItemId} on plan {PlanId}.", itemId, planId);
                return Result<EventPlanDto>.Failure(ErrorCodes.Exception, $"Failed to remove item. {ex.Message}");
            }
        }

        // ─── Helpers ───────────────────────────────────────────────────────

        private async Task<Dictionary<string, SellerSubcategory>> BuildSubcategoryMapAsync(List<string> slugs)
        {
            var map = new Dictionary<string, SellerSubcategory>(StringComparer.OrdinalIgnoreCase);
            if (slugs.Count == 0) return map;

            // Load all categories + their subcategories once; cheap and avoids
            // N repository calls per template row.
            var categories = await _categoryRepository.GetAllAsync(activeOnly: true);

            foreach (var category in categories)
            {
                foreach (var sub in category.Subcategories)
                {
                    if (!sub.IsActive) continue;
                    if (string.IsNullOrWhiteSpace(sub.Slug)) continue;
                    if (!slugs.Contains(sub.Slug, StringComparer.OrdinalIgnoreCase)) continue;

                    // Ensure the category link travels with the subcategory so the
                    // DTO can expose the parent category id too.
                    if (sub.SellerCategoryId == Guid.Empty)
                        sub.SellerCategoryId = category.Id;

                    map[sub.Slug] = sub;
                }
            }

            return map;
        }

        private static string GenerateCode() => $"EVT-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        private static EventPlanDto MapToDto(EventPlan plan)
        {
            var items = plan.Items
                .OrderBy(i => i.Priority)
                .ThenBy(i => i.DisplayOrder)
                .Select(MapItem)
                .ToList();

            return new EventPlanDto
            {
                Id = plan.Id,
                Code = plan.Code,
                UserId = plan.UserId,
                EventType = plan.EventType,
                Title = plan.Title,
                EventDate = plan.EventDate,
                GuestCount = plan.GuestCount,
                LocationArea = plan.LocationArea,
                BudgetTotal = plan.BudgetTotal,
                Currency = plan.Currency,
                Status = plan.Status,
                Notes = plan.Notes,
                CreatedAtUtc = plan.CreatedAtUtc,
                UpdatedAtUtc = plan.UpdatedAtUtc,
                SelectedCount = items.Count(i => i.ListingId.HasValue),
                TotalCount = items.Count,
                EstimatedTotal = items.Where(i => i.EstimatedCost.HasValue).Sum(i => i.EstimatedCost!.Value),
                Items = items
            };
        }

        private static EventPlanListItemDto MapToListItem(EventPlan plan)
        {
            return new EventPlanListItemDto
            {
                Id = plan.Id,
                Code = plan.Code,
                EventType = plan.EventType,
                Title = plan.Title,
                EventDate = plan.EventDate,
                GuestCount = plan.GuestCount,
                LocationArea = plan.LocationArea,
                Status = plan.Status,
                SelectedCount = plan.Items.Count(i => i.ListingId.HasValue),
                TotalCount = plan.Items.Count,
                BudgetTotal = plan.BudgetTotal,
                EstimatedTotal = plan.Items.Where(i => i.EstimatedCost.HasValue).Sum(i => i.EstimatedCost!.Value),
                Currency = plan.Currency,
                CreatedAtUtc = plan.CreatedAtUtc
            };
        }

        private static EventPlanItemDto MapItem(EventPlanItem item)
        {
            return new EventPlanItemDto
            {
                Id = item.Id,
                EventPlanId = item.EventPlanId,
                SellerSubcategoryId = item.SellerSubcategoryId,
                CategorySlug = item.CategorySlug,
                CategoryLabel = item.CategoryLabel,
                Priority = item.Priority,
                DisplayOrder = item.DisplayOrder,
                ListingId = item.ListingId,
                ListingTitle = item.Listing?.Title,
                ListingImageUrl = item.Listing?.Images != null && item.Listing.Images.Count > 0
                    ? item.Listing.Images[0]
                    : null,
                MerchantName = item.Listing?.Merchant?.Name,
                EstimatedCost = item.EstimatedCost,
                Notes = item.Notes,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            };
        }
    }
}
