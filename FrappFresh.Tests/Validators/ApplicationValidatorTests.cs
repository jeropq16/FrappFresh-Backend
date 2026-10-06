using FrappFresh.Application;
using FrappFresh.Application.DTOs.Auth;
using FrappFresh.Application.DTOs.Frappes;
using FrappFresh.Application.DTOs.Orders;
using FrappFresh.Application.DTOs.Payments;
using FrappFresh.Application.DTOs.Users;
using FrappFresh.Application.Exceptions;
using FrappFresh.Application.Interfaces;
using FrappFresh.Application.Validators;
using FrappFresh.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FrappFresh.Tests.Validators;

public class ApplicationValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    [InlineData("solominusculas1234")]
    [InlineData("SOLOMAYUSCULAS1234")]
    [InlineData("SinNingunNumero")]
    public void Registration_RejectsWeakPasswords(string password)
    {
        var dto = Registration();
        dto.Password = dto.ConfirmPassword = password;
        Assert.False(new RegisterRequestValidator().Validate(dto).IsValid);
    }

    [Fact]
    public void RegistrationAndUserValidators_ValidateProfilesAndRoles()
    {
        var validator = new RegisterRequestValidator();
        var request = Registration();
        Assert.True(validator.Validate(request).IsValid);
        request.ConfirmPassword = "Diferente123456";
        Assert.Contains(validator.Validate(request).Errors, x => x.PropertyName == "ConfirmPassword");

        Assert.False(new UpdateUserRequestValidator().Validate(new UpdateUserRequestDto()).IsValid);
        Assert.False(new ChangeUserRoleRequestValidator().Validate(new ChangeUserRoleRequestDto { Role = (UserRole)99 }).IsValid);
        Assert.False(new ChangePasswordRequestValidator().Validate(new ChangePasswordRequestDto
        {
            CurrentPassword = "MismaPassword123", NewPassword = "MismaPassword123", ConfirmPassword = "MismaPassword123"
        }).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1.001)]
    [InlineData(10000000000)]
    public void Frappe_RejectsInvalidPrices(double value)
    {
        var request = new FrappeRequestDto { Name = "Mango", Description = "Natural", Flavor = "Mango", Price = (decimal)value };
        Assert.False(new FrappeRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Frappe_RejectsUnknownBaseAndUnsafeImageUrl()
    {
        var request = new FrappeRequestDto
        {
            Name = "Mango", Description = "Natural", Flavor = "Mango", Price = 10000,
            Base = (BeverageBase)99, ImageUrl = "javascript:alert(1)"
        };
        var result = new FrappeRequestValidator().Validate(request);
        Assert.Contains(result.Errors, x => x.PropertyName == "Base");
        Assert.Contains(result.Errors, x => x.PropertyName == "ImageUrl");
    }

    [Fact]
    public void Order_RequiresAddressForDelivery_AndRejectsEmptyDuplicateOrNullLines()
    {
        var validator = new OrderRequestValidator();
        var request = Order();
        Assert.True(validator.Validate(request).IsValid);
        request.DeliveryType = DeliveryType.Delivery;
        Assert.False(validator.Validate(request).IsValid);
        request.DeliveryAddress = "Calle ficticia 123";
        Assert.True(validator.Validate(request).IsValid);
        request.Items.Add(request.Items[0]);
        Assert.False(validator.Validate(request).IsValid);
        request.Items = [null!];
        Assert.False(validator.Validate(request).IsValid);
        request.Items = null!;
        Assert.False(validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void OrderItem_RejectsInvalidQuantities(int quantity) =>
        Assert.False(new OrderItemRequestValidator().Validate(new OrderItemRequestDto { FrappeId = Guid.NewGuid(), Quantity = quantity }).IsValid);

    [Fact]
    public void PaymentAndLogin_RejectMalformedRequests()
    {
        Assert.False(new CreatePaymentRequestValidator().Validate(new CreatePaymentRequestDto
        {
            OrderId = Guid.Empty, Method = (PaymentMethod)99
        }).IsValid);
        Assert.False(new CancelPaymentRequestValidator().Validate(new CancelPaymentRequestDto { Reason = " " }).IsValid);
        Assert.False(new LoginRequestValidator().Validate(new LoginRequestDto { Email = "invalid", Password = "" }).IsValid);
        Assert.False(new RefreshTokenRequestValidator().Validate(new RefreshTokenRequestDto()).IsValid);
        Assert.False(new ChangeOrderStatusRequestValidator().Validate(new ChangeOrderStatusRequestDto { Status = (OrderStatus)99 }).IsValid);
    }

    [Fact]
    public async Task ValidationAdapter_ReturnsFieldErrorsAndHandlesMissingBody()
    {
        var services = new ServiceCollection();
        services.AddApplication(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IRequestValidator>();
        var error = await Assert.ThrowsAsync<RequestValidationException>(() => validator.ValidateAsync(new RegisterRequestDto()));
        Assert.Contains("Email", error.Errors.Keys);
        Assert.Contains("Password", error.Errors.Keys);
        await Assert.ThrowsAsync<RequestValidationException>(() => validator.ValidateAsync<RegisterRequestDto>(null!));
    }

    internal static RegisterRequestDto Registration() => new()
    {
        FirstName = "Cliente", LastName = "Prueba", Email = $"{Guid.NewGuid():N}@test.local",
        PhoneNumber = "573001234567", Password = "PasswordDePrueba123!", ConfirmPassword = "PasswordDePrueba123!"
    };

    internal static OrderRequestDto Order(Guid? frappeId = null) => new()
    {
        CustomerName = "Cliente Prueba", CustomerPhone = "573001234567",
        Items = [new OrderItemRequestDto { FrappeId = frappeId ?? Guid.NewGuid(), Quantity = 2 }]
    };
}
