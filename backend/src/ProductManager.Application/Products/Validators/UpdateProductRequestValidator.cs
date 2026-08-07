using FluentValidation;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Entities;

namespace ProductManager.Application.Products.Validators;

/// <summary>Validates PUT /api/products/{id} payloads.</summary>
public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(Product.NameMaxLength);

        RuleFor(request => request.Description)
            .MaximumLength(Product.DescriptionMaxLength);

        RuleFor(request => request.Price)
            .GreaterThanOrEqualTo(0m).WithMessage("Price cannot be negative.")
            .LessThanOrEqualTo(ProductValidationRules.MaxPrice)
            .WithMessage($"Price cannot exceed {ProductValidationRules.MaxPrice:N2}.")
            .Must(ProductValidationRules.HasAtMostTwoDecimals)
            .WithMessage("Price cannot have more than two decimal places.");

        RuleFor(request => request.LowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("Low-stock threshold cannot be negative.")
            .LessThanOrEqualTo(ProductValidationRules.MaxStock);

        RuleFor(request => request.CategoryId)
            .GreaterThan(0).WithMessage("A valid category must be selected.");

        // Only enforced when a stock take is actually being submitted.
        RuleFor(request => request.StockQuantity!.Value)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.")
            .LessThanOrEqualTo(ProductValidationRules.MaxStock)
            .When(request => request.StockQuantity.HasValue);
    }
}
