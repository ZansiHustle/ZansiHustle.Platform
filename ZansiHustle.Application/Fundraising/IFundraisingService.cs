using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Fundraising.Dtos;
using ZansiHustle.Shared.Enums.Fundraising;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Fundraising
{
    /// <summary>
    /// Service contract for the private Fundraising module. All operations
    /// assume an authorized caller — authorization (role filtering) is enforced
    /// at the controller layer.
    /// </summary>
    public interface IFundraisingService
    {
        // Valuations
        Task<Result<ValuationDto>> GetValuationByIdAsync(Guid id);
        Task<Result<ValuationDto?>> GetActiveValuationAsync();
        Task<Result<ActiveValuationPublicDto?>> GetActivePublicAsync();
        Task<Result<List<ValuationDto>>> GetAllValuationsAsync();
        Task<Result<ValuationDto>> CreateValuationAsync(Guid? createdByUserId, CreateValuationRequestDto request);
        Task<Result<ValuationDto>> UpdateValuationAsync(Guid id, UpdateValuationRequestDto request);
        Task<Result<ValuationDto>> ActivateValuationAsync(Guid id);
        Task<Result> DeleteValuationAsync(Guid id);

        // Stakeholders
        Task<Result<List<StakeholderDto>>> GetStakeholdersAsync(StakeholderType? type, bool activeOnly);
        Task<Result<StakeholderDto>> GetStakeholderByIdAsync(Guid id);
        Task<Result<StakeholderDto>> CreateStakeholderAsync(Guid? createdByUserId, CreateStakeholderRequestDto request);
        Task<Result<StakeholderDto>> UpdateStakeholderAsync(Guid id, UpdateStakeholderRequestDto request);
        Task<Result> DeactivateStakeholderAsync(Guid id);

        // Simulator + summary
        Task<Result<SimulateResponseDto>> SimulateAsync(SimulateRequestDto request);
        Task<Result<FundraisingSummaryDto>> GetSummaryAsync();
    }
}
