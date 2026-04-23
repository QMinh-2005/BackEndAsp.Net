namespace MyOwnLearning.DTO.Request
{
    public class ChangeInfoRequest
    {
        public string? FullName { get; set; } = null!;

        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
