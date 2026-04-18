using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Fundraising.Dtos;
using ZansiHustle.Application.Persistence.Fundraising;
using ZansiHustle.Domain.Fundraising;
using ZansiHustle.Shared.Enums.Fundraising;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Fundraising
{
    public class FundraisingService : IFundraisingService
    {
        private const string ProjectionDisclaimer =
            "For private fundraising discussions only. These figures are illustrative projections based on admin-set scenario valuations. They are not guarantees, offers, or solicitations of securities. Actual outcomes depend on business performance, market conditions, and other factors.";

        private readonly IValuationRepository _valuationRepository;
        private readonly IStakeholderRepository _stakeholderRepository;
        private readonly ILogger<FundraisingService> _logger;

        public FundraisingService(IValuationRepository valuationRepository, IStakeholderRepository stakeholderRepository, ILogger<FundraisingService> logger)
        {
            _valuationRepository = valuationRepository;
            _stakeholderRepository = stakeholderRepository;
            _logger = logger;
        }

        // ── Valuations ──────────────────────────────────────────────────

        public async Task<Result<ValuationDto>> GetValuationByIdAsync(Guid id)
        {
            try
            {
                var v = await _valuationRepository.GetByIdAsync(id);
                if (v is null)
                    return Result<ValuationDto>.Failure(ErrorCodes.NotFound, "Valuation not found.");

                return Result<ValuationDto>.Success(MapValuation(v), "Valuation retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get valuation {Id}.", id);
                return Result<ValuationDto>.Failure(ErrorCodes.Exception, $"Failed to get valuation. {ex.Message}");
            }
        }

        public async Task<Result<ValuationDto?>> GetActiveValuationAsync()
        {
            try
            {
                var v = await _valuationRepository.GetActiveAsync();
                return Result<ValuationDto?>.Success(v is null ? null : MapValuation(v), "Active valuation retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active valuation.");
                return Result<ValuationDto?>.Failure(ErrorCodes.Exception, $"Failed to get active valuation. {ex.Message}");
            }
        }

        public async Task<Result<ActiveValuationPublicDto?>> GetActivePublicAsync()
        {
            try
            {
                var v = await _valuationRepository.GetActiveAsync();
                if (v is null)
                    return Result<ActiveValuationPublicDto?>.Success(null, "No active valuation.");

                return Result<ActiveValuationPublicDto?>.Success(new ActiveValuationPublicDto
                {
                    Id = v.Id,
                    Label = v.Label,
                    FundraisingValuation = v.FundraisingValuation,
                    ScenarioConservative = v.ScenarioConservative,
                    ScenarioModerate = v.ScenarioModerate,
                    ScenarioAggressive = v.ScenarioAggressive,
                    ScenarioHorizonLabel = v.ScenarioHorizonLabel,
                    Currency = v.Currency,
                    EffectiveFromUtc = v.EffectiveFromUtc
                }, "Active valuation retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active valuation (public).");
                return Result<ActiveValuationPublicDto?>.Failure(ErrorCodes.Exception, $"Failed to get active valuation. {ex.Message}");
            }
        }

        public async Task<Result<List<ValuationDto>>> GetAllValuationsAsync()
        {
            try
            {
                var list = await _valuationRepository.GetAllAsync();
                return Result<List<ValuationDto>>.Success(list.Select(MapValuation).ToList(), "Valuations retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list valuations.");
                return Result<List<ValuationDto>>.Failure(ErrorCodes.Exception, $"Failed to list valuations. {ex.Message}");
            }
        }

        public async Task<Result<ValuationDto>> CreateValuationAsync(Guid? createdByUserId, CreateValuationRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<ValuationDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var validate = ValidateValuationFields(request.Label, request.InternalBaseline, request.FundraisingValuation, request.ScenarioConservative, request.ScenarioModerate, request.ScenarioAggressive);
                if (!validate.IsSuccess)
                    return Result<ValuationDto>.Failure(validate.Code, validate.Message);

                var now = DateTime.UtcNow;
                var valuation = new Valuation
                {
                    Id = Guid.NewGuid(),
                    Label = request.Label.Trim(),
                    InternalBaseline = request.InternalBaseline,
                    FundraisingValuation = request.FundraisingValuation,
                    ScenarioConservative = request.ScenarioConservative,
                    ScenarioModerate = request.ScenarioModerate,
                    ScenarioAggressive = request.ScenarioAggressive,
                    ScenarioHorizonLabel = request.ScenarioHorizonLabel?.Trim(),
                    Currency = "ZAR",
                    IsActive = false,
                    EffectiveFromUtc = request.EffectiveFromUtc ?? now,
                    Notes = request.Notes?.Trim(),
                    CreatedByUserId = createdByUserId,
                    CreatedAtUtc = now
                };

                if (request.ActivateImmediately)
                {
                    await DeactivateCurrentActiveAsync();
                    valuation.IsActive = true;
                }

                await _valuationRepository.AddAsync(valuation);
                var saved = await _valuationRepository.SaveChangesAsync();

                if (!saved)
                    return Result<ValuationDto>.Failure(ErrorCodes.Exception, "Failed to create valuation.");

                return Result<ValuationDto>.Success(MapValuation(valuation), "Valuation created.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create valuation.");
                return Result<ValuationDto>.Failure(ErrorCodes.Exception, $"Failed to create valuation. {ex.Message}");
            }
        }

        public async Task<Result<ValuationDto>> UpdateValuationAsync(Guid id, UpdateValuationRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<ValuationDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var v = await _valuationRepository.GetByIdAsync(id);
                if (v is null)
                    return Result<ValuationDto>.Failure(ErrorCodes.NotFound, "Valuation not found.");

                if (!string.IsNullOrWhiteSpace(request.Label)) v.Label = request.Label.Trim();
                if (request.InternalBaseline.HasValue) v.InternalBaseline = request.InternalBaseline.Value;
                if (request.FundraisingValuation.HasValue) v.FundraisingValuation = request.FundraisingValuation.Value;
                if (request.ScenarioConservative.HasValue) v.ScenarioConservative = request.ScenarioConservative.Value;
                if (request.ScenarioModerate.HasValue) v.ScenarioModerate = request.ScenarioModerate.Value;
                if (request.ScenarioAggressive.HasValue) v.ScenarioAggressive = request.ScenarioAggressive.Value;
                if (request.ScenarioHorizonLabel != null) v.ScenarioHorizonLabel = request.ScenarioHorizonLabel.Trim();
                if (request.Notes != null) v.Notes = request.Notes.Trim();
                if (request.EffectiveFromUtc.HasValue) v.EffectiveFromUtc = request.EffectiveFromUtc.Value;

                var validate = ValidateValuationFields(v.Label, v.InternalBaseline, v.FundraisingValuation, v.ScenarioConservative, v.ScenarioModerate, v.ScenarioAggressive);
                if (!validate.IsSuccess)
                    return Result<ValuationDto>.Failure(validate.Code, validate.Message);

                v.UpdatedAtUtc = DateTime.UtcNow;
                _valuationRepository.Update(v);
                var saved = await _valuationRepository.SaveChangesAsync();

                if (!saved)
                    return Result<ValuationDto>.Failure(ErrorCodes.Exception, "Failed to update valuation.");

                return Result<ValuationDto>.Success(MapValuation(v), "Valuation updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update valuation {Id}.", id);
                return Result<ValuationDto>.Failure(ErrorCodes.Exception, $"Failed to update valuation. {ex.Message}");
            }
        }

        public async Task<Result<ValuationDto>> ActivateValuationAsync(Guid id)
        {
            try
            {
                var target = await _valuationRepository.GetByIdAsync(id);
                if (target is null)
                    return Result<ValuationDto>.Failure(ErrorCodes.NotFound, "Valuation not found.");

                if (target.IsActive)
                    return Result<ValuationDto>.Success(MapValuation(target), "Valuation already active.");

                await DeactivateCurrentActiveAsync();

                target.IsActive = true;
                target.UpdatedAtUtc = DateTime.UtcNow;
                _valuationRepository.Update(target);

                var saved = await _valuationRepository.SaveChangesAsync();
                if (!saved)
                    return Result<ValuationDto>.Failure(ErrorCodes.Exception, "Failed to activate valuation.");

                return Result<ValuationDto>.Success(MapValuation(target), "Valuation activated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate valuation {Id}.", id);
                return Result<ValuationDto>.Failure(ErrorCodes.Exception, $"Failed to activate valuation. {ex.Message}");
            }
        }

        public async Task<Result> DeleteValuationAsync(Guid id)
        {
            try
            {
                var v = await _valuationRepository.GetByIdAsync(id);
                if (v is null)
                    return Result.Failure(ErrorCodes.NotFound, "Valuation not found.");

                if (v.IsActive)
                    return Result.Failure(ErrorCodes.BadRequest, "Cannot delete the active valuation. Activate another valuation first.");

                _valuationRepository.Delete(v);
                var saved = await _valuationRepository.SaveChangesAsync();

                if (!saved)
                    return Result.Failure(ErrorCodes.Exception, "Failed to delete valuation.");

                return Result.Success("Valuation deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete valuation {Id}.", id);
                return Result.Failure(ErrorCodes.Exception, $"Failed to delete valuation. {ex.Message}");
            }
        }

        // ── Stakeholders ────────────────────────────────────────────────

        public async Task<Result<List<StakeholderDto>>> GetStakeholdersAsync(StakeholderType? type, bool activeOnly)
        {
            try
            {
                var list = await _stakeholderRepository.GetAllAsync(type, activeOnly);
                return Result<List<StakeholderDto>>.Success(list.Select(MapStakeholder).ToList(), "Stakeholders retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list stakeholders.");
                return Result<List<StakeholderDto>>.Failure(ErrorCodes.Exception, $"Failed to list stakeholders. {ex.Message}");
            }
        }

        public async Task<Result<StakeholderDto>> GetStakeholderByIdAsync(Guid id)
        {
            try
            {
                var s = await _stakeholderRepository.GetByIdAsync(id);
                if (s is null)
                    return Result<StakeholderDto>.Failure(ErrorCodes.NotFound, "Stakeholder not found.");

                return Result<StakeholderDto>.Success(MapStakeholder(s), "Stakeholder retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get stakeholder {Id}.", id);
                return Result<StakeholderDto>.Failure(ErrorCodes.Exception, $"Failed to get stakeholder. {ex.Message}");
            }
        }

        public async Task<Result<StakeholderDto>> CreateStakeholderAsync(Guid? createdByUserId, CreateStakeholderRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<StakeholderDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                if (string.IsNullOrWhiteSpace(request.FullName))
                    return Result<StakeholderDto>.Failure(ErrorCodes.BadRequest, "Full name is required.");

                if (request.PercentageOwned < 0 || request.PercentageOwned > 100)
                    return Result<StakeholderDto>.Failure(ErrorCodes.BadRequest, "Percentage must be between 0 and 100.");

                if (request.AmountInvested.HasValue && request.AmountInvested.Value < 0)
                    return Result<StakeholderDto>.Failure(ErrorCodes.BadRequest, "Amount invested cannot be negative.");

                if (request.PricingBasisValuation < 0)
                    return Result<StakeholderDto>.Failure(ErrorCodes.BadRequest, "Pricing basis valuation cannot be negative.");

                var now = DateTime.UtcNow;
                var s = new Stakeholder
                {
                    Id = Guid.NewGuid(),
                    FullName = request.FullName.Trim(),
                    Type = request.Type,
                    Email = request.Email?.Trim(),
                    Phone = request.Phone?.Trim(),
                    PercentageOwned = request.PercentageOwned,
                    AmountInvested = request.AmountInvested,
                    PricingBasisValuation = request.PricingBasisValuation,
                    EntryDateUtc = request.EntryDateUtc ?? now,
                    AgreementReference = request.AgreementReference?.Trim(),
                    Notes = request.Notes?.Trim(),
                    IsActive = true,
                    CreatedByUserId = createdByUserId,
                    CreatedAtUtc = now
                };

                await _stakeholderRepository.AddAsync(s);
                var saved = await _stakeholderRepository.SaveChangesAsync();

                if (!saved)
                    return Result<StakeholderDto>.Failure(ErrorCodes.Exception, "Failed to create stakeholder.");

                return Result<StakeholderDto>.Success(MapStakeholder(s), "Stakeholder created.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create stakeholder.");
                return Result<StakeholderDto>.Failure(ErrorCodes.Exception, $"Failed to create stakeholder. {ex.Message}");
            }
        }

        public async Task<Result<StakeholderDto>> UpdateStakeholderAsync(Guid id, UpdateStakeholderRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<StakeholderDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var s = await _stakeholderRepository.GetByIdAsync(id);
                if (s is null)
                    return Result<StakeholderDto>.Failure(ErrorCodes.NotFound, "Stakeholder not found.");

                if (!string.IsNullOrWhiteSpace(request.FullName)) s.FullName = request.FullName.Trim();
                if (request.Type.HasValue) s.Type = request.Type.Value;
                if (request.Email != null) s.Email = request.Email.Trim();
                if (request.Phone != null) s.Phone = request.Phone.Trim();

                if (request.PercentageOwned.HasValue)
                {
                    if (request.PercentageOwned.Value < 0 || request.PercentageOwned.Value > 100)
                        return Result<StakeholderDto>.Failure(ErrorCodes.BadRequest, "Percentage must be between 0 and 100.");
                    s.PercentageOwned = request.PercentageOwned.Value;
                }

                if (request.AmountInvested.HasValue)
                {
                    if (request.AmountInvested.Value < 0)
                        return Result<StakeholderDto>.Failure(ErrorCodes.BadRequest, "Amount invested cannot be negative.");
                    s.AmountInvested = request.AmountInvested;
                }

                if (request.PricingBasisValuation.HasValue)
                {
                    if (request.PricingBasisValuation.Value < 0)
                        return Result<StakeholderDto>.Failure(ErrorCodes.BadRequest, "Pricing basis valuation cannot be negative.");
                    s.PricingBasisValuation = request.PricingBasisValuation.Value;
                }

                if (request.EntryDateUtc.HasValue) s.EntryDateUtc = request.EntryDateUtc.Value;
                if (request.AgreementReference != null) s.AgreementReference = request.AgreementReference.Trim();
                if (request.Notes != null) s.Notes = request.Notes.Trim();
                if (request.IsActive.HasValue) s.IsActive = request.IsActive.Value;

                s.UpdatedAtUtc = DateTime.UtcNow;
                _stakeholderRepository.Update(s);

                var saved = await _stakeholderRepository.SaveChangesAsync();
                if (!saved)
                    return Result<StakeholderDto>.Failure(ErrorCodes.Exception, "Failed to update stakeholder.");

                return Result<StakeholderDto>.Success(MapStakeholder(s), "Stakeholder updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update stakeholder {Id}.", id);
                return Result<StakeholderDto>.Failure(ErrorCodes.Exception, $"Failed to update stakeholder. {ex.Message}");
            }
        }

        public async Task<Result> DeactivateStakeholderAsync(Guid id)
        {
            try
            {
                var s = await _stakeholderRepository.GetByIdAsync(id);
                if (s is null)
                    return Result.Failure(ErrorCodes.NotFound, "Stakeholder not found.");

                s.IsActive = false;
                s.UpdatedAtUtc = DateTime.UtcNow;
                _stakeholderRepository.Update(s);

                var saved = await _stakeholderRepository.SaveChangesAsync();
                if (!saved)
                    return Result.Failure(ErrorCodes.Exception, "Failed to deactivate stakeholder.");

                return Result.Success("Stakeholder deactivated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate stakeholder {Id}.", id);
                return Result.Failure(ErrorCodes.Exception, $"Failed to deactivate stakeholder. {ex.Message}");
            }
        }

        // ── Simulator ───────────────────────────────────────────────────

        public async Task<Result<SimulateResponseDto>> SimulateAsync(SimulateRequestDto request)
        {
            try
            {
                if (request is null || request.Amount <= 0)
                    return Result<SimulateResponseDto>.Failure(ErrorCodes.BadRequest, "A positive investment amount is required.");

                var active = await _valuationRepository.GetActiveAsync();
                if (active is null)
                    return Result<SimulateResponseDto>.Failure(ErrorCodes.NotFound, "No active fundraising valuation is set. Please ask an admin to activate one.");

                if (active.FundraisingValuation <= 0)
                    return Result<SimulateResponseDto>.Failure(ErrorCodes.BadRequest, "Active fundraising valuation is invalid.");

                var ownershipPct = Math.Round((request.Amount / active.FundraisingValuation) * 100m, 4);

                ScenarioProjectionDto Project(decimal scenarioValuation)
                {
                    var projected = Math.Round((ownershipPct / 100m) * scenarioValuation, 2);
                    var multiple = request.Amount == 0 ? 0 : Math.Round(projected / request.Amount, 2);
                    return new ScenarioProjectionDto
                    {
                        ScenarioValuation = scenarioValuation,
                        ProjectedStakeValue = projected,
                        ProjectedMultiple = multiple
                    };
                }

                var response = new SimulateResponseDto
                {
                    Amount = request.Amount,
                    Currency = active.Currency,
                    ValuationId = active.Id,
                    ValuationLabel = active.Label,
                    FundraisingValuation = active.FundraisingValuation,
                    ScenarioHorizonLabel = active.ScenarioHorizonLabel,
                    OwnershipPercentage = ownershipPct,
                    ImpliedValueAtEntry = request.Amount,
                    Conservative = Project(active.ScenarioConservative),
                    Moderate = Project(active.ScenarioModerate),
                    Aggressive = Project(active.ScenarioAggressive),
                    Disclaimer = ProjectionDisclaimer
                };

                return Result<SimulateResponseDto>.Success(response, "Projection calculated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simulate failed.");
                return Result<SimulateResponseDto>.Failure(ErrorCodes.Exception, $"Simulate failed. {ex.Message}");
            }
        }

        public async Task<Result<FundraisingSummaryDto>> GetSummaryAsync()
        {
            try
            {
                var active = await _valuationRepository.GetActiveAsync();
                var stakeholders = await _stakeholderRepository.GetAllAsync(null, activeOnly: true);

                var totalPct = stakeholders.Sum(s => s.PercentageOwned);
                var totalInvested = stakeholders.Where(s => s.AmountInvested.HasValue).Sum(s => s.AmountInvested!.Value);

                var summary = new FundraisingSummaryDto
                {
                    ActiveValuationId = active?.Id,
                    ActiveValuationLabel = active?.Label,
                    FundraisingValuation = active?.FundraisingValuation,
                    Currency = active?.Currency ?? "ZAR",
                    StakeholderCount = stakeholders.Count,
                    FounderCount = stakeholders.Count(s => s.Type == StakeholderType.Founder),
                    PartnerCount = stakeholders.Count(s => s.Type == StakeholderType.StrategicPartner),
                    InvestorCount = stakeholders.Count(s => s.Type == StakeholderType.Investor),
                    TotalPercentageAllocated = Math.Round(totalPct, 4),
                    RemainingPercentage = Math.Round(100m - totalPct, 4),
                    TotalAmountInvested = totalInvested
                };

                return Result<FundraisingSummaryDto>.Success(summary, "Summary retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute fundraising summary.");
                return Result<FundraisingSummaryDto>.Failure(ErrorCodes.Exception, $"Failed to compute summary. {ex.Message}");
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private async Task DeactivateCurrentActiveAsync()
        {
            var current = await _valuationRepository.GetActiveAsync();
            if (current is null) return;

            // Reload as tracked entity (GetActiveAsync uses AsNoTracking).
            var tracked = await _valuationRepository.GetByIdAsync(current.Id);
            if (tracked is null || !tracked.IsActive) return;

            tracked.IsActive = false;
            tracked.UpdatedAtUtc = DateTime.UtcNow;
            _valuationRepository.Update(tracked);
            await _valuationRepository.SaveChangesAsync();
        }

        private static Result ValidateValuationFields(string label, decimal internalBaseline, decimal fundraising, decimal conservative, decimal moderate, decimal aggressive)
        {
            if (string.IsNullOrWhiteSpace(label))
                return Result.Failure(ErrorCodes.BadRequest, "Label is required.");

            if (internalBaseline < 0 || fundraising <= 0 || conservative < 0 || moderate < 0 || aggressive < 0)
                return Result.Failure(ErrorCodes.BadRequest, "Valuations must be non-negative and the fundraising valuation must be positive.");

            return Result.Success();
        }

        private static ValuationDto MapValuation(Valuation v) => new()
        {
            Id = v.Id,
            Label = v.Label,
            InternalBaseline = v.InternalBaseline,
            FundraisingValuation = v.FundraisingValuation,
            ScenarioConservative = v.ScenarioConservative,
            ScenarioModerate = v.ScenarioModerate,
            ScenarioAggressive = v.ScenarioAggressive,
            ScenarioHorizonLabel = v.ScenarioHorizonLabel,
            Currency = v.Currency,
            IsActive = v.IsActive,
            EffectiveFromUtc = v.EffectiveFromUtc,
            Notes = v.Notes,
            CreatedAtUtc = v.CreatedAtUtc,
            UpdatedAtUtc = v.UpdatedAtUtc
        };

        private static StakeholderDto MapStakeholder(Stakeholder s) => new()
        {
            Id = s.Id,
            FullName = s.FullName,
            Type = s.Type,
            Email = s.Email,
            Phone = s.Phone,
            PercentageOwned = s.PercentageOwned,
            AmountInvested = s.AmountInvested,
            PricingBasisValuation = s.PricingBasisValuation,
            EntryDateUtc = s.EntryDateUtc,
            AgreementReference = s.AgreementReference,
            Notes = s.Notes,
            IsActive = s.IsActive,
            CreatedAtUtc = s.CreatedAtUtc,
            UpdatedAtUtc = s.UpdatedAtUtc
        };
    }
}
