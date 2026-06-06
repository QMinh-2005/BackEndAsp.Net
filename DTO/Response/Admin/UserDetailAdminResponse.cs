namespace MyOwnLearning.DTO.Response.Admin
{
    public class UserDetailAdminResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? DetailedAddress { get; set; }
        public List<string> Roles { get; set; } = new();
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }
}
