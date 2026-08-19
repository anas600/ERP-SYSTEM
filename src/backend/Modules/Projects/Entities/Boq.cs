using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// Sprint 59 (DEC-181): Bill of Quantities (مقايسة / حصر) لمشروع مقاولات.
/// كل قسم (section) يحوي عدة بنود (lines)، كل بند له sub-items بحسابات L×W×H.
/// </summary>
public class BoqSection
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public string Code { get; set; } = string.Empty;   // "1", "2", "3"
    public string Name { get; set; } = string.Empty;   // "أعمال الإزالة"
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class BoqLine
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid SectionId { get; set; }
    public Guid? PriceListItemId { get; set; }
    public string Code { get; set; } = string.Empty;     // "1.1.1.1"
    public string Description { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public decimal ContractQty { get; set; }              // كمية العقد
    public decimal ExecutedQty { get; set; }              // كمية منفذة
    public decimal UnitPrice { get; set; }                // سعر مرجعي
    public decimal RegionalPremiumPct { get; set; }       // علاوة المنطقة %
    public decimal FinalUnitPrice { get; set; }           // سعر نهائي بعد العلاوة
    public decimal TotalAmount { get; set; }              // = ContractQty × FinalUnitPrice
    public bool IsMeasurable { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BoqSubitem
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BoqLineId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
    public decimal LengthM { get; set; }
    public decimal WidthM { get; set; }
    public decimal HeightM { get; set; }
    public decimal InitialQty { get; set; }               // = count × L × W × H
    public decimal Deductions { get; set; }
    public decimal FinalQty { get; set; }                 // = initial - deductions
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
