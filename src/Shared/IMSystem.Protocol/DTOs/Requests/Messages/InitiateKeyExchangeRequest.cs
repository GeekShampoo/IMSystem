namespace IMSystem.Protocol.DTOs.Requests.Messages;

public class InitiateKeyExchangeRequest
{
    public string RecipientUserId { get; set; } = default!;
    public string PublicKey { get; set; } = default!;
}