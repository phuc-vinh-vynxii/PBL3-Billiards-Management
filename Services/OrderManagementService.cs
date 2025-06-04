using Microsoft.EntityFrameworkCore;
using BilliardsManagement.Models.Entities;

namespace BilliardsManagement.Services
{
    public class OrderManagementService : IOrderManagementService
    {
        private readonly BilliardsDbContext _context;

        public OrderManagementService(BilliardsDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> AddOrderAsync(int tableId, int productId, int quantity, int employeeId)
        {
            try
            {
                // Validate input
                if (quantity <= 0)
                    return (false, "Số lượng phải lớn hơn 0");

                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.TableId == tableId && s.EndTime == null);

                if (session == null)
                    return (false, "Không tìm thấy phiên hoạt động cho bàn này");

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return (false, "Không tìm thấy sản phẩm");

                if (product.Status != "AVAILABLE")
                    return (false, "Sản phẩm hiện không có sẵn");

                var currentStock = product.Quantity ?? 0;
                if (currentStock < quantity)
                    return (false, $"Số lượng trong kho không đủ. Chỉ còn {currentStock} {product.ProductName}");

                // Get or create order for this session
                var order = await GetOrCreateOrderForSessionAsync(session.SessionId, employeeId);
                if (order == null)
                    return (false, "Không thể tạo đơn hàng");

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
                await _context.SaveChangesAsync();

                return (true, $"Đã thêm {quantity} {product.ProductName} vào đơn hàng");
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                return (false, $"Lỗi cơ sở dữ liệu: {innerMessage}");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RemoveOrderDetailAsync(int orderDetailId)
        {
            try
            {
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Product)
                    .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

                if (orderDetail == null)
                    return (false, "Không tìm thấy chi tiết đơn hàng");

                // Restore product quantity
                if (orderDetail.Product != null)
                {
                    orderDetail.Product.Quantity = (orderDetail.Product.Quantity ?? 0) + (orderDetail.Quantity ?? 0);
                    if (orderDetail.Product.Quantity > 0)
                        orderDetail.Product.Status = "AVAILABLE";
                }

                _context.OrderDetails.Remove(orderDetail);
                await _context.SaveChangesAsync();

                return (true, "Đã xóa sản phẩm khỏi đơn hàng");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateOrderDetailQuantityAsync(int orderDetailId, int changeAmount)
        {
            try
            {
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Product)
                    .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

                if (orderDetail == null)
                    return (false, "Không tìm thấy chi tiết đơn hàng");

                int currentQuantity = orderDetail.Quantity ?? 0;
                int newQuantity = currentQuantity + changeAmount;

                // Handle quantity decrease
                if (changeAmount < 0)
                {
                    // Restore product quantity (subtract negative change = add)
                    if (orderDetail.Product != null)
                    {
                        orderDetail.Product.Quantity = (orderDetail.Product.Quantity ?? 0) - changeAmount;
                        if (orderDetail.Product.Quantity > 0)
                            orderDetail.Product.Status = "AVAILABLE";
                    }

                    // If new quantity is 0 or negative, remove the order detail
                    if (newQuantity <= 0)
                    {
                        _context.OrderDetails.Remove(orderDetail);
                        await _context.SaveChangesAsync();
                        return (true, "Đã xóa sản phẩm khỏi đơn hàng");
                    }
                }
                else if (changeAmount > 0)
                {
                    // Handle quantity increase - check stock availability
                    if (orderDetail.Product != null)
                    {
                        var availableStock = orderDetail.Product.Quantity ?? 0;
                        if (availableStock < changeAmount)
                            return (false, $"Số lượng trong kho không đủ. Chỉ còn {availableStock} {orderDetail.Product.ProductName}");

                        // Reduce product quantity
                        orderDetail.Product.Quantity = availableStock - changeAmount;
                        if (orderDetail.Product.Quantity <= 0)
                        {
                            orderDetail.Product.Status = "OUT_OF_STOCK";
                            orderDetail.Product.Quantity = 0;
                        }
                    }
                }

                // Update order detail quantity
                orderDetail.Quantity = newQuantity;
                _context.OrderDetails.Update(orderDetail);
                await _context.SaveChangesAsync();

                return (true, "Đã cập nhật số lượng sản phẩm");
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra: {ex.Message}");
            }
        }

        public async Task<OrderDetail?> GetOrderDetailAsync(int orderDetailId)
        {
            return await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);
        }

        public async Task<List<OrderDetail>> GetOrderDetailsBySessionAsync(int sessionId)
        {
            return await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order != null && od.Order.SessionId == sessionId && od.Order.Status == "PENDING")
                .ToListAsync();
        }

        public async Task<Order?> GetOrCreateOrderForSessionAsync(int sessionId, int employeeId)
        {
            // Find existing order for this session
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.SessionId == sessionId && o.Status == "PENDING");

            // If no order for this session, create a new one
            if (order == null)
            {
                order = new Order
                {
                    SessionId = sessionId,
                    EmployeeId = employeeId,
                    Status = "PENDING",
                    CreatedAt = DateTime.Now
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
            }

            return order;
        }
    }
} 