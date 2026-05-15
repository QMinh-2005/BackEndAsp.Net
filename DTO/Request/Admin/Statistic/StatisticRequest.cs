namespace MyOwnLearning.DTO.Request.Admin.Statistic
{
    public class StatisticRequest
    {
        /// <summary>
        /// Bộ lọc chung dùng cho tất cả các API thống kê.
        /// Tất cả các trường đều optional để cho phép query linh hoạt.
        /// </summary>
        public class StatisticsFilterRequest
        {
            /// <summary>Từ ngày (UTC). Ví dụ: 2024-01-01</summary>
            public DateTime? FromDate { get; set; }

            /// <summary>Đến ngày (UTC). Ví dụ: 2024-12-31</summary>
            public DateTime? ToDate { get; set; }

            /// <summary>Lọc theo danh mục sản phẩm (CategoryId)</summary>
            public int? CategoryId { get; set; }

            /// <summary>Lọc theo thương hiệu (BrandId)</summary>
            public int? BrandId { get; set; }
        }

        /// <summary>
        /// Request cho API doanh thu theo tháng trong năm.
        /// </summary>
        public class RevenueByMonthRequest
        {
            /// <summary>Năm cần xem thống kê. Mặc định: năm hiện tại.</summary>
            public int Year { get; set; } = DateTime.UtcNow.Year;
        }

        /// <summary>
        /// Request cho API doanh thu danh mục theo tháng.
        /// </summary>
        public class RevenueCategoryByMonthRequest
        {
            public int Year { get; set; } = DateTime.UtcNow.Year;
            public int? CategoryId { get; set; }
        }

        /// <summary>
        /// Request cho API top sản phẩm bán chạy (kết hợp filter + số lượng top).
        /// </summary>
        public class TopProductRequest : StatisticsFilterRequest
        {
            /// <summary>Số sản phẩm muốn lấy. Mặc định: 10.</summary>
            public int Top { get; set; } = 10;
        }
    }
}
