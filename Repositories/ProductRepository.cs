using Dapper;
using PosBackend.Models;
using System.Data;

namespace PosBackend.Repositories
{
    public class ProductRepository : IRepository<Product>
    {
        private readonly IDbConnection _db;

        public ProductRepository(IDbConnection db)
        {
            _db = db;

            // Ensure connection is open
            if (_db.State != ConnectionState.Open)
            {
                _db.Open();
            }
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _db.QueryAsync<Product>("SELECT * FROM products");
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _db.QueryFirstOrDefaultAsync<Product>(
                "SELECT * FROM products WHERE id = @Id", new { Id = id });
        }

        public async Task<int> CreateAsync(Product product)
        {
            var sql = @"INSERT INTO products (sku, name, description, price, stock_quantity) 
                    VALUES (@Sku, @Name, @Description, @Price, @StockQuantity)
                    RETURNING id";
            return await _db.ExecuteScalarAsync<int>(sql, product);
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            var affectedRows = await _db.ExecuteAsync(
                @"UPDATE products SET 
                sku = @Sku, 
                name = @Name, 
                description = @Description, 
                price = @Price, 
                stock_quantity = @StockQuantity 
            WHERE id = @Id", product);
            return affectedRows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var affectedRows = await _db.ExecuteAsync(
                "DELETE FROM products WHERE id = @Id", new { Id = id });
            return affectedRows > 0;
        }
    }
}
