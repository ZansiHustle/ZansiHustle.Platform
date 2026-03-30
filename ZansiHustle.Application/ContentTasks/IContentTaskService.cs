using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.ContentTasks.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ContentTasks
{
    /// <summary>
    /// Service contract for content task operations.
    /// </summary>
    public interface IContentTaskService
    {
        Task<Result<List<ContentTaskListItemDto>>> GetAllAsync();
        Task<Result<ContentTaskDetailsDto>> GetByIdAsync(Guid id);
        Task<Result<ContentTaskDetailsDto>> CreateAsync(CreateContentTaskRequestDto request);
        Task<Result<ContentTaskDetailsDto>> UpdateAsync(Guid id, UpdateContentTaskRequestDto request);
        Task<Result> DeleteAsync(Guid id);
    }
}
