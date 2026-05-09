namespace MyOwnLearning.DTO.Request.Admin
{
    public class UpdateProductDetailRequest
    {
        public string? WeightClass { get; set; }
        public string? GripSize { get; set; }
        public string? BalancePoint { get; set; }
        public string? Stiffness { get; set; }
        public int? MaxTension { get; set; }
        public decimal? Price { get; set; }
        public int StockQuantity { get; set; }
    }
}
