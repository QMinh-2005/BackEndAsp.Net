namespace MyOwnLearning.DTO.Response.Admin.Statistic
{
    public class StatisticResponse
    {
        public class OverviewStatisticsResponse
        {
            public decimal TotalRevenue { get; set; }
            public int TotalOrders { get; set; }
            public int CancelledOrders { get; set; }
            public int TotalCustomers { get; set; }
            public decimal AverageOrderValue { get; set; }
        }

        // =====================================================
        // DOANH THU THEO DANH MỤC
        // =====================================================
        public class RevenueByCategory
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public decimal TotalRevenue { get; set; }
            public int TotalOrders { get; set; }
            public int TotalItems { get; set; }

            /// <summary>Tỷ trọng % so với tổng doanh thu (được tính ở Service layer)</summary>
            public double RevenueShare { get; set; }
        }

        // =====================================================
        // DOANH THU THEO THƯƠNG HIỆU
        // =====================================================
        public class RevenueByBrand
        {
            public int BrandId { get; set; }
            public string BrandName { get; set; } = string.Empty;
            public decimal TotalRevenue { get; set; }
            public int TotalOrders { get; set; }
            public int TotalItems { get; set; }

            /// <summary>Tỷ trọng % so với tổng doanh thu (được tính ở Service layer)</summary>
            public double RevenueShare { get; set; }
        }

        // =====================================================
        // DOANH THU THEO THÁNG
        // =====================================================
        public class RevenueByMonth
        {
            public int Month { get; set; }
            public int Year { get; set; }
            public decimal TotalRevenue { get; set; }
            public int TotalOrders { get; set; }

            /// <summary>Tăng trưởng so với tháng trước (%) — tính ở Service</summary>
            public double? GrowthRate { get; set; }
        }

        // =====================================================
        // TOP SẢN PHẨM BÁN CHẠY
        // =====================================================
        public class TopProductResponse
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public string BrandName { get; set; } = string.Empty;
            public int TotalSold { get; set; }
            public decimal TotalRevenue { get; set; }
        }

        // =====================================================
        // DOANH THU THEO DANH MỤC + THÁNG (CROSS REPORT)
        // =====================================================
        public class RevenueCategoryByMonth
        {
            public int Month { get; set; }
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public decimal TotalRevenue { get; set; }
        }

        public class OrderStatusStatisticResponse
        {
            public int StatusId { get; set; }
            public string StatusName { get; set; } = string.Empty;
            public int TotalOrders { get; set; }
            public decimal TotalRevenue { get; set; }
            public double OrderShare { get; set; }
        }

        public class PaymentMethodStatisticResponse
        {
            public string PaymentMethod { get; set; } = string.Empty;
            public int TotalOrders { get; set; }
            public decimal TotalRevenue { get; set; }
            public double RevenueShare { get; set; }
        }

        public class VoucherStatisticResponse
        {
            public int VoucherId { get; set; }
            public string VoucherCode { get; set; } = string.Empty;
            public int TotalOrders { get; set; }
            public int CurrentUsedCount { get; set; }
            public decimal TotalDiscount { get; set; }
            public decimal TotalRevenue { get; set; }
            public decimal AverageDiscountPerOrder { get; set; }
        }

        public class TopCustomerResponse
        {
            public int UserId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string? FullName { get; set; }
            public string? PhoneNumber { get; set; }
            public int TotalOrders { get; set; }
            public decimal TotalSpent { get; set; }
            public DateTime? LastOrderDate { get; set; }
        }

        // =====================================================
        // WRAPPER RESPONSE ĐẦY ĐỦ (trả về 1 lần cho dashboard)
        // =====================================================
        public class FullStatisticsReportResponse
        {
            public OverviewStatisticsResponse Overview { get; set; } = new();
            public List<RevenueByCategory> RevenueByCategories { get; set; } = new();
            public List<RevenueByBrand> RevenueByBrands { get; set; } = new();
            public List<RevenueByMonth> RevenueByMonths { get; set; } = new();
            public List<TopProductResponse> TopProducts { get; set; } = new();
            public List<RevenueCategoryByMonth> RevenueCategoryMonthly { get; set; } = new();
        }
    }
}
