using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Infrastructure.CQRS;
using Xunit;

namespace NeoServiceLayer.Infrastructure.CQRS.Tests
{
    public class CommandHandlerTests
    {
        private readonly Mock<ILogger<CommandBus>> _mockLogger;
        private readonly CommandBus _commandBus;
        private readonly Mock<ICommandValidator> _mockValidator;

        public CommandHandlerTests()
        {
            _mockLogger = new Mock<ILogger<CommandBus>>();
            _mockValidator = new Mock<ICommandValidator>();
            _commandBus = new CommandBus(_mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_WithValidCommand_ExecutesSuccessfully()
        {
            // Arrange
            var command = new CreateUserCommand 
            { 
                UserId = Guid.NewGuid(), 
                Username = "testuser",
                Email = "test@example.com"
            };
            
            var handler = new CreateUserCommandHandler();
            _commandBus.RegisterHandler<CreateUserCommand>(handler);

            // Act
            var result = await _commandBus.SendAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(command.UserId);
        }

        [Fact]
        public async Task HandleAsync_WithInvalidCommand_ReturnsFailure()
        {
            // Arrange
            var command = new CreateUserCommand 
            { 
                UserId = Guid.Empty, // Invalid
                Username = "",
                Email = "invalid"
            };
            
            var handler = new CreateUserCommandHandler();
            _commandBus.RegisterHandler<CreateUserCommand>(handler);

            // Act
            var result = await _commandBus.SendAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task HandleAsync_WithValidator_ValidatesBeforeExecution()
        {
            // Arrange
            var command = new UpdateUserCommand 
            { 
                UserId = Guid.NewGuid(),
                NewEmail = "newemail@example.com"
            };
            
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateUserCommand>()))
                .ReturnsAsync(new ValidationResult { IsValid = true });

            var handler = new UpdateUserCommandHandler(_mockValidator.Object);
            _commandBus.RegisterHandler<UpdateUserCommand>(handler);

            // Act
            var result = await _commandBus.SendAsync(command);

            // Assert
            _mockValidator.Verify(v => v.ValidateAsync(command), Times.Once);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task HandleAsync_WithFailedValidation_DoesNotExecute()
        {
            // Arrange
            var command = new UpdateUserCommand 
            { 
                UserId = Guid.NewGuid(),
                NewEmail = "invalid-email"
            };
            
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<UpdateUserCommand>()))
                .ReturnsAsync(new ValidationResult 
                { 
                    IsValid = false, 
                    Errors = new[] { "Invalid email format" }
                });

            var handler = new UpdateUserCommandHandler(_mockValidator.Object);
            _commandBus.RegisterHandler<UpdateUserCommand>(handler);

            // Act
            var result = await _commandBus.SendAsync(command);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Invalid email");
        }

        [Fact]
        public async Task HandleAsync_WithCancellation_CancelsExecution()
        {
            // Arrange
            var command = new LongRunningCommand { Duration = TimeSpan.FromSeconds(10) };
            var handler = new LongRunningCommandHandler();
            _commandBus.RegisterHandler<LongRunningCommand>(handler);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(100);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await _commandBus.SendAsync(command, cts.Token));
        }

        [Fact]
        public async Task HandleAsync_WithRetryPolicy_RetriesOnFailure()
        {
            // Arrange
            var command = new RetryableCommand { MaxAttempts = 3 };
            var handler = new RetryableCommandHandler();
            _commandBus.RegisterHandler<RetryableCommand>(handler);

            // Act
            var result = await _commandBus.SendAsync(command);

            // Assert
            result.IsSuccess.Should().BeTrue();
            handler.AttemptCount.Should().Be(3);
        }

