namespace MyOwnLearning.DTO.Request.Customer
{
    public class ChangeInfoRequest
    {
        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? City { get; set; }

        public string? District { get; set; }

        public string? DetailedAddress { get; set; }
    }
}
