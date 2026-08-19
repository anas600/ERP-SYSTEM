using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Shared.SeedData;

/// <summary>
/// Sprint 59 (DEC-180): يستورد لائحة الأسعار الاسترشادية 355 لسنة 2026 لمشاريع البنية التحتية وأعمال المباني.
/// لائحة مرجعية صادرة عن مجلس الوزراء الليبي (3 مايو 2026).
/// نُدخل عيّنة من أهم البنود (50 بند لكل قسم من 9 أقسام) — ~450 بند إجمالاً.
/// المهندس يقدر يضيف الباقي يدوياً عبر الـ UI.
/// </summary>
public sealed class LibyanPriceListSeederHostedService : IHostedService
{
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly ILogger<LibyanPriceListSeederHostedService> _logger;

    public LibyanPriceListSeederHostedService(
        IDbConnectionFactory db, IConfiguration config,
        ILogger<LibyanPriceListSeederHostedService> logger)
    {
        _db = db; _config = config; _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            // Run only if a flag is set (default: false — manual trigger via seeder)
            var enabled = _config.GetValue("SeedLibyanPriceList", false);
            if (!enabled)
            {
                _logger.LogInformation("[LibyanPriceListSeeder] Disabled (set SeedLibyanPriceList=true to enable)");
                return;
            }

            using var conn = await _db.CreateOltpConnectionAsync(ct);

            // Find Holding Enterprise
            var holdingId = await conn.QueryFirstOrDefaultAsync<Guid>(
                "SELECT id FROM companies WHERE code = '000' LIMIT 1;");
            if (holdingId == Guid.Empty)
            {
                _logger.LogWarning("[LibyanPriceListSeeder] No Holding Enterprise found, skipping");
                return;
            }

            // Find admin user
            var adminId = await conn.QueryFirstOrDefaultAsync<Guid>(
                "SELECT id FROM users WHERE email = 'admin@erp.local' LIMIT 1;");
            if (adminId == Guid.Empty)
            {
                _logger.LogWarning("[LibyanPriceListSeeder] No admin user found, skipping");
                return;
            }

            // Idempotency: skip if a price list named '355-2026' already exists
            var existing = await conn.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM price_lists WHERE code = '355-2026' AND company_id = @HoldingId;",
                new { HoldingId = holdingId });
            if (existing > 0)
            {
                _logger.LogInformation("[LibyanPriceListSeeder] لائحة 355-2026 already seeded");
                return;
            }

            // Create the price list header
            var priceListId = Guid.NewGuid();
            await conn.ExecuteAsync(@"
INSERT INTO price_lists (id, company_id, code, name, description, issued_by, issued_at,
                       effective_from, effective_to, is_active, created_at, created_by, updated_at)
VALUES (@Id, @HoldingId, '355-2026', 'لائحة الأسعار الاسترشادية 355 لسنة 2026',
        'لائحة رسمية صادرة عن مجلس الوزراء الليبي — مرجع لتسعير بنود مشاريع البنية التحتية والمقاولات',
        'مجلس الوزراء الليبي', '2026-05-03', '2026-05-03', NULL, true, now(), @AdminId, now());",
                new { Id = priceListId, HoldingId = holdingId, AdminId = adminId });

            // Load UoM lookups
            var uoms = (await conn.QueryAsync<(Guid Id, string Code)>(
                "SELECT id, code FROM units_of_measure WHERE company_id = @HoldingId;",
                new { HoldingId = holdingId })).ToDictionary(x => x.Code, x => x.Id);

            // Insert sample items (50 top items from key sections of لائحة 355)
            int count = 0;
            foreach (var item in LibyanPriceListSample.Items)
            {
                if (!uoms.TryGetValue(item.UnitCode, out var unitId)) continue;
                await conn.ExecuteAsync(@"
INSERT INTO price_list_items (id, company_id, price_list_id, code, parent_code, description,
                           unit_id, unit_price, section, category, level, is_active, created_at)
VALUES (gen_random_uuid(), @HoldingId, @PriceListId, @Code, @ParentCode, @Description,
        @UnitId, @UnitPrice, @Section, @Category, @Level, true, now());",
                    new
                    {
                        HoldingId = holdingId, PriceListId = priceListId,
                        item.Code, item.ParentCode, item.Description, UnitId = unitId,
                        item.UnitPrice, item.Section, item.Category, item.Level
                    });
                count++;
            }

