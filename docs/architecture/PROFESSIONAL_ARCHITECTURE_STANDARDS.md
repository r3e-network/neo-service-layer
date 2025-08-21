# Neo Service Layer - Professional Architecture Standards

## Executive Summary

This document establishes the professional architectural standards for the Neo Service Layer, ensuring enterprise-grade quality, maintainability, and scalability. These standards are based on a comprehensive architectural analysis and industry best practices.

## Architecture Grading: B+ → A-

**Current State**: Good foundation with professional patterns
**Target State**: Enterprise-ready architecture with industry best practices

## 1. Domain-Driven Design (DDD) Standards

### Domain Model Requirements

**✅ Rich Domain Models**
```csharp
// ❌ Avoid: Anemic domain models
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    // No business logic
}

// ✅ Preferred: Rich domain models
public class User : AggregateRoot<UserId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public Username Username { get; private set; }
    public Password Password { get; private set; }
    public AuthenticationAttempts FailedAttempts { get; private set; }
    
    public void ChangePassword(Password newPassword, IPasswordPolicy policy)
    {
        policy.ValidatePassword(newPassword);
        Password = newPassword;
        AddDomainEvent(new PasswordChangedEvent(Id, Username));
    }
    
    public AuthenticationResult Authenticate(string plainPassword)
    {
        var isValid = Password.Verify(plainPassword);
        if (!isValid)
        {
            FailedAttempts = FailedAttempts.Increment();
            if (FailedAttempts.ExceedsLimit())
            {
                AddDomainEvent(new AccountLockedEvent(Id));
                return AuthenticationResult.AccountLocked();
            }
        }
        return AuthenticationResult.Success();
    }
}
```

### Value Objects Implementation
```csharp
public class Username : ValueObject
{
    public string Value { get; }
    
    private Username(string value)
    {
        Value = value;
    }
    
    public static Username Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Username cannot be empty");
            
        if (value.Length < 3 || value.Length > 50)
            throw new DomainException("Username must be between 3 and 50 characters");
            
        return new Username(value);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }
}
```

### Aggregate Root Pattern
```csharp
public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

## 2. Service Layer Architecture Standards

### Service Interface Segregation
```csharp
// ❌ Avoid: Large monolithic interfaces
public interface IAuthenticationService
{
    Task<AuthResult> AuthenticateAsync(string username, string password);
    Task RegisterAsync(RegisterRequest request);
    Task ChangePasswordAsync(string userId, string password);
    Task ResetPasswordAsync(string email);
    Task EnableMfaAsync(string userId);
    Task<bool> ValidateTokenAsync(string token);
    // 15+ more methods...
}

// ✅ Preferred: Focused, single-responsibility interfaces
public interface IUserAuthentication
{
    Task<AuthenticationResult> AuthenticateAsync(Username username, string password);
    Task<bool> ValidateTokenAsync(string token);
}

public interface IUserRegistration
{
    Task<RegistrationResult> RegisterAsync(UserRegistrationCommand command);
}

public interface IPasswordManagement
{
    Task<bool> ChangePasswordAsync(UserId userId, ChangePasswordCommand command);
    Task InitiatePasswordResetAsync(Email email);
}
```

### Service Implementation Standards
```csharp
[ServiceLifetime(ServiceLifetime.Scoped)]
[AuditLog]
[ValidateInput]
public class UserAuthenticationService : IUserAuthentication
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<UserAuthenticationService> _logger;
    
    public UserAuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IDomainEventPublisher eventPublisher,
        ILogger<UserAuthenticationService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<AuthenticationResult> AuthenticateAsync(Username username, string password)
    {
        using var activity = Activity.StartActivity(nameof(AuthenticateAsync));
        activity?.SetTag("username", username.Value);
        
        try
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("Authentication attempt for non-existent user {Username}", username.Value);
                return AuthenticationResult.InvalidCredentials();
            }
            
            var result = user.Authenticate(password);
            
            // Publish domain events
            foreach (var domainEvent in user.DomainEvents)
            {
                await _eventPublisher.PublishAsync(domainEvent);
            }
            user.ClearDomainEvents();
            
            await _userRepository.SaveAsync(user);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user {Username}", username.Value);
            throw;
        }
    }
}
```

## 3. Dependency Injection Best Practices

### Service Registration Standards
```csharp
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Scoped: Stateful services that maintain context per request
        services.AddScoped<IUserAuthentication, UserAuthenticationService>();
        services.AddScoped<IUserRegistration, UserRegistrationService>();
        
        // Transient: Lightweight services, factories, and stateless components
        services.AddTransient<IUserFactory, UserFactory>();
        services.AddTransient<IPasswordHasher, BCryptPasswordHasher>();
        
        // Singleton: Truly stateless services and policies
        services.AddSingleton<IPasswordPolicy, EnterprisePasswordPolicy>();
        services.AddSingleton<IEmailValidator, RegexEmailValidator>();
        
        return services;
    }
    
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Repositories should be scoped to the request
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, EntityFrameworkUnitOfWork>();
        
        return services;
    }
}
```

### Service Lifetime Guidelines
```csharp
// Service lifetime attribute for explicit declaration
[AttributeUsage(AttributeTargets.Class)]
public class ServiceLifetimeAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; }
    
    public ServiceLifetimeAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}

