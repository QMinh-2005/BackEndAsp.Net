namespace MyOwnLearning.DTO.Request.Admin
{
    public class AddDeliveryProofRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public int NewOrderStatusId { get; set; }
        public string? DeliveryProofImageUrl { get; set; }
        public string? DeliveryProofNote { get; set; }
    }

    public class ReviewReturnRequestRequest
    {
        public string? AdminNote { get; set; }
        public decimal? RefundAmount { get; set; }
    }

    public class MarkReturnRefundedRequest
    {
        public string? AdminNote { get; set; }
        public decimal? RefundAmount { get; set; }
    }
}
