using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Persistence.Podcasts;
using ZansiHustle.Application.Podcasts.Dtos;
using ZansiHustle.Domain.Podcasts;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Podcasts
{
    /// <summary>
    /// Provides business logic for podcast operations.
    /// </summary>
    public class PodcastService : IPodcastService
    {
        private readonly IPodcastRepository _podcastRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="PodcastService"/> class.
        /// </summary>
        public PodcastService(IPodcastRepository podcastRepository)
        {
            _podcastRepository = podcastRepository;
        }

        /// <inheritdoc />
        public async Task<Result<List<PodcastListItemDto>>> GetAllAsync()
        {
            try
            {
                var podcasts = await _podcastRepository.GetAllAsync();
                var data = podcasts.Select(MapToListItemDto).ToList();

                return Result<List<PodcastListItemDto>>.Success(data, "Podcasts retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<PodcastListItemDto>>.Failure($"An error occurred while retrieving podcasts. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<PodcastDetailsDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var podcast = await _podcastRepository.GetByIdAsync(id);

                if (podcast is null)
                {
                    return Result<PodcastDetailsDto>.Failure("Podcast not found.");
                }

                return Result<PodcastDetailsDto>.Success(MapToDetailsDto(podcast), "Podcast retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<PodcastDetailsDto>.Failure($"An error occurred while retrieving the podcast. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<PodcastDetailsDto>> CreateAsync(CreatePodcastRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<PodcastDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Result<PodcastDetailsDto>.Failure("Name is required.");
                }

                var entity = new Podcast
                {
                    Id = Guid.NewGuid(),
                    Code = $"POD-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    Name = request.Name.Trim(),
                    HostName = request.HostName?.Trim(),
                    Country = request.Country?.Trim(),
                    Region = request.Region?.Trim(),
                    Niche = request.Niche?.Trim(),
                    AudienceSize = request.AudienceSize,
                    WebsiteUrl = request.WebsiteUrl?.Trim(),
                    ContactEmail = request.ContactEmail?.Trim(),
                    MediaKitUrl = request.MediaKitUrl?.Trim(),
                    AllowsGuestAppearance = request.AllowsGuestAppearance,
                    AllowsSponsoredSegments = request.AllowsSponsoredSegments,
                    QuotedPrice = request.QuotedPrice,
                    Notes = request.Notes?.Trim(),
                    AddedByUserId = request.AddedByUserId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                foreach (var format in request.AdFormats)
                {
                    if (!string.IsNullOrWhiteSpace(format.FormatName))
                    {
                        entity.AdFormats.Add(new PodcastAdFormat
                        {
                            Id = Guid.NewGuid(),
                            FormatName = format.FormatName.Trim(),
                            CreatedAtUtc = DateTime.UtcNow
                        });
                    }
                }

                await _podcastRepository.AddAsync(entity);

                var saved = await _podcastRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<PodcastDetailsDto>.Failure("Failed to create podcast.");
                }

                var createdEntity = await _podcastRepository.GetByIdAsync(entity.Id) ?? entity;

                return Result<PodcastDetailsDto>.Success(MapToDetailsDto(createdEntity), "Podcast created successfully.");
            }
            catch (Exception ex)
            {
                return Result<PodcastDetailsDto>.Failure($"An error occurred while creating the podcast. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<PodcastDetailsDto>> UpdateAsync(Guid id, UpdatePodcastRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<PodcastDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Result<PodcastDetailsDto>.Failure("Name is required.");
                }

                var podcast = await _podcastRepository.GetByIdAsync(id);

                if (podcast is null)
                {
                    return Result<PodcastDetailsDto>.Failure("Podcast not found.");
                }

                podcast.Name = request.Name.Trim();
                podcast.HostName = request.HostName?.Trim();
                podcast.Country = request.Country?.Trim();
                podcast.Region = request.Region?.Trim();
                podcast.Niche = request.Niche?.Trim();
                podcast.AudienceSize = request.AudienceSize;
                podcast.WebsiteUrl = request.WebsiteUrl?.Trim();
                podcast.ContactEmail = request.ContactEmail?.Trim();
                podcast.MediaKitUrl = request.MediaKitUrl?.Trim();
                podcast.AllowsGuestAppearance = request.AllowsGuestAppearance;
                podcast.AllowsSponsoredSegments = request.AllowsSponsoredSegments;
                podcast.QuotedPrice = request.QuotedPrice;
                podcast.Notes = request.Notes?.Trim();
                podcast.UpdatedAtUtc = DateTime.UtcNow;

                podcast.AdFormats.Clear();

                foreach (var format in request.AdFormats)
                {
                    if (!string.IsNullOrWhiteSpace(format.FormatName))
                    {
                        podcast.AdFormats.Add(new PodcastAdFormat
                        {
                            Id = Guid.NewGuid(),
                            PodcastId = podcast.Id,
                            FormatName = format.FormatName.Trim(),
                            CreatedAtUtc = DateTime.UtcNow
                        });
                    }
                }

                _podcastRepository.Update(podcast);

                var saved = await _podcastRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<PodcastDetailsDto>.Failure("Failed to update podcast.");
                }

                var updatedEntity = await _podcastRepository.GetByIdAsync(podcast.Id) ?? podcast;

                return Result<PodcastDetailsDto>.Success(MapToDetailsDto(updatedEntity), "Podcast updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<PodcastDetailsDto>.Failure($"An error occurred while updating the podcast. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<PodcastDetailsDto>> UpdateStatusAsync(Guid id, UpdatePodcastStatusRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<PodcastDetailsDto>.Failure("Request is required.");
                }

                var podcast = await _podcastRepository.GetByIdAsync(id);

                if (podcast is null)
                {
                    return Result<PodcastDetailsDto>.Failure("Podcast not found.");
                }

                podcast.OutreachStatus = request.OutreachStatus;
                podcast.ResponseStatus = request.ResponseStatus;
                podcast.UpdatedAtUtc = DateTime.UtcNow;

                _podcastRepository.Update(podcast);

                var saved = await _podcastRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<PodcastDetailsDto>.Failure("Failed to update podcast status.");
                }

                return Result<PodcastDetailsDto>.Success(MapToDetailsDto(podcast), "Podcast status updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<PodcastDetailsDto>.Failure($"An error occurred while updating podcast status. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var podcast = await _podcastRepository.GetByIdAsync(id);

                if (podcast is null)
                {
                    return Result.Failure("Podcast not found.");
                }

                _podcastRepository.Delete(podcast);

                var saved = await _podcastRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result.Failure("Failed to delete podcast.");
                }

                return Result.Success("Podcast deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"An error occurred while deleting the podcast. {ex.Message}");
            }
        }

        private static PodcastListItemDto MapToListItemDto(Podcast podcast)
        {
            return new PodcastListItemDto
            {
                Id = podcast.Id,
                Code = podcast.Code,
                Name = podcast.Name,
                HostName = podcast.HostName,
                Country = podcast.Country,
                Region = podcast.Region,
                Niche = podcast.Niche,
                AudienceSize = podcast.AudienceSize,
                ContactEmail = podcast.ContactEmail,
                QuotedPrice = podcast.QuotedPrice,
                OutreachStatus = podcast.OutreachStatus,
                ResponseStatus = podcast.ResponseStatus,
                AdFormats = podcast.AdFormats.Select(MapAdFormatDto).ToList(),
                CreatedAtUtc = podcast.CreatedAtUtc
            };
        }

        private static PodcastDetailsDto MapToDetailsDto(Podcast podcast)
        {
            return new PodcastDetailsDto
            {
                Id = podcast.Id,
                Code = podcast.Code,
                Name = podcast.Name,
                HostName = podcast.HostName,
                Country = podcast.Country,
                Region = podcast.Region,
                Niche = podcast.Niche,
                AudienceSize = podcast.AudienceSize,
                WebsiteUrl = podcast.WebsiteUrl,
                ContactEmail = podcast.ContactEmail,
                MediaKitUrl = podcast.MediaKitUrl,
                AllowsGuestAppearance = podcast.AllowsGuestAppearance,
                AllowsSponsoredSegments = podcast.AllowsSponsoredSegments,
                QuotedPrice = podcast.QuotedPrice,
                Notes = podcast.Notes,
                AddedByUserId = podcast.AddedByUserId,
                OutreachStatus = podcast.OutreachStatus,
                ResponseStatus = podcast.ResponseStatus,
                AdFormats = podcast.AdFormats.Select(MapAdFormatDto).ToList(),
                CreatedAtUtc = podcast.CreatedAtUtc,
                UpdatedAtUtc = podcast.UpdatedAtUtc
            };
        }

        private static PodcastAdFormatDto MapAdFormatDto(PodcastAdFormat adFormat)
        {
            return new PodcastAdFormatDto
            {
                FormatName = adFormat.FormatName
            };
        }
    }
}
