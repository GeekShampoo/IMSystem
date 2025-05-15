namespace IMSystem.Protocol.DTOs.Notifications;

public class KeyExchangeOfferDto
{
    public string SenderUserId { get; set; } = default!;
    public string PublicKey { get; set; } = default!;
}