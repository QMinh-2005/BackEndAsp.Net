using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using MyOwnLearning.DTO.Request.Admin;
using MyOwnLearning.DTO.Request.Customer;
using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Enums;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Service
{
    public interface IReturnRequestService
    {
        Task<List<ReturnReasonResponse>> GetReturnReasonsAsync();
        Task<List<DeliveryProofResponse>> GetDeliveryProofsAsync(int orderId, int userId, bool isAdmin);
        Task<DeliveryProofResponse> AddDeliveryProofAsync(int orderId, IFormFile file, string? note);
        Task<ReturnRequestResponse> CreateReturnRequestAsync(int userId, CreateReturnRequest request);
        Task<(List<ReturnRequestResponse> Requests, int TotalCount)> GetMyReturnRequestsAsync(int userId, int page, int pageSize);
        Task<ReturnRequestResponse> GetMyReturnRequestDetailAsync(int userId, int returnRequestId);
        Task<(List<ReturnRequestResponse> Requests, int TotalCount)> GetReturnRequestsForAdminAsync(
            string? status,
            int? orderId,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize);
        Task<ReturnRequestResponse> GetReturnRequestDetailForAdminAsync(int returnRequestId);
        Task<ReturnRequestResponse> ApproveReturnRequestAsync(int returnRequestId, ReviewReturnRequestRequest request);
        Task<ReturnRequestResponse> RejectReturnRequestAsync(int returnRequestId, ReviewReturnRequestRequest request);
        Task<ReturnRequestResponse> MarkRefundedAsync(int returnRequestId, MarkReturnRefundedRequest request);
    }

    public class ReturnRequestService : IReturnRequestService
    {
        private readonly IReturnRequestRepository _returnRequestRepository;
        private readonly IWebHostEnvironment _env;

        public ReturnRequestService(IReturnRequestRepository returnRequestRepository, IWebHostEnvironment env)
        {
            _returnRequestRepository = returnRequestRepository;
            _env = env;
        }

        public Task<List<ReturnReasonResponse>> GetReturnReasonsAsync()
        {
            return Task.FromResult(BuildReturnReasons());
        }

        public async Task<List<DeliveryProofResponse>> GetDeliveryProofsAsync(int orderId, int userId, bool isAdmin)
        {
            if (!isAdmin && !await _returnRequestRepository.IsOrderOwnedByUserAsync(orderId, userId))
                throw new UnauthorizedAccessException("Bạn không có quyền xem ảnh minh chứng giao hàng của đơn này.");

            var proofs = await _returnRequestRepository.GetDeliveryProofsByOrderIdAsync(orderId);
            return proofs.Select(MapDeliveryProof).ToList();
        }

        public async Task<DeliveryProofResponse> AddDeliveryProofAsync(int orderId, IFormFile file, string? note)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Vui lòng chọn file ảnh minh chứng giao hàng.");

            if (!await _returnRequestRepository.CanAddDeliveryProofAsync(orderId))
                throw new InvalidOperationException("Chỉ có thể thêm minh chứng giao hàng khi đơn ở trạng thái Đã giao hàng hoặc Hoàn tất.");

            // Lưu file vào wwwroot/uploads/delivery-proofs/
            var uploadDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "delivery-proofs");
            Directory.CreateDirectory(uploadDir);
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);
            await using (var stream = File.Create(filePath))
                await file.CopyToAsync(stream);

            var proof = await _returnRequestRepository.AddDeliveryProofAsync(new OrderDeliveryProof
            {
                OrderId   = orderId,
                ImageUrl  = $"/uploads/delivery-proofs/{fileName}",
                Note      = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            return MapDeliveryProof(proof);
        }

        public async Task<ReturnRequestResponse> CreateReturnRequestAsync(int userId, CreateReturnRequest request)
        {
            if (request == null)
                throw new ArgumentException("Dữ liệu yêu cầu trả hàng không hợp lệ.");

            request.MainReason = request.MainReason.Trim();
            request.DetailReason = request.DetailReason.Trim();

            ValidateReason(request.MainReason, request.DetailReason);

            var order = await _returnRequestRepository.GetReturnableOrderForUserAsync(request.OrderId, userId);
            if (order == null)
                throw new InvalidOperationException("Chỉ có thể gửi yêu cầu trả hàng/hoàn tiền khi đơn đã giao hàng hoặc hoàn tất.");

            if (await _returnRequestRepository.HasOpenReturnRequestAsync(request.OrderId, userId))
                throw new InvalidOperationException("Đơn hàng này đang có yêu cầu trả hàng/hoàn tiền chưa xử lý.");

            var imageUrls = NormalizeImageUrls(request.ImageUrls);
            var requiresEvidence = ReturnRequestConstants.RequiresCustomerEvidence(request.DetailReason);

            if (IsNotReceivedReason(request.DetailReason) &&
                await _returnRequestRepository.HasDeliveryProofsAsync(request.OrderId))
            {
                throw new InvalidOperationException("Đơn hàng đã có ảnh minh chứng giao hàng thành công, bạn không thể chọn lý do 'Chưa nhận được hàng'. Vui lòng chọn lý do phù hợp khác nếu hàng bị thiếu hoặc có vấn đề.");
            }

            if (requiresEvidence && !imageUrls.Any())
                throw new ArgumentException("Vui lòng gửi ít nhất một ảnh minh chứng cho lý do này.");

            if (requiresEvidence && string.IsNullOrWhiteSpace(request.CustomerDescription))
                throw new ArgumentException("Vui lòng nhập mô tả chi tiết cho lý do này.");

            var returnRequest = new ReturnRequest
            {
                OrderId = request.OrderId,
                UserId = userId,
                MainReason = request.MainReason,
                DetailReason = request.DetailReason,
                CustomerDescription = string.IsNullOrWhiteSpace(request.CustomerDescription)
                    ? null
                    : request.CustomerDescription.Trim(),
                Status = ReturnRequestConstants.StatusPending,
                RequestedAt = DateTime.UtcNow,
                OriginalOrderStatusId = order.OrderStatusId,
                ReturnRequestImages = imageUrls.Select(url => new ReturnRequestImage
                {
                    ImageUrl = url,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };

            var created = await _returnRequestRepository.CreateReturnRequestAsync(
                returnRequest,
                (int)OrderStatusEnum.DangYeuCauTraHangHoanTien);
            return MapReturnRequest(created);
        }

        public async Task<(List<ReturnRequestResponse> Requests, int TotalCount)> GetMyReturnRequestsAsync(int userId, int page, int pageSize)
        {
            NormalizePaging(ref page, ref pageSize);
            var (requests, totalCount) = await _returnRequestRepository.GetReturnRequestsByUserAsync(userId, page, pageSize);
            return (requests.Select(MapReturnRequest).ToList(), totalCount);
        }

        public async Task<ReturnRequestResponse> GetMyReturnRequestDetailAsync(int userId, int returnRequestId)
        {
            var request = await _returnRequestRepository.GetReturnRequestByIdForUserAsync(returnRequestId, userId);

            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu trả hàng/hoàn tiền.");

            return MapReturnRequest(request);
        }

        public async Task<(List<ReturnRequestResponse> Requests, int TotalCount)> GetReturnRequestsForAdminAsync(
            string? status,
            int? orderId,
            int? userId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            NormalizePaging(ref page, ref pageSize);
            ValidateDateRange(fromDate, toDate);

            if (!string.IsNullOrWhiteSpace(status) && !ReturnRequestConstants.IsValidStatus(status.Trim()))
                throw new ArgumentException("Trạng thái yêu cầu trả hàng/hoàn tiền không hợp lệ.");

            var (requests, totalCount) = await _returnRequestRepository.GetReturnRequestsForAdminAsync(
                status,
                orderId,
                userId,
                fromDate,
                toDate,
                page,
                pageSize);

            return (requests.Select(MapReturnRequest).ToList(), totalCount);
        }

        public async Task<ReturnRequestResponse> GetReturnRequestDetailForAdminAsync(int returnRequestId)
        {
            var request = await _returnRequestRepository.GetReturnRequestByIdAsync(returnRequestId);

            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu trả hàng/hoàn tiền.");

            return MapReturnRequest(request);
        }

        public async Task<ReturnRequestResponse> ApproveReturnRequestAsync(int returnRequestId, ReviewReturnRequestRequest request)
        {
            var returnRequest = await GetPendingReturnRequestAsync(returnRequestId);
            returnRequest.Status = ReturnRequestConstants.StatusApproved;
            returnRequest.ReviewedAt = DateTime.UtcNow;
            returnRequest.AdminNote = NormalizeOptionalText(request?.AdminNote);
            returnRequest.RefundAmount = request?.RefundAmount ?? returnRequest.Order.FinalAmount;

            var updated = await _returnRequestRepository.UpdateReturnRequestAsync(
                returnRequest,
                (int)OrderStatusEnum.DaChapThuanTraHangHoanTien);
            return MapReturnRequest(updated);
        }

        public async Task<ReturnRequestResponse> RejectReturnRequestAsync(int returnRequestId, ReviewReturnRequestRequest request)
        {
            var returnRequest = await GetPendingReturnRequestAsync(returnRequestId);

            if (string.IsNullOrWhiteSpace(request?.AdminNote))
                throw new ArgumentException("Vui lòng nhập lý do từ chối yêu cầu.");

            returnRequest.Status = ReturnRequestConstants.StatusRejected;
            returnRequest.ReviewedAt = DateTime.UtcNow;
            returnRequest.AdminNote = request.AdminNote.Trim();
            returnRequest.RefundAmount = null;

            var updated = await _returnRequestRepository.UpdateReturnRequestAsync(
                returnRequest,
                restoreOriginalOrderStatus: true);
            return MapReturnRequest(updated);
        }

        public async Task<ReturnRequestResponse> MarkRefundedAsync(int returnRequestId, MarkReturnRefundedRequest request)
        {
            var returnRequest = await _returnRequestRepository.GetReturnRequestByIdAsync(returnRequestId);

            if (returnRequest == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu trả hàng/hoàn tiền.");

            if (!string.Equals(returnRequest.Status, ReturnRequestConstants.StatusApproved, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ có thể đánh dấu đã hoàn tiền sau khi yêu cầu được chấp thuận.");

            returnRequest.Status = ReturnRequestConstants.StatusRefunded;
            returnRequest.ReviewedAt ??= DateTime.UtcNow;
            returnRequest.AdminNote = NormalizeOptionalText(request?.AdminNote) ?? returnRequest.AdminNote;
            returnRequest.RefundAmount = request?.RefundAmount ?? returnRequest.RefundAmount ?? returnRequest.Order.FinalAmount;

            var updated = await _returnRequestRepository.UpdateReturnRequestAsync(
                returnRequest,
                (int)OrderStatusEnum.DaHoanHangHoanTien);
            return MapReturnRequest(updated);
        }

        private async Task<ReturnRequest> GetPendingReturnRequestAsync(int returnRequestId)
        {
            var returnRequest = await _returnRequestRepository.GetReturnRequestByIdAsync(returnRequestId);

            if (returnRequest == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu trả hàng/hoàn tiền.");

            if (!string.Equals(returnRequest.Status, ReturnRequestConstants.StatusPending, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Chỉ có thể xử lý yêu cầu đang chờ duyệt.");

            return returnRequest;
        }

        private static void ValidateReason(string mainReason, string detailReason)
        {
            var reason = BuildReturnReasons().FirstOrDefault(r =>
                string.Equals(r.Code, mainReason, StringComparison.OrdinalIgnoreCase));

            if (reason == null)
                throw new ArgumentException("Lý do trả hàng/hoàn tiền không hợp lệ.");

            if (!reason.Details.Any(d => string.Equals(d.Code, detailReason, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Lý do chi tiết không hợp lệ.");
        }

        private static List<string> NormalizeImageUrls(List<string>? imageUrls)
        {
            return imageUrls?
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool IsNotReceivedReason(string detailReason)
        {
            return string.Equals(detailReason, ReturnRequestConstants.DetailNotReceived, StringComparison.OrdinalIgnoreCase);
        }

        private static void NormalizePaging(ref int page, ref int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
        }

        private static void ValidateDateRange(DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                throw new ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
        }

        private static DeliveryProofResponse MapDeliveryProof(OrderDeliveryProof proof)
        {
            return new DeliveryProofResponse
            {
                ProofId = proof.ProofId,
                OrderId = proof.OrderId,
                ImageUrl = proof.ImageUrl,
                Note = proof.Note,
                CreatedAt = proof.CreatedAt
            };
        }

        private static ReturnRequestResponse MapReturnRequest(ReturnRequest request)
        {
            return new ReturnRequestResponse
            {
                ReturnRequestId = request.ReturnRequestId,
                OrderId = request.OrderId,
                UserId = request.UserId,
                MainReason = request.MainReason,
                MainReasonName = GetMainReasonName(request.MainReason),
                DetailReason = request.DetailReason,
                DetailReasonName = GetDetailReasonName(request.DetailReason),
                CustomerDescription = request.CustomerDescription,
                Status = request.Status,
                StatusText = request.Status,
                RequestedAt = request.RequestedAt,
                ReviewedAt = request.ReviewedAt,
                AdminNote = request.AdminNote,
                RefundAmount = request.RefundAmount,
                OrderFinalAmount = request.Order?.FinalAmount,
                ReceiverName = request.Order?.ReceiverName,
                PhoneNumber = request.Order?.PhoneNumber,
                Images = request.ReturnRequestImages
                    .OrderBy(i => i.ImageId)
                    .Select(i => new ReturnRequestImageResponse
                    {
                        ImageId = i.ImageId,
                        ImageUrl = i.ImageUrl,
                        CreatedAt = i.CreatedAt
                    })
                    .ToList(),
                DeliveryProofs = request.Order?.OrderDeliveryProofs
                    .OrderBy(p => p.ProofId)
                    .Select(MapDeliveryProof)
                    .ToList() ?? new List<DeliveryProofResponse>()
            };
        }

        private static List<ReturnReasonResponse> BuildReturnReasons()
        {
            const string note = "Trường hợp yêu cầu trả hàng hoàn tiền của bạn được chấp nhận, Voucher có thể sẽ không hoàn lại.";

            return new List<ReturnReasonResponse>
            {
                new()
                {
                    Code = ReturnRequestConstants.MainDamagedOrDefective,
                    Name = ReturnRequestConstants.MainDamagedOrDefective,
                    Note = note,
                    Details = new List<ReturnDetailReasonResponse>
                    {
                        Detail(ReturnRequestConstants.DetailBroken, ReturnRequestConstants.DetailBroken, true, true, false),
                        Detail(ReturnRequestConstants.DetailDefective, ReturnRequestConstants.DetailDefective, true, true, false)
                    }
                },
                new()
                {
                    Code = ReturnRequestConstants.MainWrongOrNotAsDescribed,
                    Name = ReturnRequestConstants.MainWrongOrNotAsDescribed,
                    Note = note,
                    Details = new List<ReturnDetailReasonResponse>
                    {
                        Detail(ReturnRequestConstants.DetailWrongItem, ReturnRequestConstants.DetailWrongItem, true, true, false),
                        Detail(ReturnRequestConstants.DetailNotAsDescribed, ReturnRequestConstants.DetailNotAsDescribed, true, true, false),
                        Detail(ReturnRequestConstants.DetailUsedItem, ReturnRequestConstants.DetailUsedItem, true, true, false),
                        Detail(ReturnRequestConstants.DetailFakeItem, ReturnRequestConstants.DetailFakeItem, true, true, false)
                    }
                },
                new()
                {
                    Code = ReturnRequestConstants.MainNotReceivedOrMissing,
                    Name = ReturnRequestConstants.MainNotReceivedOrMissing,
                    Note = note,
                    Details = new List<ReturnDetailReasonResponse>
                    {
                        Detail(ReturnRequestConstants.DetailNotReceived, ReturnRequestConstants.DetailNotReceived, false, false, true),
                        Detail(ReturnRequestConstants.DetailMissingItem, ReturnRequestConstants.DetailMissingItem, true, true, false),
                        Detail(ReturnRequestConstants.DetailEmptyPackage, ReturnRequestConstants.DetailEmptyPackage, true, true, false)
                    }
                }
            };
        }

        private static ReturnDetailReasonResponse Detail(
            string code,
            string name,
            bool requiresImage,
            bool requiresDescription,
            bool shouldShowDeliveryProof)
        {
            return new ReturnDetailReasonResponse
            {
                Code = code,
                Name = name,
                RequiresImage = requiresImage,
                RequiresDescription = requiresDescription,
                ShouldShowDeliveryProof = shouldShowDeliveryProof
            };
        }

        private static string GetMainReasonName(string code)
        {
            return BuildReturnReasons().FirstOrDefault(r =>
                string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase))?.Name ?? code;
        }

        private static string GetDetailReasonName(string code)
        {
            return BuildReturnReasons()
                .SelectMany(r => r.Details)
                .FirstOrDefault(d => string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase))?.Name ?? code;
        }
    }
}
