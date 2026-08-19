using ERPSystem.Modules.Inventory.Entities;

namespace ERPSystem.Shared.SeedData;

/// <summary>
/// Seed افتراضي للمخزون لكل tenant جديد:
/// - 6 وحدات قياس (UoM)
/// - 5 تصنيفات أصناف (Categories) — شجرية (Raw Materials تحت Materials)
/// يُستدعى من ITenantBootstrap.OnTenantCreatedAsync
/// </summary>
public static class DefaultInventorySeed
{
    public static readonly (string Code, string Name, string? Symbol)[] DefaultUoMs =
    {
        ("pcs", "قطعة", "pcs"),
        ("kg", "كيلوغرام", "kg"),
        ("g", "غرام", "g"),
        ("ton", "طن", "ton"),
        ("m", "متر", "m"),
        ("cm", "سنتيمتر", "cm"),
        ("mm", "ميليمتر", "mm"),
        ("km", "كيلومتر", "km"),
        ("m2", "متر مربع", "m²"),
        ("m3", "متر مكعب", "m³"),
        ("l", "لتر", "l"),
        ("ml", "ميليلتر", "ml"),
        ("h", "ساعة", "h"),
        ("d", "يوم", "d"),
        ("set", "طقم", "set"),
        ("box", "صندوق", "box"),
        ("pkg", "عبوة", "pkg"),
        // Sprint 59 (DEC-179): construction-specific units (لائحة 355 لسنة 2026)
        ("mlt", "متر طولي", "م.ط"),
        ("lump", "مقطوعية", "مقطوعية"),
        ("ea", "عدد", "عدد"),
    };

    public static readonly (string Code, string Name, string? Description, string? ParentCode)[] DefaultCategories =
    {
        ("RM", "المواد الخام", "مواد خام تدخل في الإنتاج", null),
        ("FG", "المنتجات النهائية", "منتجات جاهزة للبيع", null),
        ("CON", "مواد استهلاكية", "مواد تُستهلك ولا تُنتج", null),
        ("SVC", "خدمات", "خدمات (لا مخزون فعلي)", null),
        ("OFF", "لوازم مكتبية", "قرطاسية ولوازم إدارية", null),
    };
}
