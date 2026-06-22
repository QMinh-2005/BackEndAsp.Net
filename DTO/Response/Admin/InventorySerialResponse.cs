namespace MyOwnLearning.DTO.Response.Admin
{
    public class InventorySerialResponse
    {
        public int SerialId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ImportDate { get; set; }

        public int DetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public string VariantInfo { get; set; } = string.Empty;

    }
}
