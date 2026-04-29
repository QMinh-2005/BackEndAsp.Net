namespace MyOwnLearning.DTO.Request.Customer
{
    public class AddToCartRequest
    {
        public int DetailId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
