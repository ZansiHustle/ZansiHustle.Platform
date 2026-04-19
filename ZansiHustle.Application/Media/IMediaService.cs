using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Media.Dtos;
using ZansiHustle.Shared.Enums.Media;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Media
{
    public interface IMediaService
    {
        /// <summary>Issue a signed upload URL + create the Pending row.</summary>
        Task<Result<IssueUploadResponseDto>> IssueUploadAsync(IssueUploadRequestDto request);

        /// <summary>Confirm the blob is now in storage; flip to Uploaded / PendingReview.</summary>
        Task<Result<MediaAssetDto>> FinalizeAsync(Guid id);

        /// <summary>Look up an asset and return a DTO with a freshly-signed read URL.</summary>
        Task<Result<MediaAssetDto>> GetAsync(Guid id);

        /// <summary>List all media for an owner. Caller is responsible for authorization.</summary>
        Task<Result<List<MediaAssetDto>>> GetByOwnerAsync(OwnerEntityType ownerType, Guid ownerId);

        /// <summary>Admin review: Approve or Reject a verification asset.</summary>
        Task<Result<MediaAssetDto>> ReviewAsync(Guid id, ReviewMediaRequestDto request);

        /// <summary>
        /// Re-parent a set of orphan uploads onto a freshly-created owner. Used
        /// at the end of MerchantOnboarding to attach ID doc / selfie /
        /// product-sample uploads to the new Merchant id. Validates that each
        /// asset was uploaded by the current user AND is not already attached.
        /// </summary>
        Task<Result> AttachToOwnerAsync(
            IEnumerable<Guid> mediaAssetIds,
            OwnerEntityType ownerType,
            Guid ownerId);
    }
}
