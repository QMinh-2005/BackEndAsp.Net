namespace MyOwnLearning.DTO.Request
{
    public class CustomerRegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
