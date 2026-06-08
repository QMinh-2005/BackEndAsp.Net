namespace MyOwnLearning.DTO.Response.Customer
{
    public class ReturnReasonResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public List<ReturnDetailReasonResponse> Details { get; set; } = new();
    }

    public class ReturnDetailReasonResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool RequiresImage { get; set; }
        public bool RequiresDescription { get; set; }
        public bool ShouldShowDeliveryProof { get; set; }
    }

    public class DeliveryProofResponse
    {
        public int ProofId { get; set; }
        public int OrderId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReturnRequestImageResponse
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ReturnRequestResponse
    {
        public int ReturnRequestId { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string MainReason { get; set; } = string.Empty;
        public string MainReasonName { get; set; } = string.Empty;
        public string DetailReason { get; set; } = string.Empty;
        public string DetailReasonName { get; set; } = string.Empty;
        public string? CustomerDescription { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? AdminNote { get; set; }
        public decimal? RefundAmount { get; set; }
        public decimal? OrderFinalAmount { get; set; }
        public string? ReceiverName { get; set; }
        public string? PhoneNumber { get; set; }
        public List<ReturnRequestImageResponse> Images { get; set; } = new();
        public List<DeliveryProofResponse> DeliveryProofs { get; set; } = new();
    }
}
