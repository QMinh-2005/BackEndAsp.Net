namespace MyOwnLearning.DTO.Request.Admin
{
    public class CreateAdminAuditLogRequest
    {
        public int? AdminId { get; set; }
        public string? AdminEmail { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public int? TargetId { get; set; }
        public string? Description { get; set; }
    }
}
