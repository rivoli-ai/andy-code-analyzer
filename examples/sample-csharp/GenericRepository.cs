using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SampleApp.Data
{
    /// <summary>
    /// Generic repository interface for data access.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TKey">The type of the entity's primary key</typeparam>
    public interface IRepository<T, TKey> where T : class
    {
        Task<T?> GetByIdAsync(TKey id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(TKey id);
        Task<int> CountAsync();
        Task<bool> ExistsAsync(TKey id);
    }

    /// <summary>
    /// In-memory implementation of the repository pattern.
    /// </summary>
    public class InMemoryRepository<T, TKey> : IRepository<T, TKey> where T : class, IEntity<TKey> where TKey : notnull
    {
        protected readonly Dictionary<TKey, T> _data = new();
        protected readonly object _lock = new();

        public virtual async Task<T?> GetByIdAsync(TKey id)
        {
            await Task.Delay(5); // Simulate DB access
            lock (_lock)
            {
                _data.TryGetValue(id, out var entity);
                return entity;
            }
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            await Task.Delay(10); // Simulate DB access
            lock (_lock)
            {
                return _data.Values.ToList();
            }
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            await Task.Delay(15); // Simulate DB access
            var compiled = predicate.Compile();
            lock (_lock)
            {
                return _data.Values.Where(compiled).ToList();
            }
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await Task.Delay(20); // Simulate DB access
            lock (_lock)
            {
                if (_data.ContainsKey(entity.Id))
                    throw new InvalidOperationException($"Entity with ID {entity.Id} already exists");
                
                _data[entity.Id] = entity;
                return entity;
            }
        }

        public virtual async Task UpdateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await Task.Delay(15); // Simulate DB access
            lock (_lock)
            {
                if (!_data.ContainsKey(entity.Id))
                    throw new InvalidOperationException($"Entity with ID {entity.Id} not found");
                
                _data[entity.Id] = entity;
            }
        }

        public virtual async Task DeleteAsync(TKey id)
        {
            await Task.Delay(10); // Simulate DB access
            lock (_lock)
            {
                if (!_data.Remove(id))
                    throw new InvalidOperationException($"Entity with ID {id} not found");
            }
        }

        public virtual async Task<int> CountAsync()
        {
            await Task.Delay(5); // Simulate DB access
            lock (_lock)
            {
                return _data.Count;
            }
        }

        public virtual async Task<bool> ExistsAsync(TKey id)
        {
            await Task.Delay(5); // Simulate DB access
            lock (_lock)
            {
                return _data.ContainsKey(id);
            }
        }
    }

    /// <summary>
    /// Base interface for entities with an ID.
    /// </summary>
    public interface IEntity<TKey>
    {
        TKey Id { get; set; }
    }

    /// <summary>
    /// Unit of Work pattern for managing transactions.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
        void BeginTransaction();
        void Commit();
        void Rollback();
    }

    /// <summary>
    /// Simple implementation of Unit of Work.
    /// </summary>
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        private bool _isInTransaction;
        private readonly List<Action> _pendingOperations = new();

        public async Task<int> SaveChangesAsync()
        {
            await Task.Delay(10); // Simulate DB operation
            var count = _pendingOperations.Count;
            
            if (_isInTransaction)
            {
                // Operations are deferred until commit
                return 0;
            }

            // Execute all pending operations
            foreach (var operation in _pendingOperations)
            {
                operation();
            }
            _pendingOperations.Clear();
            
            return count;
        }

        public void BeginTransaction()
        {
            if (_isInTransaction)
                throw new InvalidOperationException("Transaction already in progress");
            
            _isInTransaction = true;
        }

        public void Commit()
        {
            if (!_isInTransaction)
                throw new InvalidOperationException("No transaction in progress");

            try
            {
                foreach (var operation in _pendingOperations)
                {
                    operation();
                }
                _pendingOperations.Clear();
            }
            finally
            {
                _isInTransaction = false;
            }
        }

        public void Rollback()
        {
            if (!_isInTransaction)
                throw new InvalidOperationException("No transaction in progress");

            _pendingOperations.Clear();
            _isInTransaction = false;
        }

        public void Dispose()
        {
            if (_isInTransaction)
            {
                Rollback();
            }
            _pendingOperations.Clear();
        }
    }

    /// <summary>
    /// Example entity class.
    /// </summary>
    public class Product : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Specialized repository for products with additional methods.
    /// </summary>
    public class ProductRepository : InMemoryRepository<Product, int>
    {
        public async Task<IEnumerable<Product>> GetByCategory(string category)
        {
            return await FindAsync(p => p.Category == category);
        }

        public async Task<IEnumerable<Product>> GetLowStockProducts(int threshold)
        {
            return await FindAsync(p => p.StockQuantity < threshold);
        }

        public async Task<decimal> GetTotalInventoryValue()
        {
            var products = await GetAllAsync();
            return products.Sum(p => p.Price * p.StockQuantity);
        }
    }
}