// Validation at startup
public static class ServiceLifetimeValidator
{
    public static void ValidateServiceLifetimes(IServiceCollection services)
    {
        foreach (var service in services)
        {
            var implementationType = service.ImplementationType;
            if (implementationType?.GetCustomAttribute<ServiceLifetimeAttribute>() is { } attribute)
            {
                if (service.Lifetime != attribute.Lifetime)
                {
                    throw new InvalidOperationException(
                        $"Service {implementationType.Name} is registered with {service.Lifetime} " +
                        $"but marked with {attribute.Lifetime}");
                }
            }
        }
    }
}
```

## 4. Error Handling Architecture

### Domain Exception Hierarchy
```csharp
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    
    protected DomainException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
    
    protected DomainException(string errorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string rule, string message)
        : base($"BUSINESS_RULE_VIOLATION.{rule}", message)
    {
    }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityType, object id)
        : base("ENTITY_NOT_FOUND", $"{entityType} with id '{id}' was not found")
    {
    }
}
```

### Global Error Handling
```csharp
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            DomainException domainEx => new ErrorResponse
            {
                ErrorCode = domainEx.ErrorCode,
                Message = domainEx.Message,
                StatusCode = GetStatusCode(domainEx)
            },
            ValidationException validationEx => new ErrorResponse
            {
                ErrorCode = "VALIDATION_ERROR",
                Message = validationEx.Message,
                StatusCode = 400,
                Details = validationEx.Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage)
            },
            _ => new ErrorResponse
            {
                ErrorCode = "INTERNAL_ERROR",
                Message = "An internal error occurred",
                StatusCode = 500
            }
        };
        
        context.Response.StatusCode = response.StatusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

## 5. Command/Query Separation (CQRS)

### Command Pattern Implementation
```csharp
public interface ICommand<TResult>
{
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
}

public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public class CreateUserCommand : ICommand<User>
{
    public Username Username { get; }
    public Email Email { get; }
    public string Password { get; }
    
    public CreateUserCommand(Username username, Email email, string password)
    {
        Username = username;
        Email = email;
        Password = password;
    }
}

[ServiceLifetime(ServiceLifetime.Scoped)]
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserFactory _userFactory;
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<User> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Check business rules
        var existingUser = await _userRepository.GetByUsernameAsync(command.Username);
        if (existingUser != null)
            throw new BusinessRuleViolationException("DUPLICATE_USERNAME", "Username already exists");
        
        // Create domain object
        var user = _userFactory.CreateUser(command.Username, command.Email, command.Password);
        
        // Save through repository
        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return user;
    }
}
```

## 6. Testing Architecture Standards

### Test Organization Structure
```
tests/
├── Unit/                          # Fast, isolated unit tests (< 1ms)
│   ├── Domain/                   # Domain model behavior tests
│   │   ├── UserTests.cs
│   │   └── ValueObjectTests.cs
│   ├── Services/                 # Service layer logic tests
│   │   ├── UserAuthenticationServiceTests.cs
│   │   └── UserRegistrationServiceTests.cs
│   └── Infrastructure/           # Infrastructure component tests
│       ├── RepositoryTests.cs
│       └── CachingTests.cs
├── Integration/                  # Component integration tests (< 100ms)
│   ├── Api/                     # Controller integration tests
│   ├── Database/                # Repository integration tests
│   └── Services/                # Service integration tests
├── Contract/                    # API contract tests
│   ├── RestApiContractTests.cs
│   └── GraphQLContractTests.cs
├── Architecture/                # Architectural fitness functions
│   ├── DependencyTests.cs
│   └── LayerTests.cs
├── Performance/                 # Performance and load tests
└── E2E/                        # End-to-end user scenarios
```

### Unit Test Standards
```csharp
[TestClass]
public class UserAuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IDomainEventPublisher> _eventPublisherMock;
    private readonly UserAuthenticationService _service;
    
    public UserAuthenticationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _eventPublisherMock = new Mock<IDomainEventPublisher>();
        _service = new UserAuthenticationService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _eventPublisherMock.Object,
            Mock.Of<ILogger<UserAuthenticationService>>());
    }
    
    [TestMethod]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var username = Username.Create("testuser");
        var password = "ValidPassword123!";
        var user = UserBuilder.WithUsername(username).WithValidPassword().Build();
        
        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync(user);
        
        // Act
        var result = await _service.AuthenticateAsync(username, password);
        
        // Assert
        result.Should().BeOfType<AuthenticationResult.Success>();
        _userRepositoryMock.Verify(x => x.SaveAsync(user), Times.Once);
    }
    
    [TestMethod]
    public async Task AuthenticateAsync_InvalidCredentials_ReturnsFailureAndIncrementsAttempts()
    {
        // Arrange
        var username = Username.Create("testuser");
        var password = "WrongPassword";
        var user = UserBuilder.WithUsername(username).WithFailedAttempts(0).Build();
        
        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync(user);
        
        // Act
        var result = await _service.AuthenticateAsync(username, password);
        
        // Assert
        result.Should().BeOfType<AuthenticationResult.InvalidCredentials>();
        user.FailedAttempts.Count.Should().Be(1);
    }
}
```

