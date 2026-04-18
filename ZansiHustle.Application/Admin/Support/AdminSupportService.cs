using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Support.Dtos;
using ZansiHustle.Application.Persistence.Admin.Support;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Support
{
    public class AdminSupportService : IAdminSupportService
    {
        private readonly IAdminSupportRepository _repository;

        public AdminSupportService(IAdminSupportRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<SupportKpisDto>> GetKpisAsync()
        {
            try
            {
                var data = await _repository.GetKpisAsync();
                return Result<SupportKpisDto>.Success(data, "Support KPIs retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<SupportKpisDto>.Failure($"Failed to retrieve support KPIs. {ex.Message}");
            }
        }

        public async Task<Result<PagedResult<AdminTicketListItemDto>>> GetTicketsAsync(TicketsListQuery query)
        {
            // Clamp the base fields; Priority is optional and validated at
            // the repository layer (unknown values fall through).
            var clamped = (TicketsListQuery)PagedQueryGuard.Clamp(query ?? new TicketsListQuery());
            if (clamped != query && query != null) clamped.Priority = query.Priority;

            try
            {
                var data = await _repository.GetTicketsPagedAsync(clamped);
                return Result<PagedResult<AdminTicketListItemDto>>.Success(data, "Tickets retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<PagedResult<AdminTicketListItemDto>>.Failure($"Failed to retrieve tickets. {ex.Message}");
            }
        }

        public async Task<Result<PagedResult<AdminDisputeListItemDto>>> GetDisputesAsync(PagedListQueryBase query)
        {
            var safe = PagedQueryGuard.Clamp(query);

            try
            {
                var data = await _repository.GetDisputesPagedAsync(safe);
                return Result<PagedResult<AdminDisputeListItemDto>>.Success(data, "Disputes retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<PagedResult<AdminDisputeListItemDto>>.Failure($"Failed to retrieve disputes. {ex.Message}");
            }
        }

        public async Task<Result<List<AdminSupportAgentDto>>> GetAgentsAsync()
        {
            try
            {
                var data = await _repository.GetAgentsAsync();
                return Result<List<AdminSupportAgentDto>>.Success(data, "Support agents retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<AdminSupportAgentDto>>.Failure($"Failed to retrieve support agents. {ex.Message}");
            }
        }

        public async Task<Result<List<BacklogTrendPointDto>>> GetBacklogTrendAsync(int days = 14)
        {
            if (days < 1) days = 1;
            if (days > 90) days = 90;

            try
            {
                var data = await _repository.GetBacklogTrendAsync(days);
                return Result<List<BacklogTrendPointDto>>.Success(data, "Backlog trend retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<BacklogTrendPointDto>>.Failure($"Failed to retrieve backlog trend. {ex.Message}");
            }
        }

        public async Task<Result<List<RecentEscalationDto>>> GetRecentEscalationsAsync(int limit = 10)
        {
            if (limit < 1) limit = 1;
            if (limit > 100) limit = 100;

            try
            {
                var data = await _repository.GetRecentEscalationsAsync(limit);
                return Result<List<RecentEscalationDto>>.Success(data, "Recent escalations retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<RecentEscalationDto>>.Failure($"Failed to retrieve recent escalations. {ex.Message}");
            }
        }
    }
}
