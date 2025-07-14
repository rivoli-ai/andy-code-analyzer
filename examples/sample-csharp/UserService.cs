using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleApp.Services
{
    /// <summary>
    /// Service for managing user operations.
    /// </summary>
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> CreateUserAsync(string name, string email);
        Task<bool> UpdateUserAsync(int id, string name, string email);
        Task<bool> DeleteUserAsync(int id);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
    }

    /// <summary>
    /// Implementation of user service with in-memory storage.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly List<User> _users = new();
        private readonly object _lock = new();
        private int _nextId = 1;

        public UserService()
        {
            // Initialize with some sample data
            InitializeSampleData();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            await Task.Delay(10); // Simulate async operation
            lock (_lock)
            {
                return _users.FirstOrDefault(u => u.Id == id);
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            await Task.Delay(10); // Simulate async operation
            lock (_lock)
            {
                return _users.ToList();
            }
        }

        public async Task<User> CreateUserAsync(string name, string email)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            await Task.Delay(20); // Simulate async operation

            lock (_lock)
            {
                var user = new User
                {
                    Id = _nextId++,
                    Name = name,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _users.Add(user);
                return user;
            }
        }

        public async Task<bool> UpdateUserAsync(int id, string name, string email)
        {
            await Task.Delay(15); // Simulate async operation

            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                    return false;

                if (!string.IsNullOrWhiteSpace(name))
                    user.Name = name;
                if (!string.IsNullOrWhiteSpace(email))
                    user.Email = email;
                user.ModifiedAt = DateTime.UtcNow;

                return true;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            await Task.Delay(10); // Simulate async operation

            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                    return false;

                _users.Remove(user);
                return true;
            }
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllUsersAsync();

            await Task.Delay(15); // Simulate async operation

            lock (_lock)
            {
                var lowerSearch = searchTerm.ToLower();
                return _users.Where(u => 
                    u.Name.ToLower().Contains(lowerSearch) || 
                    u.Email.ToLower().Contains(lowerSearch))
                    .ToList();
            }
        }

        private void InitializeSampleData()
        {
            _users.AddRange(new[]
            {
                new User { Id = _nextId++, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.UtcNow.AddDays(-30), IsActive = true },
                new User { Id = _nextId++, Name = "Jane Smith", Email = "jane@example.com", CreatedAt = DateTime.UtcNow.AddDays(-25), IsActive = true },
                new User { Id = _nextId++, Name = "Bob Johnson", Email = "bob@example.com", CreatedAt = DateTime.UtcNow.AddDays(-20), IsActive = false },
            });
        }
    }

    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool IsActive { get; set; }

        public override string ToString() => $"{Name} ({Email})";
    }
}