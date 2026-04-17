using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Events.Dtos;
using ZansiHustle.Shared.Enums.Events;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Events
{
    /// <summary>
    /// Service contract for the Event Builder. Plans are user-owned; all
    /// write paths enforce ownership via the JWT user id.
    /// </summary>
    public interface IEventPlanService
    {
        Task<Result<List<EventTypeTemplateItemDto>>> GetTemplateAsync(EventType eventType);

        Task<Result<EventPlanDto>> CreateAsync(Guid userId, CreateEventPlanRequestDto request);

        Task<Result<List<EventPlanListItemDto>>> GetMineAsync(Guid userId);

        Task<Result<EventPlanDto>> GetByIdAsync(Guid userId, Guid planId);

        Task<Result<EventPlanDto>> UpdateAsync(Guid userId, Guid planId, UpdateEventPlanRequestDto request);

        Task<Result> ArchiveAsync(Guid userId, Guid planId);

        Task<Result<EventPlanDto>> AddItemAsync(Guid userId, Guid planId, AddEventPlanItemRequestDto request);

        Task<Result<EventPlanDto>> UpdateItemAsync(Guid userId, Guid planId, Guid itemId, UpdateEventPlanItemRequestDto request);

        Task<Result<EventPlanDto>> RemoveItemAsync(Guid userId, Guid planId, Guid itemId);
    }
}
