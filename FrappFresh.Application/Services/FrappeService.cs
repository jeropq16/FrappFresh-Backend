using FrappFresh.Application.DTOs.Frappes;
using FrappFresh.Application.Exceptions;
using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Interfaces;

namespace FrappFresh.Application.Services;

public class FrappeService(IFrappeRepository frappes, IUserAccessService access, IRequestValidator validator,
    IUnitOfWork unitOfWork) : IFrappeService
{
    public async Task<IReadOnlyList<FrappeDto>> GetCatalogAsync(CancellationToken cancellationToken = default) =>
        (await frappes.GetAvailableAsync(cancellationToken)).Select(x => x.ToDto()).ToArray();

    public async Task<IReadOnlyList<FrappeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await access.RequireAdminAsync(cancellationToken);
        return (await frappes.GetAllAsync(cancellationToken)).Select(x => x.ToDto()).ToArray();
    }

    public async Task<FrappeDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        var frappe = await FindAsync(id, cancellationToken);
        if (!frappe.IsActive) await access.RequireAdminAsync(cancellationToken);
        return frappe.ToDto();
    }

    public async Task<FrappeDto> CreateAsync(FrappeRequestDto request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireAdminAsync(ct);
            var frappe = new Frappe();
            Apply(frappe, request);
            await frappes.AddAsync(frappe, ct);
            return frappe.ToDto();
        }, cancellationToken);
    }

    public async Task<FrappeDto> UpdateAsync(Guid id, FrappeRequestDto request, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireAdminAsync(ct);
            var frappe = await FindAsync(id, ct);
            Apply(frappe, request);
            await frappes.UpdateAsync(frappe, ct);
            return (await FindAsync(id, ct)).ToDto();
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireAdminAsync(ct);
            var frappe = await FindAsync(id, ct);
            frappe.IsActive = false;
            frappe.IsAvailable = false;
            await frappes.UpdateAsync(frappe, ct);
            return true;
        }, cancellationToken);
    }

    private async Task<Frappe> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await frappes.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Frappé no encontrado.");

    private static void Apply(Frappe frappe, FrappeRequestDto request)
    {
        frappe.Name = request.Name.Trim();
        frappe.Description = request.Description.Trim();
        frappe.Flavor = request.Flavor.Trim();
        frappe.Base = request.Base;
        frappe.Toppings = ServiceGuard.OptionalText(request.Toppings);
        frappe.Price = request.Price;
        frappe.ImageUrl = ServiceGuard.OptionalText(request.ImageUrl);
        frappe.IsPremium = request.IsPremium;
        frappe.IsAvailable = request.IsAvailable;
        frappe.IsActive = request.IsActive;
    }
}
