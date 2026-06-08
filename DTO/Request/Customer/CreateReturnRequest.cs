namespace MyOwnLearning.DTO.Request.Customer
{
    public class CreateReturnRequest
    {
        public int OrderId { get; set; }
        public string MainReason { get; set; } = string.Empty;
        public string DetailReason { get; set; } = string.Empty;
        public string? CustomerDescription { get; set; }
        public List<string>? ImageUrls { get; set; }
    }
}
