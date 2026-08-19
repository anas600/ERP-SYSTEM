// ============================================================================
// ConstructionPostingRulesSeederHostedService — Sprint 59 (DEC-183)
//
// Seeds the 5 default posting rules for the Libyan construction cycle
// (NDB / لائحة 355). All 5 rules are INACTIVE by default — the admin enables
// them when the company is ready to start the construction cycle.
//
// Why INACTIVE: the 4-level CoA (DEC-58) has 2106/2107 + 9201 WIP accounts,
// but the FE + service layer for the events (AdvanceReceived, ProgressBilling,
// RetentionReleased, etc.) lands in Sprint 60-61. Until then, these rules are
// documentation — they show in /admin/posting-rules but won't fire.
//
// Event type mapping (matches TriggeringEvent enum in PostingRule.cs):
//   7 = AdvanceReceived       — Dr Cash / Cr 2106 (advance received from NDB)
//   8 = ProgressBillingPosted — 4 lines (AR / Revenue / WIP / Advance / Retention)
//   9 = RetentionReleased     — Dr 2107 (Retention Payable) / Cr Cash (at final delivery)
//  10 = VariationOrderApproved — Off-balance WIP add (no GL; tracked in contract table)
//  11 = ContractCompleted     — WIP → P&L: Dr 9201 / Cr 5100 + Dr AR / Cr 9201
//
// Reference: docs/plans/sprint-59-construction-core.md § Phase 3
// ============================================================================

using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text.Json;

namespace ERPSystem.Shared.SeedData;

