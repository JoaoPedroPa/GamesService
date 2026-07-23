namespace FCG.Contracts.Payments;

public sealed record ProcessPaymentRequest(Guid PurchaseId, int UserId, int GameId, decimal Amount);
public sealed record ProcessPaymentResponse(Guid PaymentId, Guid PurchaseId, string Status, decimal Amount, string? FailureReason);
