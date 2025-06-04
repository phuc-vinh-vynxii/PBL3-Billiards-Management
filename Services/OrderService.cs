using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public class OrderService : IOrderService
    {
        private readonly BilliardsDbContext _context;

        public OrderService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<List<dynamic>> GetCurrentOrderAsync(int sessionId)
        {
            var orderDetails = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order != null && od.Order.SessionId == sessionId && od.Order.Status == "PENDING")
                .Select(od => new {
                    orderDetailId = od.OrderDetailId,
                    productName = od.Product.ProductName,
                    quantity = od.Quantity,
                    unitPrice = od.UnitPrice
                })
                .ToListAsync();
                
            return orderDetails.Cast<dynamic>().ToList();
        }

        public async Task<(bool Success, string Message, int? RemainingQuantity)> AddOrderAsync(int tableId, int productId, int quantity, int employeeId)
        {
            try
            {
                // Validate input
                if (quantity <= 0)
                    return (false, "Số lượng phải lớn hơn 0", null);

                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.TableId == tableId && s.EndTime == null);

                if (session == null)
                    return (false, "Không tìm thấy phiên hoạt động cho bàn này", null);

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return (false, "Không tìm thấy sản phẩm", null);

                if (product.Status != "AVAILABLE")
                    return (false, "Sản phẩm hiện không có sẵn", null);

                var currentStock = product.Quantity ?? 0;
                if (currentStock < quantity)
                    return (false, $"Số lượng trong kho không đủ. Chỉ còn {currentStock} {product.ProductName}", null);

                // Find existing order for this session (prioritize by SessionId)
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.SessionId == session.SessionId && o.Status == "PENDING");

                // If no order for this session, create a new one
                if (order == null)
                {
                    order = new Order
                    {
                        SessionId = session.SessionId,
                        EmployeeId = employeeId,
                        Status = "PENDING",
                        CreatedAt = DateTime.Now
                    };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                }

                // Check if the product already exists in the order
                var existingOrderDetail = await _context.OrderDetails
                    .FirstOrDefaultAsync(od => od.OrderId == order.OrderId && od.ProductId == productId);

                if (existingOrderDetail != null)
                {
                    // Product already exists, just update quantity
                    existingOrderDetail.Quantity = (existingOrderDetail.Quantity ?? 0) + quantity;
                    _context.OrderDetails.Update(existingOrderDetail);
                }
                else
                {
                    // Create new order detail
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price ?? 0
                    };
                    
                    _context.OrderDetails.Add(orderDetail);
                }

                // Update product quantity
                product.Quantity = currentStock - quantity;
                if (product.Quantity <= 0)
                {
                    product.Status = "OUT_OF_STOCK";
                    product.Quantity = 0;
                }
                
                _context.Products.Update(product);

                // Save all changes
                await _context.SaveChangesAsync();

                return (true, $"Đã thêm {quantity} {product.ProductName} vào đơn hàng", product.Quantity);
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra: {ex.Message}", null);
            }
        }

        public async Task<bool> RemoveOrderDetailAsync(int orderDetailId)
        {
            try
            {
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Product)
                    .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

                if (orderDetail == null)
                    return false;

                if (orderDetail.Product != null)
                {
                    orderDetail.Product.Quantity = (orderDetail.Product.Quantity ?? 0) + orderDetail.Quantity;
                    if (orderDetail.Product.Quantity > 0)
                        orderDetail.Product.Status = "AVAILABLE";
                }

                _context.OrderDetails.Remove(orderDetail);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string Message, bool Removed, int NewQuantity, decimal Subtotal)> UpdateOrderDetailQuantityAsync(int orderDetailId, int changeAmount)
        {
            try
            {
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Product)
                    .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

                if (orderDetail == null)
                    return (false, "Không tìm thấy chi tiết đơn hàng", false, 0, 0);

                int newQuantity = (orderDetail.Quantity ?? 0) + changeAmount;

                // Xử lý trường hợp giảm số lượng
                if (changeAmount < 0)
                {
                    // Cập nhật lại số lượng trong kho (trả lại số lượng sản phẩm)
                    if (orderDetail.Product != null)
                    {
                        orderDetail.Product.Quantity = (orderDetail.Product.Quantity ?? 0) - changeAmount; // Trừ với số âm = cộng
                        if (orderDetail.Product.Quantity > 0)
                            orderDetail.Product.Status = "AVAILABLE";
                    }
                    
                    // Nếu số lượng mới là 0 hoặc âm, xóa mục này khỏi đơn hàng
                    if (newQuantity <= 0)
                    {
                        _context.OrderDetails.Remove(orderDetail);
                        await _context.SaveChangesAsync();
                        return (true, "Đã xóa sản phẩm khỏi đơn hàng", true, 0, 0);
                    }
                }
                else if (changeAmount > 0) // Xử lý trường hợp tăng số lượng
                {
                    // Kiểm tra xem còn đủ hàng trong kho không
                    if (orderDetail.Product == null || orderDetail.Product.Quantity == null)
                    {
                        return (false, "Không thể xác định số lượng tồn kho", false, 0, 0);
                    }
                    
                    if (orderDetail.Product.Quantity < changeAmount)
                    {
                        return (false, $"Số lượng trong kho không đủ. Chỉ còn {orderDetail.Product.Quantity} {orderDetail.Product.ProductName}", false, 0, 0);
                    }
                    
                    // Cập nhật lại số lượng trong kho (giảm số lượng)
                    orderDetail.Product.Quantity = orderDetail.Product.Quantity - changeAmount;
                    if (orderDetail.Product.Quantity <= 0)
                        orderDetail.Product.Status = "OUT_OF_STOCK";
                }
                
                // Cập nhật số lượng mới cho chi tiết đơn hàng
                orderDetail.Quantity = newQuantity;
                await _context.SaveChangesAsync();
                
                var subtotal = newQuantity * (orderDetail.UnitPrice ?? 0);
                return (true, "Cập nhật thành công", false, newQuantity, subtotal);
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra: {ex.Message}", false, 0, 0);
            }
        }

        public async Task<List<dynamic>> GetOrderDetailInfoAsync()
        {
            // Get all order details with their product information
            var orderDetails = await _context.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.Order.Status == "PENDING")
                .Select(od => new
                {
                    orderDetailId = od.OrderDetailId,
                    productId = od.ProductId,
                    productName = od.Product.ProductName,
                    currentQuantity = od.Quantity,
                    availableQuantity = od.Product.Quantity,
                    unitPrice = od.UnitPrice
                })
                .ToListAsync();

            return orderDetails.Cast<dynamic>().ToList();
        }

        public async Task<bool> CleanupOrphanedOrdersAsync()
        {
            try
            {
                var orphanedOrders = await _context.Orders
                    .Where(o => o.SessionId == null || !_context.Sessions.Any(s => s.SessionId == o.SessionId))
                    .ToListAsync();

                _context.Orders.RemoveRange(orphanedOrders);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 