public class ConstructionPostingRulesSeederHostedService : IHostedService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<ConstructionPostingRulesSeederHostedService> _log;

    public ConstructionPostingRulesSeederHostedService(
        IDbConnectionFactory db,
        ILogger<ConstructionPostingRulesSeederHostedService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await SeedAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[SPRINT-59] ConstructionPostingRulesSeeder failed — continuing");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private async Task SeedAsync(CancellationToken ct)
    {
        using var conn = (NpgsqlConnection)await _db.CreateOltpConnectionAsync(ct);

        // Get all companies
        using var cmd = new NpgsqlCommand("SELECT id, name FROM companies ORDER BY created_at;", conn);
        using var reader = await cmd.ExecuteReaderAsync(ct);
        var companies = new List<(Guid Id, string Name)>();
        while (await reader.ReadAsync(ct))
        {
            companies.Add((reader.GetGuid(0), reader.GetString(1)));
        }
        await reader.CloseAsync();

        if (companies.Count == 0)
        {
            _log.LogWarning("[SPRINT-59] No companies found — skipping construction posting rules seed.");
            return;
        }

        var rules = new[]
        {
            new
            {
                Name = "استلام دفعة مقدمة (مقاول — ليبي)",
                Description = "عند استلام دفعة مقدمة من العميل (حتى 15% من قيمة العقد). Dr نقدية/بنك / Cr 2106 دفعة مقدمة مستلمة.",
                EventType = 7, // AdvanceReceived
                IsActive = false, // INACTIVE by default — admin enables
                TemplateJson = JsonSerializer.Serialize(new
                {
                    Lines = new object[]
                    {
                        new { Side = "debit", AccountCode = "1101-001", AmountFormula = "{amount}" },
                        new { Side = "credit", AccountCode = "2106-001", AmountFormula = "{amount}" }
                    },
                    Reference = "Advance payment received",
                    Description = "استلام دفعة مقدمة من العميل"
                })
            },
            new
            {
                Name = "مستخلص جاري (مقاول — ليبي)",
                Description = "4 سطور لكل مستخلص: Dr AR / Cr Revenue + Dr WIP / Cr COGS + Dr Advance (خصم) / Cr AR + Dr Retention (خصم) / Cr AR. المبلغ الصافي = القيمة الإجمالية - الدفعة المقدمة - الاحتجاز.",
                EventType = 8, // ProgressBillingPosted
                IsActive = false,
                TemplateJson = JsonSerializer.Serialize(new
                {
                    Lines = new object[]
                    {
                        new { Side = "debit", AccountCode = "1201-001", AmountFormula = "{grossAmount}" },
                        new { Side = "credit", AccountCode = "5100", AmountFormula = "{revenue}" },
                        new { Side = "debit", AccountCode = "9201-001", AmountFormula = "{cogs}" },
                        new { Side = "credit", AccountCode = "5110", AmountFormula = "{cogs}" },
                        new { Side = "debit", AccountCode = "2106-001", AmountFormula = "{advanceDeducted}" },
                        new { Side = "credit", AccountCode = "1201-001", AmountFormula = "{advanceDeducted}" },
                        new { Side = "debit", AccountCode = "1201-001", AmountFormula = "{retentionDeducted}" },
                        new { Side = "credit", AccountCode = "2107-001", AmountFormula = "{retentionDeducted}" }
                    },
                    Reference = "Progress billing posted",
                    Description = "مستخلص جاري مع خصم دفعة مقدمة واحتجاز ضمان"
                })
            },
            new
            {
                Name = "إطلاق احتجاز الضمان (تسليم نهائي)",
                Description = "عند التسليم النهائي: 50% يُطلق فوراً + 50% بعد فترة الضمان (12 شهر عادةً). Dr 2107 احتجاز / Cr نقدية/بنك.",
                EventType = 9, // RetentionReleased
                IsActive = false,
                TemplateJson = JsonSerializer.Serialize(new
                {
                    Lines = new object[]
                    {
                        new { Side = "debit", AccountCode = "2107-001", AmountFormula = "{amount}" },
                        new { Side = "credit", AccountCode = "1101-001", AmountFormula = "{amount}" }
                    },
                    Reference = "Retention released",
                    Description = "إطلاق احتجاز الضمان للعميل"
                })
            },
            new
            {
                Name = "أمر تعديلي معتمد (إضافة WIP — Off-balance)",
                Description = "عند اعتماد Variation Order: إضافة Off-balance WIP فقط (لا يُنشأ قيد). يُسجّل في جدول contract_variations للعرض في تقارير WIP.",
                EventType = 10, // VariationOrderApproved
                IsActive = false,
                TemplateJson = JsonSerializer.Serialize(new
                {
                    Lines = new object[]
                    {
                        new { Side = "memo", AccountCode = "9201-MEMO", AmountFormula = "{variationAmount}" }
                    },
                    Reference = "Variation order approved (off-balance)",
                    Description = "إضافة WIP بدون قيد محاسبي"
                })
            },
            new
            {
                Name = "إكمال العقد (تحويل WIP إلى P&L)",
                Description = "عند اكتمال العقد: Dr 9201 WIP / Cr 5100 إيرادات (نهائي) + Dr 9201 / Cr 5110 تكلفة (نهائية).",
                EventType = 11, // ContractCompleted
                IsActive = false,
                TemplateJson = JsonSerializer.Serialize(new
                {
                    Lines = new object[]
                    {
                        new { Side = "debit", AccountCode = "1101-001", AmountFormula = "{finalInvoiceAmount}" },
                        new { Side = "credit", AccountCode = "5100", AmountFormula = "{finalRevenue}" },
                        new { Side = "debit", AccountCode = "9201-001", AmountFormula = "{finalCogs}" },
                        new { Side = "credit", AccountCode = "5110", AmountFormula = "{finalCogs}" }
                    },
                    Reference = "Contract completed — WIP transferred to P&L",
                    Description = "إنهاء المشروع وتحويل WIP إلى قائمة الدخل"
                })
            }
        };

        int inserted = 0;
        foreach (var company in companies)
        {
            foreach (var rule in rules)
            {
                // Idempotent: only insert if no rule with the same name+event_type for this company
                using var checkCmd = new NpgsqlCommand(
                    @"SELECT COUNT(*) FROM posting_rules WHERE company_id = @cid AND event_type = @evt AND name = @name;",
                    conn);
                checkCmd.Parameters.AddWithValue("cid", company.Id);
                checkCmd.Parameters.AddWithValue("evt", rule.EventType);
                checkCmd.Parameters.AddWithValue("name", rule.Name);
                var existing = Convert.ToInt32(await checkCmd.ExecuteScalarAsync(ct));
                if (existing > 0) continue;

                using var insCmd = new NpgsqlCommand(@"
INSERT INTO posting_rules (id, company_id, name, description, event_type, is_active, template_json, created_at, updated_at)
VALUES (gen_random_uuid(), @cid, @name, @desc, @evt, @active, @tmpl::jsonb, now(), now());", conn);
                insCmd.Parameters.AddWithValue("cid", company.Id);
                insCmd.Parameters.AddWithValue("name", rule.Name);
                insCmd.Parameters.AddWithValue("desc", rule.Description);
                insCmd.Parameters.AddWithValue("evt", rule.EventType);
                insCmd.Parameters.AddWithValue("active", rule.IsActive);
                insCmd.Parameters.AddWithValue("tmpl", rule.TemplateJson);
                await insCmd.ExecuteNonQueryAsync(ct);
                inserted++;
            }
        }

        _log.LogInformation("[SPRINT-59] ConstructionPostingRulesSeeder: inserted {N} rules across {C} companies (all INACTIVE — admin enables in /admin/posting-rules).", inserted, companies.Count);
    }
}
