using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.Service;
using static MyOwnLearning.DTO.Request.Admin.Statistic.StatisticRequest;

namespace MyOwnLearning.Controllers
{
    [ApiController]
    [Route("api/admin/statistic")]
    [Authorize(Roles = "Admin")]  // Chỉ Admin mới truy cập được
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticService _statisticService;

        public StatisticsController(IStatisticService statisticService)
        {
            _statisticService = statisticService;
        }

        // =====================================================
        // GET /api/admin/statistics/overview
        // KPI tổng quan: tổng doanh thu, đơn hàng, khách hàng, AOV
        // =====================================================
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] StatisticsFilterRequest filter)
        {
            try
            {
                var result = await _statisticService.GetOverviewAsync(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // =====================================================
        // GET /api/admin/statistics/revenue/category
        // Doanh thu theo danh mục sản phẩm
        // =====================================================
        [HttpGet("revenue/category")]
        public async Task<IActionResult> GetRevenueByCategory([FromQuery] StatisticsFilterRequest filter)
        {
            try
            {
                var result = await _statisticService.GetRevenueByCategoryAsync(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // =====================================================
        // GET /api/admin/statistics/revenue/brand
        // Doanh thu theo thương hiệu
        // =====================================================
        [HttpGet("revenue/brand")]
        public async Task<IActionResult> GetRevenueByBrand([FromQuery] StatisticsFilterRequest filter)
        {
            try
            {
                var result = await _statisticService.GetRevenueByBrandAsync(filter);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // =====================================================
        // GET /api/admin/statistics/revenue/monthly?year=2024
        // Doanh thu 12 tháng trong năm + tăng trưởng MoM
        // =====================================================
        [HttpGet("revenue/monthly")]
        public async Task<IActionResult> GetRevenueByMonth([FromQuery] int year = 0)
        {
            try
            {
                if (year == 0) year = DateTime.UtcNow.Year;

                if (year < 2000 || year > DateTime.UtcNow.Year + 1)
                    return BadRequest(new { success = false, message = "Năm không hợp lệ." });

                var result = await _statisticService.GetRevenueByMonthAsync(year);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // =====================================================
        // GET /api/admin/statistics/products/top?top=10
        // Top sản phẩm bán chạy nhất (kết hợp lọc)
        // =====================================================
        [HttpGet("products/top")]
        public async Task<IActionResult> GetTopProducts([FromQuery] TopProductRequest request)
        {
            try
            {
                var result = await _statisticService.GetTopSellingProductsAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // =====================================================
        // GET /api/admin/statistics/revenue/category-monthly?year=2024&categoryId=1
        // Doanh thu từng danh mục theo từng tháng (cho biểu đồ đa đường)
        // =====================================================
        [HttpGet("revenue/category-monthly")]
        public async Task<IActionResult> GetRevenueCategoryByMonth([FromQuery] RevenueCategoryByMonthRequest request)
        {
            try
            {
                var result = await _statisticService.GetRevenueCategoryByMonthAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // =====================================================
        // GET /api/admin/statistics/full-report?year=2024
        // Full report: gom tất cả cho Dashboard load 1 lần (dùng Task.WhenAll)
        // =====================================================
        [HttpGet("full-report")]
        public async Task<IActionResult> GetFullReport(
            [FromQuery] StatisticsFilterRequest filter,
            [FromQuery] int year = 0)
        {
            try
            {
                if (year == 0) year = DateTime.UtcNow.Year;

                var result = await _statisticService.GetFullReportAsync(filter, year);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("orders/status")]
        public async Task<IActionResult> GetOrderStatusStatistics(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var result = await _statisticService.GetOrderStatusStatisticsAsync(fromDate, toDate);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("revenue/payment-method")]
        public async Task<IActionResult> GetRevenueByPaymentMethod(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var result = await _statisticService.GetRevenueByPaymentMethodAsync(fromDate, toDate);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("vouchers/effectiveness")]
        public async Task<IActionResult> GetVoucherEffectiveness(DateTime? fromDate, DateTime? toDate, int top = 10)
        {
            try
            {
                var result = await _statisticService.GetVoucherEffectivenessAsync(fromDate, toDate, top);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("customers/top")]
        public async Task<IActionResult> GetTopCustomers(DateTime? fromDate, DateTime? toDate, int top = 10)
        {
            try
            {
                var result = await _statisticService.GetTopCustomersAsync(fromDate, toDate, top);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
