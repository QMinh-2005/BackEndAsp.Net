using MyOwnLearning.DTO.Response.Customer;
using MyOwnLearning.Interfaces;
using MyOwnLearning.Models;

namespace MyOwnLearning.Service
{
    public interface ICartService
    {
        Task<CartResponse?> GetCartByUserIdAsync(int userId);
        Task<CartResponse> AddToCartAsync(int userId, int detailId, int quantity);
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
            var detail = await _productDetailRepository.GetByIdAsync(detailId);
            if (detail == null) throw new Exception("Product detail not found");
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
                throw new Exception($"Cannot add {quantity} items to cart. Only {detail.StockQuantity - currentQuantityInCart} items left in stock.");
            }
            if (finalQuantity <= 0)
            {
                if (existingItem != null)
                {
                    await _cartItemRepository.DeleteAsync(existingItem.CartItemId);
                }
            }
            else
            {
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
            }

            return await GetCartByUserIdAsync(userId);
        }
    }
}