using FrappFresh.Application.DTOs.Frappes;

namespace FrappFresh.Application.Interfaces;

public interface IFrappeService
{
    Task<IReadOnlyList<FrappeDto>> GetCatalogAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FrappeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FrappeDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FrappeDto> CreateAsync(FrappeRequestDto request, CancellationToken cancellationToken = default);
    Task<FrappeDto> UpdateAsync(Guid id, FrappeRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
