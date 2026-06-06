namespace MyOwnLearning.DTO.Response.Admin
{
    public class AdminAuditLogResponse
    {
        public int AuditLogId { get; set; }
        public int? AdminId { get; set; }
        public string? AdminEmail { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public int? TargetId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
