namespace MyOwnLearning.DTO.Request.Customer
{
    public class ProductFilterRequest
    {
        public List<string>? CategorySlugs { get; set; }
        public List<string>? BrandSlugs { get; set; }
        public List<PriceRangeRequest>? PriceRanges { get; set; }
        public string? Keyword { get; set; }
        public bool? Voucher { get; set; }
        public bool? IsBestSeller { get; set; }
        public string? SortBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PriceRangeRequest
    {
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
    }
}
