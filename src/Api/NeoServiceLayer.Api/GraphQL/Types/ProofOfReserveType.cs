using HotChocolate;
using HotChocolate.Types;
using NeoServiceLayer.Services.ProofOfReserve;

namespace NeoServiceLayer.Api.GraphQL.Types;

/// <summary>
/// GraphQL type for ProofOfReserve.
/// </summary>
public class ProofOfReserveType : ObjectType<ProofOfReserveData>
{
    /// <summary>
    /// Configures the ProofOfReserve type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    protected override void Configure(IObjectTypeDescriptor<ProofOfReserveData> descriptor)
    {
        descriptor.Name("ProofOfReserve");
        descriptor.Description("Proof of reserve attestation data");

        descriptor
            .Field(f => f.AssetId)
            .Description("Identifier of the asset");

        descriptor
            .Field(f => f.TotalReserves)
            .Description("Total amount of reserves");

        descriptor
            .Field(f => f.TotalLiabilities)
            .Description("Total amount of liabilities");

        descriptor
            .Field(f => f.BlockHeight)
            .Description("Blockchain block height at attestation");

        descriptor
            .Field(f => f.Timestamp)
            .Description("When the proof was generated");

        descriptor
            .Field(f => f.MerkleRoot)
            .Description("Merkle root of the reserve tree");

        descriptor
            .Field(f => f.AttestationSignature)
            .Description("Cryptographic signature of the attestation");

        // Add computed field
        descriptor
            .Field("reserveRatio")
            .Type<NonNullType<FloatType>>()
            .Description("Ratio of reserves to liabilities")
            .Resolve(ctx =>
            {
                var proof = ctx.Parent<ProofOfReserveData>();
                return proof.TotalLiabilities > 0 
                    ? (double)proof.TotalReserves / proof.TotalLiabilities 
                    : 0;
            });
    }
}