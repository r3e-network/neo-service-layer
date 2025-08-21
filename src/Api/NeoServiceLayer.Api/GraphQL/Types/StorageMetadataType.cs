using HotChocolate;
using HotChocolate.Types;
using NeoServiceLayer.Services.Storage;

namespace NeoServiceLayer.Api.GraphQL.Types;

/// <summary>
/// GraphQL type for StorageMetadata.
/// </summary>
public class StorageMetadataType : ObjectType<StorageMetadata>
{
    /// <summary>
    /// Configures the StorageMetadata type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    protected override void Configure(IObjectTypeDescriptor<StorageMetadata> descriptor)
    {
        descriptor.Name("StorageMetadata");
        descriptor.Description("Metadata for stored objects");

        descriptor
            .Field(f => f.ObjectId)
            .Description("Unique identifier for the object");

        descriptor
            .Field(f => f.Bucket)
            .Description("Storage bucket name");

        descriptor
            .Field(f => f.Key)
            .Description("Object key within the bucket");

        descriptor
            .Field(f => f.Size)
            .Description("Size of the object in bytes");

        descriptor
            .Field(f => f.ContentType)
            .Description("MIME type of the object");

        descriptor
            .Field(f => f.Hash)
            .Description("Content hash for integrity verification");

        descriptor
            .Field(f => f.CreatedAt)
            .Description("When the object was created");

        descriptor
            .Field(f => f.ModifiedAt)
            .Description("When the object was last modified");

        descriptor
            .Field(f => f.Metadata)
            .Description("Custom metadata key-value pairs");

        // Add computed field
        descriptor
            .Field("sizeFormatted")
            .Type<NonNullType<StringType>>()
            .Description("Human-readable file size")
            .Resolve(ctx =>
            {
                var storage = ctx.Parent<StorageMetadata>();
                var size = storage.Size;
                
                return size switch
                {
                    < 1024 => $"{size} B",
                    < 1024 * 1024 => $"{size / 1024:F2} KB",
                    < 1024 * 1024 * 1024 => $"{size / (1024 * 1024):F2} MB",
                    _ => $"{size / (1024 * 1024 * 1024):F2} GB"
                };
            });
    }
}