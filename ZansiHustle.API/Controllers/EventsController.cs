using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Events;
using ZansiHustle.Application.Events.Dtos;
using ZansiHustle.Shared.Enums.Events;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Event Builder endpoints — a user-owned checklist experience that helps
    /// buyers plan occasions (weddings, funerals, birthdays etc.) by
    /// recommending service categories and letting them pick providers from
    /// the existing listings catalogue.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : BaseController
    {
        private readonly IEventPlanService _service;
        private readonly ICurrentUserService _currentUserService;

        public EventsController(IEventPlanService service, ICurrentUserService currentUserService)
        {
            _service = service;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Returns the recommended category checklist for the given event type.
        /// Used on the event-basics screen to preview what the plan will include.
        /// </summary>
        [HttpGet("templates/{eventType}")]
        [ProducesResponseType(typeof(Result<List<EventTypeTemplateItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemplate(EventType eventType)
        {
            var result = await _service.GetTemplateAsync(eventType);
            return ToActionResult(result);
        }

        /// <summary>Creates a new plan — items are auto-seeded from the template.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<EventPlanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateEventPlanRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<EventPlanDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.CreateAsync(userId.Value, request);
            return ToActionResult(result);
        }

        /// <summary>Lists the current user's event plans (active + completed).</summary>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(Result<List<EventPlanListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine()
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<List<EventPlanListItemDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.GetMineAsync(userId.Value);
            return ToActionResult(result);
        }

        /// <summary>Returns a single plan owned by the caller.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<EventPlanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<EventPlanDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.GetByIdAsync(userId.Value, id);
            return ToActionResult(result);
        }

        /// <summary>Updates a plan's basics (title, date, budget, status, etc.).</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<EventPlanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventPlanRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<EventPlanDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.UpdateAsync(userId.Value, id, request);
            return ToActionResult(result);
        }

        /// <summary>Archives a plan (soft-delete).</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Archive(Guid id)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.ArchiveAsync(userId.Value, id);
            return ToActionResult(result);
        }

        /// <summary>Adds an ad-hoc category slot (off-template) to an existing plan.</summary>
        [HttpPost("{id:guid}/items")]
        [ProducesResponseType(typeof(Result<EventPlanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddItem(Guid id, [FromBody] AddEventPlanItemRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<EventPlanDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.AddItemAsync(userId.Value, id, request);
            return ToActionResult(result);
        }

        /// <summary>Picks / clears the listing for a slot and sets notes / estimated cost.</summary>
        [HttpPut("{id:guid}/items/{itemId:guid}")]
        [ProducesResponseType(typeof(Result<EventPlanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateItem(Guid id, Guid itemId, [FromBody] UpdateEventPlanItemRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<EventPlanDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.UpdateItemAsync(userId.Value, id, itemId, request);
            return ToActionResult(result);
        }

        /// <summary>Removes a slot from the plan.</summary>
        [HttpDelete("{id:guid}/items/{itemId:guid}")]
        [ProducesResponseType(typeof(Result<EventPlanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveItem(Guid id, Guid itemId)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<EventPlanDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.RemoveItemAsync(userId.Value, id, itemId);
            return ToActionResult(result);
        }
    }
}