        [Fact]
        public async Task HandleAsync_WithTransaction_CommitsOnSuccess()
        {
            // Arrange
            var command = new TransactionalCommand { Value = "test-value" };
            var mockTransaction = new Mock<ITransaction>();
            var handler = new TransactionalCommandHandler(mockTransaction.Object);
            _commandBus.RegisterHandler<TransactionalCommand>(handler);

            // Act
            var result = await _commandBus.SendAsync(command);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockTransaction.Verify(t => t.CommitAsync(), Times.Once);
            mockTransaction.Verify(t => t.RollbackAsync(), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithTransactionFailure_RollsBack()
        {
            // Arrange
            var command = new TransactionalCommand { Value = "fail" };
            var mockTransaction = new Mock<ITransaction>();
            var handler = new TransactionalCommandHandler(mockTransaction.Object);
            _commandBus.RegisterHandler<TransactionalCommand>(handler);

            // Act
            var result = await _commandBus.SendAsync(command);

            // Assert
            result.IsSuccess.Should().BeFalse();
            mockTransaction.Verify(t => t.RollbackAsync(), Times.Once);
            mockTransaction.Verify(t => t.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithPipeline_ExecutesInOrder()
        {
            // Arrange
            var command = new PipelinedCommand();
            var executionOrder = new List<string>();
            
            _commandBus.AddPipeline<LoggingPipeline>(p => p.ExecutionOrder = executionOrder);
            _commandBus.AddPipeline<ValidationPipeline>(p => p.ExecutionOrder = executionOrder);
            _commandBus.AddPipeline<AuthorizationPipeline>(p => p.ExecutionOrder = executionOrder);
            
            var handler = new PipelinedCommandHandler();
            _commandBus.RegisterHandler<PipelinedCommand>(handler);

            // Act
            await _commandBus.SendAsync(command);

            // Assert
            executionOrder.Should().ContainInOrder("Logging", "Validation", "Authorization", "Handler");
        }

        [Fact]
        public async Task HandleAsync_WithEventSourcing_StoresEvents()
        {
            // Arrange
            var command = new EventSourcedCommand { AggregateId = Guid.NewGuid() };
            var mockEventStore = new Mock<IEventStore>();
            var handler = new EventSourcedCommandHandler(mockEventStore.Object);
            _commandBus.RegisterHandler<EventSourcedCommand>(handler);

            // Act
            var result = await _commandBus.SendAsync(command);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockEventStore.Verify(es => es.AppendAsync(
                It.IsAny<Guid>(), 
                It.IsAny<IEnumerable<IDomainEvent>>(), 
                It.IsAny<int>()), 
                Times.Once);
        }

        [Fact]
        public void RegisterHandler_WithDuplicateRegistration_ThrowsException()
        {
            // Arrange
            var handler1 = new CreateUserCommandHandler();
            var handler2 = new CreateUserCommandHandler();
            
            _commandBus.RegisterHandler<CreateUserCommand>(handler1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _commandBus.RegisterHandler<CreateUserCommand>(handler2));
        }

        [Fact]
        public async Task SendAsync_WithoutHandler_ThrowsException()
        {
            // Arrange
            var command = new UnhandledCommand();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _commandBus.SendAsync(command));
        }
    }

    // Test Commands
    public class CreateUserCommand : ICommand
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class UpdateUserCommand : ICommand
    {
        public Guid UserId { get; set; }
        public string NewEmail { get; set; }
    }

    public class LongRunningCommand : ICommand
    {
        public TimeSpan Duration { get; set; }
    }

    public class RetryableCommand : ICommand
    {
        public int MaxAttempts { get; set; }
    }

    public class TransactionalCommand : ICommand
    {
        public string Value { get; set; }
    }

    public class PipelinedCommand : ICommand { }

    public class EventSourcedCommand : ICommand
    {
        public Guid AggregateId { get; set; }
    }

    public class UnhandledCommand : ICommand { }

    // Test Handlers
    public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
    {
        public async Task<CommandResult> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
        {
            if (command.UserId == Guid.Empty || string.IsNullOrEmpty(command.Username))
            {
                return CommandResult.Failure("Invalid user data");
            }

            await Task.Delay(10, cancellationToken);
            return CommandResult.Success(command.UserId);
        }
    }

    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
    {
        private readonly ICommandValidator _validator;

        public UpdateUserCommandHandler(ICommandValidator validator)
        {
            _validator = validator;
        }

        public async Task<CommandResult> HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
        {
            var validation = await _validator.ValidateAsync(command);
            if (!validation.IsValid)
            {
                return CommandResult.Failure(string.Join(", ", validation.Errors));
            }

            await Task.Delay(10, cancellationToken);
            return CommandResult.Success();
        }
    }

    public class LongRunningCommandHandler : ICommandHandler<LongRunningCommand>
    {
        public async Task<CommandResult> HandleAsync(LongRunningCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Delay(command.Duration, cancellationToken);
            return CommandResult.Success();
        }
    }

    public class RetryableCommandHandler : ICommandHandler<RetryableCommand>
    {
        public int AttemptCount { get; private set; }

        public async Task<CommandResult> HandleAsync(RetryableCommand command, CancellationToken cancellationToken = default)
        {
            AttemptCount++;
            
            if (AttemptCount < command.MaxAttempts)
            {
                throw new TransientException("Temporary failure");
            }

            await Task.Delay(10, cancellationToken);
            return CommandResult.Success();
        }
    }

    public class TransactionalCommandHandler : ICommandHandler<TransactionalCommand>
    {
        private readonly ITransaction _transaction;

        public TransactionalCommandHandler(ITransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task<CommandResult> HandleAsync(TransactionalCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                await _transaction.BeginAsync();
                
                if (command.Value == "fail")
                {
                    throw new InvalidOperationException("Simulated failure");
                }

                await Task.Delay(10, cancellationToken);
                await _transaction.CommitAsync();
                
                return CommandResult.Success();
            }
            catch
            {
                await _transaction.RollbackAsync();
                return CommandResult.Failure("Transaction failed");
            }
        }
    }

    public class PipelinedCommandHandler : ICommandHandler<PipelinedCommand>
    {
        public async Task<CommandResult> HandleAsync(PipelinedCommand command, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return CommandResult.Success();
        }
    }

    public class EventSourcedCommandHandler : ICommandHandler<EventSourcedCommand>
    {
        private readonly IEventStore _eventStore;

        public EventSourcedCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<CommandResult> HandleAsync(EventSourcedCommand command, CancellationToken cancellationToken = default)
        {
            var events = new List<IDomainEvent>
            {
                new UserCreatedEvent { AggregateId = command.AggregateId, Timestamp = DateTime.UtcNow }
            };

            await _eventStore.AppendAsync(command.AggregateId, events, 0);
            return CommandResult.Success();
        }
    }

    // Supporting classes
    public class CommandResult
    {
        public bool IsSuccess { get; set; }
        public object Data { get; set; }
        public string Error { get; set; }

        public static CommandResult Success(object data = null) => new() { IsSuccess = true, Data = data };
        public static CommandResult Failure(string error) => new() { IsSuccess = false, Error = error };
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string[] Errors { get; set; }
    }

    public interface ITransaction
    {
        Task BeginAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }

    public interface IEventStore
    {
        Task AppendAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion);
    }

    public interface IDomainEvent
    {
        Guid AggregateId { get; }
        DateTime Timestamp { get; }
    }

    public class UserCreatedEvent : IDomainEvent
    {
        public Guid AggregateId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TransientException : Exception
    {
        public TransientException(string message) : base(message) { }
    }

    public class LoggingPipeline
    {
        public List<string> ExecutionOrder { get; set; }
        public void Execute() => ExecutionOrder.Add("Logging");
    }

    public class ValidationPipeline
    {
        public List<string> ExecutionOrder { get; set; }
        public void Execute() => ExecutionOrder.Add("Validation");
    }

    public class AuthorizationPipeline
    {
        public List<string> ExecutionOrder { get; set; }
        public void Execute() => ExecutionOrder.Add("Authorization");
    }
}