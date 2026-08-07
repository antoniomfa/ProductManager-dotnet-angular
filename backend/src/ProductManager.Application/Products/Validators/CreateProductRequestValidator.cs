using FluentValidation;
using ProductManager.Application.Products.Dtos;
using ProductManager.Domain.Entities;

namespace ProductManager.Application.Products.Validators;

/// <summary>
/// Validates POST /api/products payloads. Structural rules live here so a bad request fails
/// before any database work happens; the domain re-checks its own invariants independently.
/// </summary>
public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
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

        RuleFor(request => request.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Initial stock cannot be negative.")
            .LessThanOrEqualTo(ProductValidationRules.MaxStock);

        RuleFor(request => request.LowStockThreshold)
            .GreaterThanOrEqualTo(0).WithMessage("Low-stock threshold cannot be negative.")
            .LessThanOrEqualTo(ProductValidationRules.MaxStock);

        RuleFor(request => request.CategoryId)
            .GreaterThan(0).WithMessage("A valid category must be selected.");
    }
}
