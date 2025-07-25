using Dapper;
using PosBackend.Models;
using System.Data;

namespace PosBackend.Repositories
{
    public class OrdersRepository : IRepository<Order>
    {
        private readonly IDbConnection _db;

        public OrdersRepository(IDbConnection db)
        {
            _db = db;

            // Ensure connection is open
            if (_db.State != ConnectionState.Open)
            {
                _db.Open();
            }
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            var orders = await _db.QueryAsync<Order>("SELECT * FROM orders");

            // Load order items for each order
            foreach (var order in orders)
            {
                order.Items = (await GetOrderItemsAsync(order.Id)).ToList();
            }

            return orders;
        }

        public async Task<Order?> GetByIdAsync(int id)
        {
            var order = await _db.QueryFirstOrDefaultAsync<Order>(
                "SELECT * FROM orders WHERE id = @Id", new { Id = id });

            if (order != null)
            {
                order.Items = (await GetOrderItemsAsync(order.Id)).ToList();
            }

            return order;
        }

        public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
        {
            var order = await _db.QueryFirstOrDefaultAsync<Order>(
                "SELECT * FROM orders WHERE order_number = @OrderNumber",
                new { OrderNumber = orderNumber });

            if (order != null)
            {
                order.Items = (await GetOrderItemsAsync(order.Id)).ToList();
            }

            return order;
        }

        public async Task<int> CreateAsync(Order order)
        {
            using var transaction = _db.BeginTransaction();

            try
            {
                // Insert order
                var sql = @"INSERT INTO orders 
                        (order_number, customer_id, total_amount, status) 
                        VALUES (@OrderNumber, @CustomerId, @TotalAmount, @Status)
                        RETURNING id";

                order.Id = await _db.ExecuteScalarAsync<int>(sql, order, transaction);

                // Insert order items
                foreach (var item in order.Items)
                {
                    item.OrderId = order.Id;
                    await CreateOrderItemAsync(item, transaction);
                }

                transaction.Commit();
                return order.Id;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Order order)
        {
            var sql = @"UPDATE orders SET 
                    customer_id = @CustomerId,
                    total_amount = @TotalAmount,
                    status = @Status
                WHERE id = @Id";

            var affectedRows = await _db.ExecuteAsync(sql, order);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var transaction = _db.BeginTransaction();

            try
            {
                // Delete order items first
                await _db.ExecuteAsync(
                    "DELETE FROM order_items WHERE order_id = @OrderId",
                    new { OrderId = id },
                    transaction);

                // Then delete the order
                var affectedRows = await _db.ExecuteAsync(
                    "DELETE FROM orders WHERE id = @Id",
                    new { Id = id },
                    transaction);

                transaction.Commit();
                return affectedRows > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var affectedRows = await _db.ExecuteAsync(
                "UPDATE orders SET status = @Status WHERE id = @OrderId",
                new { OrderId = orderId, Status = status });

            return affectedRows > 0;
        }

        private async Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            var sql = @"SELECT oi.*, p.* 
                    FROM order_items oi
                    LEFT JOIN products p ON oi.product_id = p.id
                    WHERE oi.order_id = @OrderId";

            return await _db.QueryAsync<OrderItem, Product, OrderItem>(sql,
                (orderItem, product) =>
                {
                    orderItem.Product = product;
                    return orderItem;
                },
                new { OrderId = orderId },
                splitOn: "id");
        }

        private async Task CreateOrderItemAsync(OrderItem item, IDbTransaction transaction)
        {
            var sql = @"INSERT INTO order_items 
                    (order_id, product_id, quantity, unit_price, total_price)
                    VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, @TotalPrice)";

            await _db.ExecuteAsync(sql, item, transaction);
        }
    }
}
