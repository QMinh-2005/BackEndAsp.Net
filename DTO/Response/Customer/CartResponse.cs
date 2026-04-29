namespace MyOwnLearning.DTO.Response.Customer
{
    public class CartResponse
    {
        public List<CartItemResponse> Items { get; set; } = new List<CartItemResponse>();
        public decimal TotalAmount => Items.Sum(i => i.SubTotal);
        public int TotalQuantity => Items.Sum(i => i.Quantity);
    }
    public class CartItemResponse
    {
        public int DetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string VariantInfo { get; set; } // Ví dụ: "4U - G5"
        public string ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal => UnitPrice * Quantity;
    }
}