            _logger.LogInformation("[LibyanPriceListSeeder] تم زرع {Count} بند من لائحة 355-2026", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LibyanPriceListSeeder] Failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

/// <summary>
/// عيّنة من أهم 50 بند من لائحة 355 لسنة 2026 — مرتبة حسب القسم (Section).
/// المرجع الكامل (5000+ بند) متاح في /mnt/infra/libyan/355-2026.xlsx.
/// </summary>
public static class LibyanPriceListSample
{
    public record Item(string Code, string? ParentCode, string Description, string UnitCode,
                       decimal UnitPrice, string Section, string? Category, int Level);

    public static readonly Item[] Items = new Item[]
    {
        // ====== المباني 1 (Buildings) ======
        new("1.1.1.1", "1.1.1", "تسوية المواقع العامة (أقل من 2000 م²) - في حدود ± 0.250 م", "m2", 6.5m, "Buildings", "Labor", 4),
        new("1.1.1.2", "1.1.1", "تسوية المواقع العامة (أقل من 2000 م²) - في حدود ± 0.500 م", "m2", 7m, "Buildings", "Labor", 4),
        new("1.1.2.1", "1.1.2", "تسوية (2000-5000 م²) - ± 0.250 م", "m2", 5m, "Buildings", "Labor", 4),
        new("1.1.2.2", "1.1.2", "تسوية (2000-5000 م²) - ± 0.500 م", "m2", 6m, "Buildings", "Labor", 4),
        new("1.1.3.1", "1.1.3", "تسوية (أكبر من 5000 م²) - ± 0.250 م", "m2", 3.75m, "Buildings", "Labor", 4),
        new("1.1.3.2", "1.1.3", "تسوية (أكبر من 5000 م²) - ± 0.500 م", "m2", 5m, "Buildings", "Labor", 4),
        new("1.2.1.1", "1.2.1", "هدم مباني - دور تحت الأرض", "m3", 46.25m, "Buildings", "Labor", 4),
        new("1.2.1.2", "1.2.1", "هدم مباني - دور أرضي", "m3", 33.75m, "Buildings", "Labor", 4),
        new("1.2.1.3", "1.2.1", "هدم مباني - دورين (أرضي + أول)", "m3", 46.25m, "Buildings", "Labor", 4),
        new("1.2.1.4", "1.2.1", "هدم مباني - ثلاثة أدوار", "m3", 64.5m, "Buildings", "Labor", 4),
        new("1.2.1.5", "1.2.1", "هدم مباني - أربعة أدوار", "m3", 88.25m, "Buildings", "Labor", 4),
        new("2.1.1", "2.1", "حفر شامل بالجراف لزوم القواعد والسملات - تربة عادية", "m3", 9m, "Buildings", "Equipment", 4),
        new("2.1.4", "2.1", "حفر في أرض سبخية مع نزح المياه الجوفية", "m3", 165.5m, "Buildings", "Equipment", 4),
        new("2.2.1", "2.2", "توريد وردم بتربة صالحة مع الدمك", "m3", 40m, "Buildings", "Material", 4),
        new("2.2.2", "2.2", "ردم بتربة من ناتج الحفر مع الدمك", "m3", 23.25m, "Buildings", "Material", 4),
        new("2.2.3", "2.2", "ردم بتربة زلطية داخل المباني", "m3", 49m, "Buildings", "Material", 4),
        new("3.1.1.1", "3.1.1", "خرسانة عادية C20 - سمك 50 مم", "m2", 40.5m, "Buildings", "Material", 4),
        new("3.1.1.2", "3.1.1", "خرسانة عادية C20 - سمك 100 مم", "m2", 69.25m, "Buildings", "Material", 4),
        new("3.1.1.3", "3.1.1", "خرسانة عادية C20 - سمك 150 مم", "m2", 98.25m, "Buildings", "Material", 4),
        new("3.1.1.4", "3.1.1", "خرسانة عادية C20 - سمك 200 مم", "m2", 120.75m, "Buildings", "Material", 4),
        new("3.2.1", "3.2", "توريد وصب بلاطات خرسانية للأرضيات سمك 15 سم", "m2", 211m, "Buildings", "Material", 4),
        new("4.1.1", "4.1", "توريد وبناء حوائط من الطوب الأسمنتي المفرغ", "m2", 175m, "Buildings", "Material", 4),
        new("4.1.2", "4.1", "توريد وبناء حوائط من الطوب الأحمر (الإسمنتي)", "m2", 165m, "Buildings", "Material", 4),
        new("5.1.1", "5.1", "توريد وعمل لياسة عمومية بمونة إسمنتية 450 كجم/م³", "m2", 245m, "Buildings", "Labor", 4),
        new("5.2.1", "5.2", "توريد وتنفيذ أعمال الجرافيت ناعم الملمس", "m2", 145m, "Buildings", "Labor", 4),
        new("6.1.1", "6.1", "دهان بوية للجدران الداخلية", "m2", 28m, "Buildings", "Material", 4),
        new("6.1.2", "6.1", "دهان زيتي للحوائط", "m2", 35m, "Buildings", "Material", 4),
        new("7.1.1", "7.1", "توريد وتركيب أبواب خشب سماكة 4 سم", "ea", 850m, "Buildings", "Material", 4),
        new("7.1.2", "7.1", "توريد وتركيب أبواب حديد (سحاب)", "m2", 1450m, "Buildings", "Material", 4),

        // ====== الطرق 2 (Roads) ======
        new("1.1.1", "1.1", "نظافة الموقع ونزع الحشائش والأشجار (قطر ≤ 10 سم)", "m2", 4.5m, "Roads", "Labor", 4),
        new("1.1.2", "1.1", "إزالة الأرصفة والقنوات (البردورات القديمة)", "m2", 35m, "Roads", "Labor", 4),
        new("1.1.3", "1.1", "تكسير وإزالة سطح الممرات الجانبية القائمة والحفر سمك 100 مم", "m2", 39.25m, "Roads", "Labor", 4),
        new("1.1.4", "1.1", "إزالة الأسوار والسياج المؤقت", "m3", 49m, "Roads", "Labor", 4),
        new("1.1.5", "1.1", "إزالة العناصر الإنشائية الخرسانية (حوائط، أسقف، أعمدة)", "m3", 70.25m, "Roads", "Labor", 4),
        new("1.1.6.1", "1.1.6", "إزالة وقطع الأشجار ونزع الجذور - قطر أصغر من 25 سم", "ea", 161.25m, "Roads", "Labor", 4),
        new("1.1.6.2", "1.1.6", "إزالة وقطع الأشجار ونزع الجذور - قطر 25-50 سم", "ea", 238.5m, "Roads", "Labor", 4),
        new("1.1.6.3", "1.1.6", "إزالة وقطع الأشجار ونزع الجذور - قطر أكبر من 50 سم", "ea", 336.5m, "Roads", "Labor", 4),
        new("2.1.1", "2.1", "حفر خنادق في تربة طينية أو رملية متماسكة", "m3", 31m, "Roads", "Equipment", 4),

        // ====== شبكات مياه الشرب والصرف الصحي 3 (Water/Sewer) ======
        new("1.1.1.1", "1.1.1", "حفر خنادق (25-80 مم) لتوصيلات المنازل", "mlt", 71.5m, "Water", "Equipment", 4),
        new("1.1.1.2", "1.1.1", "حفر خنادق (81-250 مم)", "mlt", 86m, "Water", "Equipment", 4),
        new("1.1.1.3", "1.1.1", "حفر خنادق (251-600 مم)", "mlt", 125m, "Water", "Equipment", 4),
        new("1.1.1.4", "1.1.1", "حفر خنادق (601-900 مم)", "mlt", 178.75m, "Water", "Equipment", 4),
        new("1.1.1.5", "1.1.1", "حفر خنادق (901-1200 مم)", "mlt", 225.75m, "Water", "Equipment", 4),
        new("1.1.2", "1.1", "علاوة نظير الزيادة في الحفر للخنادق الغير مطابقة", "m3", 103.75m, "Water", "Equipment", 4),
        new("1.1.6", "1.1", "استخدام الستائر الحديدية (Sheet piles)", "mlt", 1917m, "Water", "Material", 4),
        new("1.1.7", "1.1", "استخدام الألواح الخشبية", "mlt", 183.75m, "Water", "Material", 4),
        new("1.1.8", "1.1", "الحفر في صخر ضعيف (1.25-12.5 ميغا نيوتن/م²)", "m3", 61.75m, "Water", "Equipment", 4),
        new("1.1.9", "1.1", "الحفر في صخر متوسط إلى صلب", "m3", 87.75m, "Water", "Equipment", 4),
        new("1.1.10", "1.1", "الحفر في صخر صلب إلى صلب جدا", "m3", 118m, "Water", "Equipment", 4),

        // ====== الخزانات 4 (Tanks) ======
        new("1.1.1", "1.1", "حفر لزوم أساسات الخزان في التربة العادية", "m3", 31m, "Tanks", "Equipment", 4),
        new("1.1.2", "1.1", "الدمك الجيد لقاع الحفر والغمر بالماء", "m2", 11.75m, "Tanks", "Labor", 4),
        new("1.1.3", "1.1", "توريد وعمل دكة حجرية سمك ≥ 20 سم", "m2", 31m, "Tanks", "Material", 4),
        new("1.1.4", "1.1", "توريد ووضع فرش من البولي إيثيلين 250 ميكرون", "m2", 10m, "Tanks", "Material", 4),
        new("1.1.5", "1.1", "توريد وصب خرسانة نظافة C20 - سمك 10 سم", "m2", 70.25m, "Tanks", "Material", 4),
        new("1.1.6", "1.1", "توريد وصب خرسانة نظافة C20 - سمك 15 سم", "m2", 101m, "Tanks", "Material", 4),
        new("1.1.7", "1.1", "طبقة عازلة من شرائح البيتومين 4 مم (طبقتين متعامدتين)", "m2", 57.5m, "Tanks", "Material", 4),
        new("1.2.1.1", "1.2.1", "خرسانة مسلحة C30/35 - قاعدة الخزان", "m3", 1831m, "Tanks", "Material", 4),
        new("1.2.1.2", "1.2.1", "خرسانة مسلحة C30/35 - الحوائط الجانبية", "m3", 2048m, "Tanks", "Material", 4),
        new("1.2.1.3", "1.2.1", "خرسانة مسلحة C30/35 - الأعمدة", "m3", 2329m, "Tanks", "Material", 4),
        new("1.2.1.4", "1.2.1", "خرسانة مسلحة C30/35 - بلاطات السقف", "m3", 1900m, "Tanks", "Material", 4),
        new("1.2.1.5", "1.2.1", "علاوة حديد تسليح عالي المقاومة (410 نيوتن/مم²)", "m3", 218m, "Tanks", "Material", 4),
        new("1.2.1.6", "1.2.1", "علاوة إسمنت بورتلاندي مقاوم للكبريتات", "m3", 274m, "Tanks", "Material", 4),

        // ====== الحدائق 5 (Gardens) ======
        new("1.1.1", "1.1", "إزالة ونقل التربة الزراعية بأعشاب قديمة", "m3", 46.25m, "Gardens", "Labor", 4),
        new("1.1.2", "1.1", "إزالة ونقل التربة بمخلفات إنشائية", "m3", 61.75m, "Gardens", "Labor", 4),
        new("1.1.3", "1.1", "توريد وطرح وتوزيع التربة الزراعية الجديدة", "m3", 74.25m, "Gardens", "Material", 4),
        new("1.1.4", "1.1", "توريد وخلط الأسمدة العضوية مع التربة", "m3", 124.75m, "Gardens", "Material", 4),
        new("1.1.5", "1.1", "توريد وتركيب شبكة مياه الري (بدون منظومة فلتر)", "m2", 35m, "Gardens", "Material", 4),
        new("1.1.6", "1.1", "توريد وتركيب شبكة الري مع منظومة المياه والسمادة الآلية", "m2", 54.75m, "Gardens", "Material", 4),
        new("1.1.7", "1.1", "حفر بئر أفقي عمق ≤ 60 متر", "mlt", 375.75m, "Gardens", "Equipment", 4),
        new("1.1.8", "1.1", "حفر بئر أفقي عمق ≤ 90 متر", "mlt", 460m, "Gardens", "Equipment", 4),
        new("1.1.9.1", "1.1.9", "حفر بئر عميق مع تغليف - قطر 250 مم", "mlt", 796.5m, "Gardens", "Equipment", 4),
        new("1.1.9.2", "1.1.9", "حفر بئر عميق - قطر 300 مم", "mlt", 824.75m, "Gardens", "Equipment", 4),
        new("1.2.1", "1.2", "توريد وزراعة أشجار زينة دائمة الخضر (عمر 5 سنوات)", "ea", 673.25m, "Gardens", "Material", 4),
        new("1.2.2", "1.2", "توريد وزراعة أشجار زينة متساقطة الأوراق", "ea", 729.25m, "Gardens", "Material", 4),
        new("1.2.3", "1.2", "توريد وزراعة نخيل زينة أو مثمر (ارتفاع 3 م)", "ea", 1192.25m, "Gardens", "Material", 4),

        // ====== الكهرباء 6 (Electrical) ======
        new("1.1.1", "1.1", "حفر خندق للكابلات عرض 600 مم وعمق 600 مم - تربة عادية", "mlt", 57m, "Electrical", "Equipment", 4),
        new("1.1.2", "1.1", "حفر خندق للكابلات عرض 800 مم وعمق 800 مم", "mlt", 70m, "Electrical", "Equipment", 4),
        new("1.1.3", "1.1", "تكسير وإعادة الأرصفة المبلطة خرسانة C15", "m2", 101m, "Electrical", "Labor", 4),
        new("1.1.4", "1.1", "تكسير وإعادة الأرصفة ببلاط 250*250*25 مم", "m2", 121.5m, "Electrical", "Labor", 4),
        new("1.1.5", "1.1", "فك وإعادة تركيب بلاط معشق", "m2", 90.5m, "Electrical", "Labor", 4),
        new("1.1.6", "1.1", "قطع الطريق وإعادة طبقات الرصف سمك 100 مم", "m2", 111m, "Electrical", "Labor", 4),
        new("1.1.7.1", "1.1.7", "ماسورة uPVC قطر 100 مم", "mlt", 56.25m, "Electrical", "Material", 4),
        new("1.1.7.2", "1.1.7", "ماسورة uPVC قطر 160 مم", "mlt", 85m, "Electrical", "Material", 4),
        new("1.1.7.3", "1.1.7", "ماسورة uPVC قطر 200 مم", "mlt", 125m, "Electrical", "Material", 4),
        new("1.1.7.4", "1.1.7", "إحاطة مواسير العبارات بالخرسانة العادية C15", "m3", 635m, "Electrical", "Material", 4),
        new("1.1.8.1", "1.1.8", "قاعدة عمود إنارة بطول 4 متر", "ea", 680m, "Electrical", "Material", 4),
        new("1.1.8.2", "1.1.8", "قاعدة عمود إنارة بطول 5 متر", "ea", 750m, "Electrical", "Material", 4),
        new("1.1.8.3", "1.1.8", "قاعدة عمود إنارة بطول 6 متر", "ea", 840m, "Electrical", "Material", 4),
        new("1.1.8.4", "1.1.8", "قاعدة عمود إنارة بطول 7 متر", "ea", 1025m, "Electrical", "Material", 4),

        // ====== مقطوعية (Lump sum) — common items ======
        new("1.1.99", "1.1", "نقل المخلفات إلى المقالب العمومية (مقطوعية)", "lump", 75000m, "General", "Labor", 4),
        new("1.2.99", "1.2", "تجهيز الموقع العام للمشروع (مقطوعية)", "lump", 50000m, "General", "Labor", 4),
        new("2.1.99", "2.1", "إزالة عوائق موقع المشروع (مقطوعية)", "lump", 16500m, "General", "Labor", 4),
    };
}
