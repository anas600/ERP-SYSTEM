namespace ERPSystem.Shared.SeedData;

/// <summary>
/// شجرة الحسابات الموحدة — تطبَّق على Holding + كل الشركات الفرعية.
/// الأكواد متّسقة مع التقارير (BS, IS, CF) في Sprint 48:
///   11xx: Cash &amp; Bank          (Asset, Dr)
///   12xx: Accounts Receivable    (Asset, Dr)
///   13xx: Inventory              (Asset, Dr)
///   14xx: Prepaid + Other CA     (Asset, Dr)
///   15xx: Fixed Assets           (Asset, Dr)
///   16xx: Accumulated Depreciation (Asset, Cr) — contra-asset
///   21xx: Accounts Payable       (Liability, Cr)
///   22xx: Accrued + VAT Payable  (Liability, Cr)
///   23xx: Short-term Loans       (Liability, Cr)
///   24xx: Long-term Loans        (Liability, Cr)
///   31xx: Capital                (Equity, Cr)
///   32xx: Retained Earnings + Drawings (Equity, Cr)
///   41xx: Sales Revenue          (Revenue, Cr)
///   42xx: Service Revenue        (Revenue, Cr)
///   49xx: Other Income / Sales Returns (Revenue, Cr)
///   51xx: COGS                   (Expense, Dr)
///   52xx: Salaries &amp; Wages          (Expense, Dr)
///   53xx: Depreciation Expense   (Expense, Dr)
///   54xx: Rent + Utilities       (Expense, Dr)
///   55xx: Marketing &amp; Admin          (Expense, Dr)
///   56xx: Finance Costs          (Expense, Dr)
/// </summary>
public static class UnifiedCoA
{
    public sealed class Account
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        /// <summary>1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense</summary>
        public int AccountType { get; set; }
        /// <summary>1=Debit, 2=Credit</summary>
        public int NormalBalance { get; set; }
    }

    public static IReadOnlyList<Account> GetAccounts() => new List<Account>
    {
        // ============ 11xx — Cash & Bank ============
        new() { Code = "1100", Name = "الصندوق (النقدية)", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1110", Name = "البنك — حساب جاري", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1120", Name = "البنك — ودائع", AccountType = 1, NormalBalance = 1 },

        // ============ 12xx — Accounts Receivable ============
        new() { Code = "1200", Name = "الذمم المدينة (العملاء)", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1290", Name = "مخصص ديون مشكوك فيها", AccountType = 1, NormalBalance = 2 },

        // ============ 13xx — Inventory ============
        new() { Code = "1300", Name = "المخزون — بضاعة جاهزة", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1310", Name = "المخزون — مواد أولية", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1320", Name = "المخزون في الطريق", AccountType = 1, NormalBalance = 1 },

        // ============ 14xx — Prepaid + Other CA ============
        new() { Code = "1400", Name = "مصروفات مقدمة", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1410", Name = "تأمينات مستردة", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1420", Name = "ضريبة القيمة المضافة — مدخلات (VAT Input)", AccountType = 1, NormalBalance = 1 },

        // ============ 15xx — Fixed Assets ============
        new() { Code = "1500", Name = "أراضي", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1510", Name = "مباني", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1520", Name = "أجهزة ومعدات", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1530", Name = "سيارات", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1540", Name = "أثاث ومفروشات", AccountType = 1, NormalBalance = 1 },
        new() { Code = "1550", Name = "برامج كمبيوتر", AccountType = 1, NormalBalance = 1 },

        // ============ 16xx — Accumulated Depreciation (Contra-Asset, Cr) ============
        new() { Code = "1600", Name = "مجمع إهلاك المباني", AccountType = 1, NormalBalance = 2 },
        new() { Code = "1610", Name = "مجمع إهلاك الأجهزة والمعدات", AccountType = 1, NormalBalance = 2 },
        new() { Code = "1620", Name = "مجمع إهلاك السيارات", AccountType = 1, NormalBalance = 2 },
        new() { Code = "1630", Name = "مجمع إهلاك الأثاث", AccountType = 1, NormalBalance = 2 },
        new() { Code = "1640", Name = "مجمع إهلاك البرامج", AccountType = 1, NormalBalance = 2 },

        // ============ 21xx — Accounts Payable ============
        new() { Code = "2100", Name = "الذمم الدائنة (الموردين)", AccountType = 2, NormalBalance = 2 },
        new() { Code = "2110", Name = "سلف من العملاء", AccountType = 2, NormalBalance = 2 },

        // ============ 22xx — Accrued + VAT Payable ============
        new() { Code = "2200", Name = "مصروفات مستحقة", AccountType = 2, NormalBalance = 2 },
        new() { Code = "2210", Name = "رواتب مستحقة", AccountType = 2, NormalBalance = 2 },
        new() { Code = "2220", Name = "ضريبة القيمة المضافة — مخرجات (VAT Output)", AccountType = 2, NormalBalance = 2 },
        new() { Code = "2230", Name = "ضريبة الدخل المستحقة", AccountType = 2, NormalBalance = 2 },

        // ============ 23xx — Short-term Loans ============
        new() { Code = "2300", Name = "قروض قصيرة الأجل — بنوك", AccountType = 2, NormalBalance = 2 },
        new() { Code = "2310", Name = "أوراق دفع", AccountType = 2, NormalBalance = 2 },

        // ============ 24xx — Long-term Loans ============
        new() { Code = "2400", Name = "قروض طويلة الأجل — بنوك", AccountType = 2, NormalBalance = 2 },

        // ============ 31xx — Capital ============
        new() { Code = "3100", Name = "رأس المال", AccountType = 3, NormalBalance = 2 },
        new() { Code = "3110", Name = "احتياطي قانوني", AccountType = 3, NormalBalance = 2 },
        new() { Code = "3120", Name = "احتياطي عام", AccountType = 3, NormalBalance = 2 },

        // ============ 32xx — Retained Earnings + Drawings ============
        new() { Code = "3200", Name = "أرباح مبقاة (سنوات سابقة)", AccountType = 3, NormalBalance = 2 },
        new() { Code = "3210", Name = "أرباح السنة الحالية (ملخّص الدخل)", AccountType = 3, NormalBalance = 2 },
        new() { Code = "3300", Name = "مسحوبات المالك", AccountType = 3, NormalBalance = 1 },

        // ============ 41xx — Sales Revenue ============
        new() { Code = "4100", Name = "إيرادات المبيعات", AccountType = 4, NormalBalance = 2 },
        new() { Code = "4110", Name = "إيرادات المبيعات — بضاعة", AccountType = 4, NormalBalance = 2 },
        new() { Code = "4120", Name = "إيرادات المبيعات — خدمات", AccountType = 4, NormalBalance = 2 },

        // ============ 42xx — Service Revenue ============
        new() { Code = "4200", Name = "إيرادات الخدمات", AccountType = 4, NormalBalance = 2 },
        new() { Code = "4210", Name = "إيرادات استشارات", AccountType = 4, NormalBalance = 2 },

        // ============ 49xx — Other Income / Sales Returns ============
        new() { Code = "4900", Name = "إيرادات أخرى", AccountType = 4, NormalBalance = 2 },
        new() { Code = "4910", Name = "إيرادات فوائد بنكية", AccountType = 4, NormalBalance = 2 },
        new() { Code = "4950", Name = "مردودات ومسموحات مبيعات", AccountType = 4, NormalBalance = 1 }, // Dr normal — reduce revenue

        // ============ 51xx — COGS ============
        new() { Code = "5100", Name = "تكلفة البضاعة المباعة (COGS)", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5110", Name = "تكلفة الخدمات المباعة", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5150", Name = "مردودات ومسموحات مشتريات (عكس)", AccountType = 5, NormalBalance = 2 }, // Cr normal — reduce COGS

        // ============ 52xx — Salaries & Wages ============
        new() { Code = "5200", Name = "رواتب وأجور", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5210", Name = "تأمينات اجتماعية (حصة الشركة)", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5220", Name = "بدلات ومكافآت", AccountType = 5, NormalBalance = 1 },

        // ============ 53xx — Depreciation Expense ============
        new() { Code = "5300", Name = "مصروف إهلاك المباني", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5310", Name = "مصروف إهلاك الأجهزة والمعدات", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5320", Name = "مصروف إهلاك السيارات", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5330", Name = "مصروف إهلاك الأثاث", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5340", Name = "مصروف إهلاك البرامج", AccountType = 5, NormalBalance = 1 },

        // ============ 54xx — Rent + Utilities ============
        new() { Code = "5400", Name = "إيجار", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5410", Name = "كهرباء وماء", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5420", Name = "اتصالات وإنترنت", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5430", Name = "وقود وزيوت", AccountType = 5, NormalBalance = 1 },

        // ============ 55xx — Marketing & Admin ============
        new() { Code = "5500", Name = "تسويق وإعلان", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5510", Name = "مستلزمات مكتبية", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5520", Name = "أتعاب مهنية (محاسبة، قانونية)", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5530", Name = "تأمين", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5540", Name = "صيانة وإصلاحات", AccountType = 5, NormalBalance = 1 },

        // ============ 56xx — Finance Costs ============
        new() { Code = "5600", Name = "فوائد بنكية", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5610", Name = "رسوم بنكية", AccountType = 5, NormalBalance = 1 },
        new() { Code = "5620", Name = "فروقات عملة", AccountType = 5, NormalBalance = 1 },
    };
}
