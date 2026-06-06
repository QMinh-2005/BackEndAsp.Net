namespace MyOwnLearning.DTO.Response.Admin
{
    public class VariantSerialsResponse
    {
        public int DetailId { get; set; }

        // Chuỗi hiển thị ở Header (VD: "4U (80-84g) - G5 - Head Heavy")
        public string VariantInfo { get; set; } = null!;

        // Các con số thống kê cho các nút Badge
        public int TotalCount { get; set; }
        public int InStockCount { get; set; }
        public int SoldCount { get; set; }
        public int DefectiveCount { get; set; }
        public int ReservedCount { get; set; }

        // Danh sách chi tiết hiển thị ở Table
        public List<SerialNumberDto> Serials { get; set; } = new List<SerialNumberDto>();   
    }
    public class SerialNumberDto
    {
        public int SerialId { get; set; }
        public string SerialNumber { get; set; } = null!;
        public string Status { get; set; } = null!; // InStock, Sold, Reserved, Defective
        public DateTime ImportDate { get; set; }
    }
}
