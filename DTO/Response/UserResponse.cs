namespace MyOwnLearning.DTO.Response
{
    public class UserResponse
    {
        public int UserId { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public string FullName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public DateTime? CreatedAt { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}
