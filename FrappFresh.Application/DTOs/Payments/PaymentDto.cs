using FrappFresh.Domain.Enums;

namespace FrappFresh.Application.DTOs.Payments;

public record PaymentDto(Guid Id, Guid OrderId, decimal Amount, string Currency, PaymentMethod Method,
    PaymentStatus Status, string? Provider, string? ProviderTransactionId, string? PaymentReference,
    string? FailureReason, DateTimeOffset? PaidAt, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
