namespace MyOwnLearning.DTO.Request
{
    public class CreateUserRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public IEnumerable<string?> Roles { get; set; }
    }
}
