using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.AppConfigs.Dtos;
using ZansiHustle.Application.Persistence.AppConfigs;
using ZansiHustle.Domain.AppConfigs;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.AppConfigs
{
    public class AppRuntimeConfigService : IAppRuntimeConfigService
    {
        private readonly IAppRuntimeConfigRepository _repository;

        public AppRuntimeConfigService(IAppRuntimeConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<List<AppConfigAdminDto>>> GetAllAsync()
        {
            var items = await _repository.GetAllAsync();
            return Result<List<AppConfigAdminDto>>.Success(items.Select(MapAdmin).ToList(),
                "App configs retrieved successfully.");
        }

        public async Task<Result<AppConfigAdminDto>> UpdateAsync(
            string key, UpdateAppConfigRequestDto request, Guid? updatedByUserId)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Result<AppConfigAdminDto>.Failure("BAD_REQUEST", "Config key is required.");
            if (request is null)
                return Result<AppConfigAdminDto>.Failure("BAD_REQUEST", "No update payload supplied.");

            var config = await _repository.GetByKeyAsync(key);
            if (config is null)
                return Result<AppConfigAdminDto>.Failure("NOT_FOUND", "App config was not found.");

            // Only overwrite fields the caller actually supplied (partial update).
            if (request.BooleanValue.HasValue) config.BooleanValue = request.BooleanValue.Value;
            if (request.NumberValue.HasValue) config.NumberValue = request.NumberValue.Value;
            if (request.StringValue is not null) config.StringValue = request.StringValue;
            if (request.JsonValue is not null) config.JsonValue = request.JsonValue;
            if (request.DisplayName is not null) config.DisplayName = request.DisplayName.Trim();
            if (request.Description is not null) config.Description = request.Description;
            if (request.DisabledTitle is not null) config.DisabledTitle = request.DisabledTitle;
            if (request.DisabledMessage is not null) config.DisabledMessage = request.DisabledMessage;
            if (request.IsPublic.HasValue) config.IsPublic = request.IsPublic.Value;
            if (request.IsActive.HasValue) config.IsActive = request.IsActive.Value;

            config.UpdatedByUserId = updatedByUserId;
            config.UpdatedAtUtc = DateTime.UtcNow;

            _repository.Update(config);
            await _repository.SaveChangesAsync();

            return Result<AppConfigAdminDto>.Success(MapAdmin(config), "App config updated successfully.");
        }

        public async Task<Result<PublicAppConfigsResponseDto>> GetPublicAsync()
        {
            var items = await _repository.GetPublicAsync();

            var response = new PublicAppConfigsResponseDto();
            DateTime? latest = null;

            foreach (var c in items)
            {
                response.Configs[c.Key] = new PublicAppConfigEntryDto
                {
                    // Boolean flags gate on BooleanValue; non-boolean rows are
                    // treated as "enabled" (their value is read separately).
                    Enabled = !string.Equals(c.ValueType, "Boolean", StringComparison.OrdinalIgnoreCase)
                              || c.BooleanValue,
                    Title = c.DisabledTitle,
                    Message = c.DisabledMessage,
                };

                var stamp = c.UpdatedAtUtc ?? c.CreatedAtUtc;
                if (latest is null || stamp > latest) latest = stamp;
            }

            response.UpdatedAtUtc = latest;
            response.Version = latest?.Ticks ?? 0;

            return Result<PublicAppConfigsResponseDto>.Success(response, "App configs retrieved successfully.");
        }

        private static AppConfigAdminDto MapAdmin(AppRuntimeConfig c) => new()
        {
            Id = c.Id,
            Key = c.Key,
            DisplayName = c.DisplayName,
            Description = c.Description,
            Category = c.Category,
            ValueType = c.ValueType,
            BooleanValue = c.BooleanValue,
            StringValue = c.StringValue,
            NumberValue = c.NumberValue,
            JsonValue = c.JsonValue,
            DisabledTitle = c.DisabledTitle,
            DisabledMessage = c.DisabledMessage,
            IsPublic = c.IsPublic,
            IsActive = c.IsActive,
            SortOrder = c.SortOrder,
            UpdatedAtUtc = c.UpdatedAtUtc,
        };
    }
}
