using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Media.Dtos
{
    public class IssueUploadResponseDto
    {
        public Guid MediaAssetId { get; set; }
        public string UploadUrl { get; set; } = string.Empty;
        public string Method { get; set; } = "PUT";
        public Dictionary<string, string> Headers { get; set; } = new();
        public DateTime ExpiresAtUtc { get; set; }
    }
}
