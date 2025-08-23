using System;
using System.Collections.Generic;
using NeoServiceLayer.Core.CQRS;

namespace NeoServiceLayer.Infrastructure.CQRS.Commands
{
    /// <summary>
    /// User-related commands for CQRS implementation
    /// </summary>
    
    public class CreateUserCommand : CommandBase
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Guid TenantId { get; set; }
        public List<Guid> RoleIds { get; set; }
        
        public CreateUserCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
            RoleIds = new List<Guid>();
        }
    }

    public class UpdateUserCommand : CommandBase
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        
        public UpdateUserCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
            Metadata = new Dictionary<string, string>();
        }
    }

    public class ChangePasswordCommand : CommandBase
    {
        public Guid UserId { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        
        public ChangePasswordCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
        }
    }

    public class EnableMfaCommand : CommandBase
    {
        public Guid UserId { get; set; }
        public string MfaType { get; set; } // TOTP, SMS, Email
        
        public EnableMfaCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
        }
    }

    public class DisableMfaCommand : CommandBase
    {
        public Guid UserId { get; set; }
        public string MfaCode { get; set; }
        
        public DisableMfaCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
        }
    }

    public class AssignRoleCommand : CommandBase
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid AssignedBy { get; set; }
        public DateTime? ExpiresAt { get; set; }
        
        public AssignRoleCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
        }
    }

    public class RemoveRoleCommand : CommandBase
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public string Reason { get; set; }
        
        public RemoveRoleCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
        }
    }

    public class LockUserCommand : CommandBase
    {
        public Guid UserId { get; set; }
        public string Reason { get; set; }
        public DateTime? LockedUntil { get; set; }
        public Guid LockedBy { get; set; }
        
        public LockUserCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
        }
    }

    public class UnlockUserCommand : CommandBase
    {
        public Guid UserId { get; set; }
        public Guid UnlockedBy { get; set; }
        
        public UnlockUserCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
        }
    }

    public class DeleteUserCommand : CommandBase
    {
        public Guid UserId { get; set; }
        public bool HardDelete { get; set; }
        public Guid DeletedBy { get; set; }
        
        public DeleteUserCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
        }
    }

    // Blockchain-related commands
    public class CreateTransactionCommand : CommandBase
    {
        public Guid NetworkId { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Value { get; set; }
        public string Data { get; set; }
        public decimal? GasPrice { get; set; }
        public long? GasLimit { get; set; }
        public long? Nonce { get; set; }
        
        public CreateTransactionCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
        }
    }

    public class DeployContractCommand : CommandBase
    {
        public Guid NetworkId { get; set; }
        public string ContractName { get; set; }
        public string Bytecode { get; set; }
        public string Abi { get; set; }
        public object[] ConstructorParams { get; set; }
        public string DeployerAddress { get; set; }
        
        public DeployContractCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
            ConstructorParams = Array.Empty<object>();
        }
    }

    public class CallContractCommand : CommandBase
    {
        public Guid NetworkId { get; set; }
        public string ContractAddress { get; set; }
        public string MethodName { get; set; }
        public object[] Parameters { get; set; }
        public string CallerAddress { get; set; }
        public decimal? Value { get; set; }
        
        public CallContractCommand(string initiatedBy, Guid? correlationId = null, long? expectedVersion = null)
            : base(initiatedBy, correlationId, expectedVersion)
        {
            Parameters = Array.Empty<object>();
        }
    }

    // Compute-related commands
    public class CreateComputeJobCommand : CommandBase
    {
        public string Name { get; set; }
        public string JobType { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public bool RequiresEnclave { get; set; }
        public int Priority { get; set; }
        public Guid CreatedBy { get; set; }
    }

    public class CancelComputeJobCommand : CommandBase
    {
        public Guid JobId { get; set; }
        public string Reason { get; set; }
        public Guid CancelledBy { get; set; }
    }

    // Storage-related commands
    public class UploadFileCommand : CommandBase
    {
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
        public string ContentType { get; set; }
        public bool Encrypt { get; set; }
        public string StorageTier { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public Guid UploadedBy { get; set; }
    }

    public class DeleteFileCommand : CommandBase
    {
        public Guid FileId { get; set; }
        public bool PermanentDelete { get; set; }
        public Guid DeletedBy { get; set; }
    }

    // Oracle-related commands
    public class CreateDataFeedCommand : CommandBase
    {
        public string Name { get; set; }
        public string FeedType { get; set; }
        public string DataSource { get; set; }
        public int UpdateInterval { get; set; }
        public List<string> Providers { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public Guid CreatedBy { get; set; }
    }

    public class UpdateDataFeedCommand : CommandBase
    {
        public string FeedId { get; set; }
        public int? UpdateInterval { get; set; }
        public List<string> Providers { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class PublishOnChainCommand : CommandBase
    {
        public string FeedId { get; set; }
        public string ContractAddress { get; set; }
        public Guid NetworkId { get; set; }
        public string PublisherAddress { get; set; }
    }

    // Notification commands
    public class SendNotificationCommand : CommandBase
    {
        public Guid UserId { get; set; }
        public string Type { get; set; } // Email, SMS, Push, InApp
        public string Subject { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public int Priority { get; set; }
    }

    public class CreateNotificationTemplateCommand : CommandBase
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Subject { get; set; }
        public string BodyTemplate { get; set; }
        public Dictionary<string, string> DefaultValues { get; set; }
        public Guid CreatedBy { get; set; }
    }
}