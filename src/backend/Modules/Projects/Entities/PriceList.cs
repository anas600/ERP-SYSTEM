using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// Sprint 59 (DEC-180): لائحة أسعار مرجعية لمشاريع المقاولات.
/// مثال: لائحة 355 لسنة 2026 الصادرة عن مجلس الوزراء الليبي.
/// كل شركة (company) عندها نسختها من اللائحة — يمكن تعديل الأسعار.
/// </summary>
public class PriceList
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;   // "355-2026"
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IssuedBy { get; set; }               // "مجلس الوزراء"
    public DateTime? IssuedAt { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PriceListItem
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PriceListId { get; set; }
    public string Code { get; set; } = string.Empty;       // "1.1.1.1"
    public string? ParentCode { get; set; }                // "1.1.1" (for hierarchy)
    public string Description { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Section { get; set; }                  // Buildings, Roads, Water, etc.
    public string? Category { get; set; }                 // Material, Labor, Equipment, etc.
    public int Level { get; set; } = 4;                   // 1=Chapter, 2=Section, 3=Sub, 4=Line
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
