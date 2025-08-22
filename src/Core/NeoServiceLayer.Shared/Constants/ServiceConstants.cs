namespace NeoServiceLayer.Shared.Constants;

public static class ServiceConstants
{
    public static class Patterns
    {
        public const string Email = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        public const string Url = @"^https?://[^\s/$.?#].[^\s]*$";
        public const string HexString = @"^[0-9A-Fa-f]+$";
        public const string IPv4 = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
        public const string PhoneNumber = @"^\+?[1-9]\d{1,14}$";
        public const string NeoAddress = @"^[AN][1-9A-HJ-NP-Za-km-z]{33}$";
        public const string EthereumAddress = @"^0x[a-fA-F0-9]{40}$";
    }
}
