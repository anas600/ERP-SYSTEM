using FluentValidation;

namespace ERPSystem.Modules.Inventory.Application;

public sealed class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEqual(Guid.Empty);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50)
            .Matches("^[A-Za-z0-9\\-_]+$").WithMessage("SKU: حروف/أرقام/-/_ فقط.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Barcode).MaximumLength(100);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StandardCost).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StandardCost).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEqual(Guid.Empty);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateUnitOfMeasureRequestValidator : AbstractValidator<CreateUnitOfMeasureRequest>
{
    public CreateUnitOfMeasureRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20)
            .Matches("^[A-Za-z0-9²³]+$").WithMessage("كود UoM: حروف/أرقام/²/³ فقط.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateItemCategoryRequestValidator : AbstractValidator<CreateItemCategoryRequest>
{
    public CreateItemCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
