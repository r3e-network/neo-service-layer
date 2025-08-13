using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoServiceLayer.Benchmarks
{
    /// <summary>
    /// Performance benchmarks for optimization improvements.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    public class OptimizationBenchmarks
    {
        private List<Vote> _votes;
        private HashSet<string> _validatorSet;
        private Dictionary<string, HashSet<Permission>> _rolePermissions;
        private Dictionary<string, HashSet<string>> _userRoles;
        private List<User> _users;

        [GlobalSetup]
        public void Setup()
        {
            // Setup test data
            _votes = GenerateVotes(1000);
            _validatorSet = new HashSet<string>(GenerateValidators(100));
            _rolePermissions = GenerateRolePermissions(50, 20);
            _userRoles = GenerateUserRoles(500, 5);
            _users = GenerateUsers(500);
        }

        /// <summary>
        /// Original nested loop approach (O(n²)).
        /// </summary>
        [Benchmark(Baseline = true)]
        public int ProcessVotes_NestedLoop()
        {
            var validVotes = 0;
            var validators = _validatorSet.ToList();
            
            foreach (var vote in _votes)
            {
                foreach (var validator in validators)
                {
                    if (vote.ValidatorId == validator)
                    {
                        validVotes++;
                        break;
                    }
                }
            }
            
            return validVotes;
        }

        /// <summary>
        /// Optimized HashSet lookup approach (O(n)).
        /// </summary>
        [Benchmark]
        public int ProcessVotes_HashSetOptimized()
        {
            return _votes.Count(vote => _validatorSet.Contains(vote.ValidatorId));
        }

        /// <summary>
        /// Parallel processing with HashSet.
        /// </summary>
        [Benchmark]
        public int ProcessVotes_ParallelOptimized()
        {
            return _votes.AsParallel()
                .Where(vote => _validatorSet.Contains(vote.ValidatorId))
                .Count();
        }

        /// <summary>
        /// Original triple nested loop for permissions (O(n³)).
        /// </summary>
        [Benchmark]
        public List<UserProjection> BuildUserProjections_NestedLoop()
        {
            var projections = new List<UserProjection>();
            var roles = _userRoles.SelectMany(kvp => kvp.Value.Select(r => new { UserId = kvp.Key, RoleId = r })).ToList();
            var permissions = _rolePermissions.SelectMany(kvp => kvp.Value.Select(p => new { RoleId = kvp.Key, Permission = p })).ToList();
            
            foreach (var user in _users)
            {
                var userPermissions = new HashSet<Permission>();
                
                foreach (var role in roles.Where(r => r.UserId == user.Id))
                {
                    foreach (var permission in permissions.Where(p => p.RoleId == role.RoleId))
                    {
                        userPermissions.Add(permission.Permission);
                    }
                }
                
                projections.Add(new UserProjection(user, userPermissions.ToList()));
            }
            
            return projections;
        }

        /// <summary>
        /// Optimized dictionary lookup approach (O(n)).
        /// </summary>
        [Benchmark]
        public List<UserProjection> BuildUserProjections_DictionaryOptimized()
        {
            return _users.Select(user =>
            {
                var userRoleIds = _userRoles.GetValueOrDefault(user.Id) ?? new HashSet<string>();
                var userPermissions = userRoleIds
                    .SelectMany(roleId => _rolePermissions.GetValueOrDefault(roleId) ?? new HashSet<Permission>())
                    .Distinct()
                    .ToList();
                
                return new UserProjection(user, userPermissions);
            }).ToList();
        }

        /// <summary>
        /// Async method without ConfigureAwait(false).
        /// </summary>
        [Benchmark]
        public async Task<int> AsyncOperation_WithoutConfigureAwait()
        {
            var result = 0;
            for (int i = 0; i < 100; i++)
            {
                result += await GetValueAsync();
            }
            return result;
        }

        /// <summary>
        /// Async method with ConfigureAwait(false).
        /// </summary>
        [Benchmark]
        public async Task<int> AsyncOperation_WithConfigureAwait()
        {
            var result = 0;
            for (int i = 0; i < 100; i++)
            {
                result += await GetValueAsync().ConfigureAwait(false);
            }
            return result;
        }

        private async Task<int> GetValueAsync()
        {
            await Task.Yield();
            return 42;
        }

        // Helper methods for data generation
        private List<Vote> GenerateVotes(int count)
        {
            var random = new Random(42);
            return Enumerable.Range(0, count)
                .Select(i => new Vote 
                { 
                    Id = i.ToString(), 
                    ValidatorId = $"validator_{random.Next(150)}" 
                })
                .ToList();
        }

        private List<string> GenerateValidators(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => $"validator_{i}")
                .ToList();
        }

        private Dictionary<string, HashSet<Permission>> GenerateRolePermissions(int roleCount, int permissionsPerRole)
        {
            var result = new Dictionary<string, HashSet<Permission>>();
            var random = new Random(42);
            
            for (int i = 0; i < roleCount; i++)
            {
                var permissions = new HashSet<Permission>();
                for (int j = 0; j < permissionsPerRole; j++)
                {
                    permissions.Add(new Permission 
                    { 
                        Id = $"perm_{i}_{j}", 
                        Name = $"Permission_{random.Next(100)}" 
                    });
                }
                result[$"role_{i}"] = permissions;
            }
            
            return result;
        }

        private Dictionary<string, HashSet<string>> GenerateUserRoles(int userCount, int maxRolesPerUser)
        {
            var result = new Dictionary<string, HashSet<string>>();
            var random = new Random(42);
            
            for (int i = 0; i < userCount; i++)
            {
                var roleCount = random.Next(1, maxRolesPerUser + 1);
                var roles = new HashSet<string>();
                
                for (int j = 0; j < roleCount; j++)
                {
                    roles.Add($"role_{random.Next(50)}");
                }
                
                result[$"user_{i}"] = roles;
            }
            
            return result;
        }

        private List<User> GenerateUsers(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => new User { Id = $"user_{i}", Name = $"User {i}" })
                .ToList();
        }
    }

    // Test models
    public class Vote
    {
        public string Id { get; set; }
        public string ValidatorId { get; set; }
    }

    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Permission
    {
        public string Id { get; set; }
        public string Name { get; set; }
        
        public override bool Equals(object obj)
        {
            return obj is Permission other && Id == other.Id;
        }
        
        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }
    }

    public class UserProjection
    {
        public User User { get; }
        public List<Permission> Permissions { get; }
        
        public UserProjection(User user, List<Permission> permissions)
        {
            User = user;
            Permissions = permissions;
        }
    }

    /// <summary>
    /// Benchmark runner.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<OptimizationBenchmarks>();
            Console.WriteLine(summary);
        }
    }
}