using Dapr.Client;
using PosBackend.Models;
using PosBackend.Repositories;

namespace PosBackend.Services
{
    public class PosService
    {
        private readonly OrdersRepository _ordersRepo;
        private readonly ProductRepository _productRepo;
        private readonly DaprClient _daprClient;
        private const string StoreName = "statestore";

        public PosService(OrdersRepository ordersRepo,ProductRepository productRepo, DaprClient daprClient)
        {
            _ordersRepo = ordersRepo;
            _productRepo = productRepo;
            _daprClient = daprClient;
        }

        // Product operations
        public async Task<IEnumerable<Product>> GetProductsAsync() =>
            await _productRepo.GetAllAsync();

        public async Task<Product?> GetProductAsync(int id) =>
            await _productRepo.GetByIdAsync(id);

        public async Task<Product> CreateProductAsync(Product product)
        {
            product.Id = await _productRepo.CreateAsync(product);
            return product;
        }

        // Order operations with Dapr state management
        public async Task<Order?> GetOrderAsync(string orderId)
        {
            return await _daprClient.GetStateAsync<Order>(StoreName, orderId);
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            // Generate order number
            order.OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString()[..4]}";
            order.CreatedAt = DateTime.UtcNow;

            // Calculate total
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

            // Save to Dapr state store
            Console.WriteLine($"Save to Dapr state store {order.OrderNumber}");

            await _daprClient.SaveStateAsync(StoreName, order.OrderNumber, order);

            // Publish order created event
            Console.WriteLine($"Publish order created event {order.OrderNumber}");
            await _daprClient.PublishEventAsync("pubsub", "orderCreated", order);

            return order;
        }

        public async Task<bool> UpdateOrderStatusAsync(string orderNumber, string status)
        {
            var order = await GetOrderAsync(orderNumber);
            if (order == null) return false;

            order.Status = status;
            await _daprClient.SaveStateAsync(StoreName, orderNumber, order);

            // Publish status updated event
            await _daprClient.PublishEventAsync("pubsub", "orderStatusUpdated",
                new { OrderNumber = orderNumber, Status = status });

            return true;
        }

        public async Task<bool> SaveOrderAsync(Order order)
        {
            bool opstat = false;

            try
            {
                int result = await _ordersRepo.CreateAsync(order);

                if (result > 0)
                {
                    opstat = true;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving order: {ex.Message}");
                // Handle logging or rethrow as needed
            }


            return opstat;
        }

        public async Task<bool> UpdateOrderAsync(Order order)
        {
            bool opstat = false;

            await _ordersRepo.UpdateAsync(order);

            return opstat;
        }
    }
}
