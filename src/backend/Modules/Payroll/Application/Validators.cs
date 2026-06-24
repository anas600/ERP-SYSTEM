using FluentValidation;

namespace ERPSystem.Modules.Payroll.Application;

/// <summary>تحقّق من طلب إنشاء دورة رواتب — period_start &lt; period_end، future guard اختياري.</summary>
public sealed class CreatePayrollRunRequestValidator : AbstractValidator<CreatePayrollRunRequest>
{
    public CreatePayrollRunRequestValidator()
    {
        RuleFor(x => x.PeriodStart).NotEmpty().WithMessage("تاريخ البداية مطلوب.");
        RuleFor(x => x.PeriodEnd).NotEmpty().WithMessage("تاريخ النهاية مطلوب.")
            .GreaterThanOrEqualTo(x => x.PeriodStart.Date)
            .WithMessage("تاريخ النهاية يجب أن يكون >= تاريخ البداية.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}