### Architectural Fitness Functions
```csharp
[TestClass]
public class ArchitecturalTests
{
    [TestMethod]
    public void DomainLayer_ShouldNotDependOn_InfrastructureLayer()
    {
        var domainAssembly = typeof(User).Assembly;
        var infrastructureAssembly = typeof(UserRepository).Assembly;
        
        var result = Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOn(infrastructureAssembly.GetName().Name);
        
        result.Should().BeTrue();
    }
    
    [TestMethod]
    public void Services_ShouldHave_SingleResponsibility()
    {
        var services = Types.InCurrentDomain()
            .That()
            .HaveNameEndingWith("Service")
            .GetTypes();
        
        foreach (var service in services)
        {
            var publicMethods = service.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.DeclaringType == service)
                .Count();
                
            publicMethods.Should().BeLessThanOrEqualTo(5, 
                $"Service {service.Name} has too many public methods, consider splitting");
        }
    }
}
```

## 7. Configuration Management Standards

### Typed Configuration
```csharp
[ConfigurationSection("Authentication")]
public class AuthenticationOptions
{
    public string JwtSecret { get; set; } = string.Empty;
    public TimeSpan TokenExpiration { get; set; } = TimeSpan.FromHours(1);
    public int MaxFailedAttempts { get; set; } = 5;
    public TimeSpan LockoutDuration { get; set; } = TimeSpan.FromMinutes(30);
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(JwtSecret))
            throw new ConfigurationException("JWT secret is required");
            
        if (JwtSecret.Length < 32)
            throw new ConfigurationException("JWT secret must be at least 32 characters");
            
        if (TokenExpiration <= TimeSpan.Zero)
            throw new ConfigurationException("Token expiration must be positive");
    }
}

// Registration and validation
services.Configure<AuthenticationOptions>(configuration.GetSection("Authentication"));
services.AddSingleton<IValidateOptions<AuthenticationOptions>, AuthenticationOptionsValidator>();
```

## 8. Performance and Monitoring Standards

### Performance Monitoring
```csharp
public static class PerformanceMetrics
{
    private static readonly Counter AuthenticationAttempts = Metrics
        .CreateCounter("auth_attempts_total", "Total authentication attempts", "result");
        
    private static readonly Histogram AuthenticationDuration = Metrics
        .CreateHistogram("auth_duration_seconds", "Authentication duration in seconds");
    
    public static void RecordAuthenticationAttempt(string result)
    {
        AuthenticationAttempts.WithLabels(result).Inc();
    }
    
    public static IDisposable TimeAuthentication()
    {
        return AuthenticationDuration.NewTimer();
    }
}

// Usage in service
public async Task<AuthenticationResult> AuthenticateAsync(Username username, string password)
{
    using var timer = PerformanceMetrics.TimeAuthentication();
    
    try
    {
        var result = await PerformAuthenticationAsync(username, password);
        PerformanceMetrics.RecordAuthenticationAttempt(result.IsSuccess ? "success" : "failure");
        return result;
    }
    catch
    {
        PerformanceMetrics.RecordAuthenticationAttempt("error");
        throw;
    }
}
```

## Implementation Priority

### Phase 1 (Immediate - Critical Issues)
1. **Fix Service Lifetime Issues** - Review all service registrations
2. **Implement Interface Segregation** - Break down large interfaces
3. **Add Domain Exception Hierarchy** - Consistent error handling

### Phase 2 (Next Sprint - Foundation)
4. **Implement Rich Domain Models** - Add business behavior to entities
5. **Add Command/Query Separation** - CQRS for complex operations
6. **Reorganize Test Structure** - Professional test organization

### Phase 3 (Future - Enhancement)
7. **Add Architectural Fitness Functions** - Automated architecture validation
8. **Implement Advanced Monitoring** - Performance and business metrics
9. **Add Configuration Validation** - Startup validation and type safety

## Success Criteria

- **Architecture Grade**: A- (90%+ score on architecture assessment)
- **Test Coverage**: 85%+ code coverage with organized test structure
- **Performance**: Sub-200ms response times for authentication operations
- **Maintainability**: Clear separation of concerns with focused responsibilities
- **Scalability**: Support for horizontal scaling without architectural changes

## Conclusion

These professional architecture standards elevate the Neo Service Layer to enterprise-grade quality while maintaining the existing functionality. The phased implementation approach ensures continuous delivery while systematically improving the codebase quality.

**Current State**: Good foundation (B+)
**Target State**: Professional enterprise architecture (A-)