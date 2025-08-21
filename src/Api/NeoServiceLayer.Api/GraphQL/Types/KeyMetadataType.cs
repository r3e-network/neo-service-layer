using HotChocolate;
using HotChocolate.Types;
using NeoServiceLayer.Services.KeyManagement;

namespace NeoServiceLayer.Api.GraphQL.Types;

/// <summary>
/// GraphQL type for KeyMetadata.
/// </summary>
public class KeyMetadataType : ObjectType<KeyMetadata>
{
    /// <summary>
    /// Configures the KeyMetadata type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    protected override void Configure(IObjectTypeDescriptor<KeyMetadata> descriptor)
    {
        descriptor.Name("KeyMetadata");
        descriptor.Description("Cryptographic key metadata information");

        descriptor
            .Field(f => f.KeyId)
            .Description("Unique identifier for the key");

        descriptor
            .Field(f => f.KeyType)
            .Description("Type of cryptographic key (e.g., Secp256k1, Secp256r1)");

        descriptor
            .Field(f => f.KeyUsage)
            .Description("Allowed operations for the key (Sign, Verify, etc.)");

        descriptor
            .Field(f => f.PublicKeyHex)
            .Description("Hexadecimal representation of the public key");

        descriptor
            .Field(f => f.Exportable)
            .Description("Whether the key can be exported");

        descriptor
            .Field(f => f.CreatedAt)
            .Description("Timestamp when the key was created");

        descriptor
            .Field(f => f.LastUsedAt)
            .Description("Timestamp when the key was last used");

        descriptor
            .Field(f => f.ExpiresAt)
            .Description("Timestamp when the key expires");

        descriptor
            .Field(f => f.RotationScheduledAt)
            .Description("Timestamp when the key is scheduled for rotation");

        descriptor
            .Field(f => f.Algorithm)
            .Description("Cryptographic algorithm used");

        descriptor
            .Field(f => f.AttestationReport)
            .Description("TEE attestation report if available");

        descriptor
            .Field(f => f.Tags)
            .Description("Tags associated with the key");

        // Add computed field for key age
        descriptor
            .Field("age")
            .Type<NonNullType<StringType>>()
            .Description("Age of the key in human-readable format")
            .Resolve(ctx =>
            {
                var key = ctx.Parent<KeyMetadata>();
                var age = DateTime.UtcNow - key.CreatedAt;
                
                if (age.TotalDays >= 1)
                    return $"{(int)age.TotalDays} days";
                else if (age.TotalHours >= 1)
                    return $"{(int)age.TotalHours} hours";
                else
                    return $"{(int)age.TotalMinutes} minutes";
            });

        // Add computed field for expiry status
        descriptor
            .Field("isExpired")
            .Type<NonNullType<BooleanType>>()
            .Description("Whether the key has expired")
            .Resolve(ctx =>
            {
                var key = ctx.Parent<KeyMetadata>();
                return key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow;
            });
    }
}