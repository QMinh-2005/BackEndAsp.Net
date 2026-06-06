using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyOwnLearning.Service;

namespace MyOwnLearning.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockVariants([FromQuery] int threshold = 5)
        {
            try
            {
                var result = await _inventoryService.GetLowStockVariantsAsync(threshold);
                return Ok(new { Items = result, TotalCount = result.Count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("serials/by-status")]
        public async Task<IActionResult> GetSerialsByStatus(
            [FromQuery] string status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _inventoryService.GetSerialsByStatusAsync(status, page, pageSize);
                return Ok(new { Items = result, Page = page, PageSize = pageSize });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("serials/{serialId:int}/mark-defective")]
        public async Task<IActionResult> MarkSerialAsDefective(int serialId)
        {
            var success = await _inventoryService.MarkSerialAsDefectiveAsync(serialId);
            if (!success)
                return BadRequest(new { Message = "Không thể chuyển serial này sang trạng thái lỗi." });

            return Ok(new { Message = "Đã chuyển serial sang trạng thái lỗi." });
        }

        [HttpPut("serials/{serialId:int}/mark-in-stock")]
        public async Task<IActionResult> MarkSerialAsInStock(int serialId)
        {
            var success = await _inventoryService.MarkSerialAsInStockAsync(serialId);
            if (!success)
                return BadRequest(new { Message = "Không thể chuyển serial này về trạng thái còn hàng." });

            return Ok(new { Message = "Đã chuyển serial về trạng thái còn hàng." });
        }
    }
}
