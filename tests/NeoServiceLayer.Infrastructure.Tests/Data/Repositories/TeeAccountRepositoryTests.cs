using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Infrastructure.Data;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Infrastructure.Data.Entities;
using Xunit;

namespace NeoServiceLayer.Infrastructure.Tests.Data.Repositories
{
    public class TeeAccountRepositoryTests
    {
        private readonly DbContextOptions<NeoServiceLayerDbContext> _options;

        public TeeAccountRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<NeoServiceLayerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task AddAccountAsync_ShouldAddAccount()
        {
            // Arrange
            var account = new TeeAccount
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Account",
                Type = AccountType.Wallet,
                PublicKey = "public-key",
                Address = "address",
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                IsExportable = true,
                Metadata = new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 123 }
                },
                AttestationProof = "attestation-proof-data"
            };

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new TeeAccountRepository(context);
                await repository.AddAccountAsync(account);
            }

            // Assert
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var savedAccount = await context.TeeAccounts.FindAsync(account.Id);
                Assert.NotNull(savedAccount);
                Assert.Equal(account.Name, savedAccount.Name);
                Assert.Equal(account.Type, savedAccount.Type);
                Assert.Equal(account.PublicKey, savedAccount.PublicKey);
                Assert.Equal(account.Address, savedAccount.Address);
                Assert.Equal(account.UserId, savedAccount.UserId);
                Assert.Equal(account.IsExportable, savedAccount.IsExportable);
            }
        }

        [Fact]
        public async Task GetAccountByIdAsync_ShouldReturnAccount()
        {
            // Arrange
            var account = new TeeAccount
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Account",
                Type = AccountType.Wallet,
                PublicKey = "public-key",
                Address = "address",
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                IsExportable = true,
                Metadata = new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 123 }
                },
                AttestationProof = "attestation-proof-data"
            };

            using (var context = new NeoServiceLayerDbContext(_options))
            {
                context.TeeAccounts.Add(TeeAccountEntity.FromDomainModel(account));
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new TeeAccountRepository(context);
                var result = await repository.GetAccountByIdAsync(account.Id);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(account.Id, result.Id);
                Assert.Equal(account.Name, result.Name);
                Assert.Equal(account.Type, result.Type);
                Assert.Equal(account.PublicKey, result.PublicKey);
                Assert.Equal(account.Address, result.Address);
                Assert.Equal(account.UserId, result.UserId);
                Assert.Equal(account.IsExportable, result.IsExportable);
            }
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnUserAccounts()
        {
            // Arrange
            var userId = "user1";
            var accounts = new List<TeeAccount>
            {
                new TeeAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Account 1",
                    Type = AccountType.Wallet,
                    PublicKey = "public-key-1",
                    Address = "address-1",
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsExportable = true,
                    Metadata = new Dictionary<string, object>
                    {
                        { "key1", "value1" },
                        { "key2", 123 }
                    },
                    AttestationProof = "attestation-proof-data-1"
                },
                new TeeAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Account 2",
                    Type = AccountType.Identity,
                    PublicKey = "public-key-2",
                    Address = "address-2",
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsExportable = false,
                    Metadata = new Dictionary<string, object>
                    {
                        { "key1", "value1" },
                        { "key2", 123 }
                    },
                    AttestationProof = "attestation-proof-data-2"
                },
                new TeeAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Account 3",
                    Type = AccountType.Wallet,
                    PublicKey = "public-key-3",
                    Address = "address-3",
                    UserId = "user2", // Different user
                    CreatedAt = DateTime.UtcNow,
                    IsExportable = true,
                    Metadata = new Dictionary<string, object>
                    {
                        { "key1", "value1" },
                        { "key2", 123 }
                    },
                    AttestationProof = "attestation-proof-data-3"
                }
            };

            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var entities = accounts.Select(a => TeeAccountEntity.FromDomainModel(a)).ToList();
                context.TeeAccounts.AddRange(entities);
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new TeeAccountRepository(context);
                var result = await repository.GetByUserIdAsync(userId);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(2, result.Count());
                Assert.All(result, a => Assert.Equal(userId, a.UserId));
                Assert.Contains(result, a => a.Name == "Account 1");
                Assert.Contains(result, a => a.Name == "Account 2");
                Assert.DoesNotContain(result, a => a.Name == "Account 3");
            }
        }

        [Fact]
        public async Task UpdateAccountAsync_ShouldUpdateAccount()
        {
            // Arrange
            var account = new TeeAccount
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Account",
                Type = AccountType.Wallet,
                PublicKey = "public-key",
                Address = "address",
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                IsExportable = true,
                Metadata = new Dictionary<string, object>
                {
                    { "key1", "value1" },
                    { "key2", 123 }
                },
                AttestationProof = "attestation-proof-data"
            };

            using (var context = new NeoServiceLayerDbContext(_options))
            {
                context.TeeAccounts.Add(TeeAccountEntity.FromDomainModel(account));
                await context.SaveChangesAsync();
            }

            // Act
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var repository = new TeeAccountRepository(context);
                var accountToUpdate = await repository.GetAccountByIdAsync(account.Id);
                accountToUpdate.Name = "Updated Account";
                accountToUpdate.IsExportable = false;
                accountToUpdate.UpdatedAt = DateTime.UtcNow;

                await repository.UpdateAccountAsync(accountToUpdate);
            }

            // Assert
            using (var context = new NeoServiceLayerDbContext(_options))
            {
                var updatedAccount = await context.TeeAccounts.FindAsync(account.Id);
                Assert.NotNull(updatedAccount);
                Assert.Equal("Updated Account", updatedAccount.Name);
                Assert.False(updatedAccount.IsExportable);
                Assert.NotNull(updatedAccount.UpdatedAt);
            }
        }
    }
}
