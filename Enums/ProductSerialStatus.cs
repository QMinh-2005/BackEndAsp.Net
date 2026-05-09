namespace MyOwnLearning.Enums
{
    public static class ProductSerialStatus
    {
        public const string InStock = "InStock";
        public const string Sold = "Sold";
        public const string Reserved = "Reserved";
        public const string Defective = "Defective";
        public static string Normalized(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new Exception("Trạng thái không được để trống.");
            }
            return input.Trim().ToLower() switch
            {
                "instock" => InStock,
                "sold" => Sold,
                "reserved" => Reserved,
                "defective" => Defective,
                _ => throw new Exception($"Trạng thái '{input}' không hợp lệ.")
            };
        }
    }
}
