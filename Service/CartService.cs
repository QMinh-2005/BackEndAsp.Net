using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Service
{
    public interface ICartService
    {
        Task<CartResponse?> GetCartByUserIdAsync(int userId);
        Task<CartResponse> AddToCartAsync(int userId, int detailId, int quantity);

        // Tìm trực tiếp item bằng CartItemId (Nhanh hơn tìm theo DetailId)
        Task<CartResponse> UpdateCartAsync(int userId, int cartItemId, int quantity);

        // 2. Hàm Xóa hẳn sản phẩm (Dùng cho nút Thùng rác)
        Task<CartResponse> DeleteCartAsync(int userId, int cartItemId);
    }
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductDetailRepository _productDetailRepository;
        private readonly ICartItemRepository _cartItemRepository;
        public CartService(ICartRepository cartRepository, IProductDetailRepository productDetailRepository, ICartItemRepository cartItemRepository)
        {
            _cartRepository = cartRepository;
            _productDetailRepository = productDetailRepository;
            _cartItemRepository = cartItemRepository;
        }

        public async Task<CartResponse?> GetCartByUserIdAsync(int userId)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null) return null;

            var cartResponse = new CartResponse
            {
                Items = cart.CartItems.Select(ci => new CartItemResponse
                {
                    CartItemId = ci.CartItemId,
                    DetailId = ci.DetailId,// giữ lại để khi click + - thì FE sẽ gửi DetailId để biết cập nhật số lượng cho item nào
                    ProductId = ci.Detail?.ProductId ?? 0,
                    ProductName = ci.Detail?.Product?.ProductName ?? string.Empty,
                    VariantInfo = $"{ci.Detail.WeightClass ?? ""} {ci.Detail.GripSize ?? ""}".Trim(),
                    ImageUrl = ci.Detail?.Product?.MainImageUrl ?? string.Empty,
                    UnitPrice = ci.Detail?.Price ?? 0,
                    Quantity = ci.Quantity
                }).ToList()
            };

            return cartResponse;
        }
        public async Task<CartResponse> AddToCartAsync(int userId, int detailId, int quantity)
        {
            // Hàm AddToCart chỉ chấp nhận thêm số lượng > 0.
            if (quantity <= 0) throw new Exception("Số lượng thêm vào phải lớn hơn 0");

            //Kiểm tra tồn tại của ProductDetail
            var detail = await _productDetailRepository.GetByIdAsync(detailId);
            if (detail == null) throw new KeyNotFoundException("Không tìm thấy thông tin sản phẩm");
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                };
                await _cartRepository.AddAsync(cart);
            }
            var existingItem = cart.CartItems.FirstOrDefault(cd => cd.DetailId == detailId);
            var currentQuantityInCart = existingItem != null ? existingItem.Quantity : 0;
            var finalQuantity = currentQuantityInCart + quantity;
            if (finalQuantity > detail.StockQuantity)
            {
                throw new InvalidOperationException($"Không đủ hàng trong kho. Bạn đang có {currentQuantityInCart} trong giỏ hàng, và chỉ có {detail.StockQuantity} sản phẩm này trong kho.");
            }

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.AddedDate = DateTime.UtcNow;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    CartId = cart.CartId,
                    DetailId = detailId,
                    Quantity = quantity,
                    AddedDate = DateTime.UtcNow
                });
            }
            await _cartRepository.UpdateAsync(cart);
            return await GetCartByUserIdAsync(userId);
        }

        public async Task<CartResponse> UpdateCartAsync(int userId, int cartItemId, int quantity)
        {
            var cartItem = await _cartItemRepository.GetByIdAsync(cartItemId);
            if (cartItem == null)
            {
                throw new KeyNotFoundException("Sản phẩm không tồn tại trong giỏ hàng");
            }
            var detail = await _productDetailRepository.GetByIdAsync(cartItem.DetailId);
            if (quantity > detail.StockQuantity)
            {
                throw new InvalidOperationException($"Không đủ hàng trong kho. Tối đa bạn có thể đặt là {detail.StockQuantity} sản phẩm này.");
            }

            if (quantity <= 0)
            {
                await _cartItemRepository.DeleteAsync(cartItemId);
            }
            else
            {
                cartItem.Quantity = quantity;
                cartItem.AddedDate = DateTime.UtcNow;
                await _cartItemRepository.UpdateAsync(cartItem);
            }
            return await GetCartByUserIdAsync(userId);
        }
        public async Task<CartResponse> DeleteCartAsync(int userId, int cartItemId)
        {
            var cartItem = await _cartItemRepository.GetByIdAsync(cartItemId);
            if (cartItem == null)
            {
                throw new KeyNotFoundException("Sản phẩm không tồn tại trong giỏ hàng");
            }
            await _cartItemRepository.DeleteAsync(cartItemId);
            return await GetCartByUserIdAsync(userId);
        }
    }
}