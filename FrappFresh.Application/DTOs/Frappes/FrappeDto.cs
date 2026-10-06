using FrappFresh.Domain.Enums;

namespace FrappFresh.Application.DTOs.Frappes;

public record FrappeDto(Guid Id, string Name, string Description, string Flavor, BeverageBase Base,
    string? Toppings, decimal Price, string? ImageUrl, bool IsPremium, bool IsAvailable, bool IsActive,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
