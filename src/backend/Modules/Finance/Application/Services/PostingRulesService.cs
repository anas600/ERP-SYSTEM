using System.Text.Json;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Finance.Infrastructure;

namespace ERPSystem.Modules.Finance.Application.Services;

public sealed class PostingRulesService : IPostingRulesService
{
    private readonly IPostingRuleRepository _rules;
    private readonly IAccountRepository _accounts;
    private readonly IJournalEntryService _journalService;
    private readonly ILogger<PostingRulesService> _logger;

    public PostingRulesService(
        IPostingRuleRepository rules,
        IAccountRepository accounts,
        IJournalEntryService journalService,
        ILogger<PostingRulesService> logger)
    {
        _rules = rules;
        _accounts = accounts;
        _journalService = journalService;
        _logger = logger;
    }

    public async Task<FinanceResult<PostingRule>> CreateAsync(CreatePostingRuleRequest request, CancellationToken ct)
    {
        // التحقق من صحة كل account code في الـ template
        foreach (var line in request.Template.Lines)
        {
            var acc = await _accounts.GetByCodeAsync(line.AccountCode, ct);
            if (acc == null)
            {
                return FinanceResult<PostingRule>.Fail(
                    $"كود الحساب '{line.AccountCode}' غير موجود في دليل الحسابات.",
                    FinanceErrorCode.NotFound);
            }
            if (!acc.IsPostable)
            {
                return FinanceResult<PostingRule>.Fail(
                    $"الحساب '{line.AccountCode}' تجميعي — لا يصلح للقيد.",
                    FinanceErrorCode.InvalidAccount);
            }
        }

        var now = DateTime.UtcNow;
        var rule = new PostingRule
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            EventType = request.EventType,
            IsActive = true,
            TemplateJson = JsonSerializer.Serialize(request.Template),
            CreatedAt = now,
            UpdatedAt = now
        };
        await _rules.InsertAsync(rule, ct);
        _logger.LogInformation("تم إنشاء قاعدة ترحيل {Name} ({EventType})", rule.Name, rule.EventType);
        return FinanceResult<PostingRule>.Ok(rule);
    }

    public async Task<FinanceResult<IReadOnlyList<PostingRule>>> ListAsync(CancellationToken ct)
    {
        var list = await _rules.ListAsync(ct);
        return FinanceResult<IReadOnlyList<PostingRule>>.Ok(list);
    }

    public async Task<int> ApplyRulesAsync(Guid userId, TriggeringEvent eventType, EventPayload payload, CancellationToken ct)
    {
        var result = await ApplyRulesInternalAsync(userId, eventType, payload, ct);
        return result.EntriesCreated;
    }

    public async Task<FinanceResult<ApplyRulesResult>> ApplyRulesAndReturnAsync(Guid userId, TriggeringEvent eventType, EventPayload payload, CancellationToken ct)
    {
        var result = await ApplyRulesInternalAsync(userId, eventType, payload, ct);
        if (result.EntriesCreated == 0)
        {
            // ما في قواعد نشطة — هذا ليس خطأ، فقط "no rules matched"
            return FinanceResult<ApplyRulesResult>.Ok(result);
        }
        return FinanceResult<ApplyRulesResult>.Ok(result);
    }

    /// <summary>
    /// التنفيذ الداخلي: يستخرج كل القواعد النشطة، يطابق، ينشئ القيود.
    /// يعيد الـ first JE ID للـ Service للربط.
    /// </summary>
    private async Task<ApplyRulesResult> ApplyRulesInternalAsync(Guid userId, TriggeringEvent eventType, EventPayload payload, CancellationToken ct)
    {
        var result = new ApplyRulesResult();
        var rules = await _rules.ListActiveByEventAsync(eventType, ct);
        if (rules.Count == 0)
        {
            _logger.LogDebug("لا توجد قواعد نشطة لـ {EventType}", eventType);
            return result;
        }

        foreach (var rule in rules)
        {
            PostingRuleTemplate? template;
            try
            {
                template = JsonSerializer.Deserialize<PostingRuleTemplate>(rule.TemplateJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "قاعدة {RuleId} لها template_json غير صالح", rule.Id);
                continue;
            }
            if (template == null || template.Lines.Count < 2) continue;

            // بناء الـ request
            var req = new PostJournalEntryRequest
            {
                EntryDate = payload.EntryDate,
                Description = string.IsNullOrEmpty(payload.Description) ? template.Description : payload.Description,
                Reference = payload.Reference ?? template.Reference,
                Lines = new List<PostJournalLineRequest>()
            };

            decimal totalDebit = 0, totalCredit = 0;
            foreach (var line in template.Lines)
            {
                var acc = await _accounts.GetByCodeAsync(line.AccountCode, ct);
                if (acc == null)
                {
                    _logger.LogWarning("تخطي سطر: الحساب {Code} غير موجود", line.AccountCode);
                    continue;
                }

                var amount = EvaluateFormula(line.AmountFormula, payload);
                var isDebit = line.Side.Equals("debit", StringComparison.OrdinalIgnoreCase);
                req.Lines.Add(new PostJournalLineRequest
                {
                    AccountId = acc.Id,
                    Debit = isDebit ? amount : 0,
                    Credit = isDebit ? 0 : amount
                });
                if (isDebit) totalDebit += amount; else totalCredit += amount;
            }

            // معادلة الـ double-entry
            if (totalDebit != totalCredit)
            {
                _logger.LogError("قاعدة {RuleId} تنتج قيد غير متوازن: D={D} C={C} — تم التخطي",
                    rule.Id, totalDebit, totalCredit);
                continue;
            }

            var draft = await _journalService.CreateDraftAsync(userId, req, ct);
            if (!draft.Succeeded)
            {
                _logger.LogError("فشل إنشاء القيد من القاعدة {RuleId}: {Error}", rule.Id, draft.Error);
                continue;
            }

            var post = await _journalService.PostAsync(userId, draft.Value!.Id, ct);
            if (post.Succeeded)
            {
                result.EntriesCreated++;
                if (result.FirstJournalEntryId == null)
                {
                    result.FirstJournalEntryId = post.Value!.Id;
                    result.FirstEntryNumber = post.Value!.EntryNumber;
                }
                _logger.LogInformation("تم تطبيق القاعدة {RuleId} وإنشاء القيد {EntryNumber}",
                    rule.Id, post.Value!.EntryNumber);
            }
        }
        return result;
    }

    public async Task EnsureDefaultRulesAsync(CancellationToken ct)
    {
        // ====== StockReceived → Inventory Debit / Accounts Payable Credit (Sprint 11) ======
        var existingStock = (await _rules.ListActiveByEventAsync(TriggeringEvent.StockReceived, ct))
            .FirstOrDefault();
        if (existingStock == null)
        {
            var stockRule = new PostingRule
            {
                Id = Guid.NewGuid(),
                Name = "استلام بضاعة (افتراضي)",
                Description = "عند استلام بضاعة، مدين المخزون ودائن الدائنون",
                EventType = TriggeringEvent.StockReceived,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TemplateJson = JsonSerializer.Serialize(new PostingRuleTemplate
                {
                    Description = "استلام بضاعة",
                    Lines = new()
                    {
                        // Sprint 21: fixed — use real account codes (1240 Inventory, 2210 AP)
                        // The previous (1300, 2100) didn't exist in the actual CoA.
                        new() { AccountCode = "1240", Side = "debit", AmountFormula = "{amount}" },
                        new() { AccountCode = "2210", Side = "credit", AmountFormula = "{amount}" }
                    }
                })
            };
            await _rules.InsertAsync(stockRule, ct);
        }

        // ====== Sprint 21: Libya default rules (no tax) ======

        // --- SalesInvoicePosted: Dr AR / Cr Sales Revenue ---
        var existingSale = (await _rules.ListActiveByEventAsync(TriggeringEvent.SalesInvoicePosted, ct))
            .FirstOrDefault();
        if (existingSale == null)
        {
            var saleRule = new PostingRule
            {
                Id = Guid.NewGuid(),
                Name = "فاتورة مبيعات (افتراضي - ليبيا)",
                Description = "ترحيل فاتورة مبيعات: مدين ذمم مدينة / دائن إيرادات",
                EventType = TriggeringEvent.SalesInvoicePosted,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TemplateJson = JsonSerializer.Serialize(new PostingRuleTemplate
                {
                    Description = "بيع",
                    // Reference is set at runtime from payload.Reference
                    Lines = new()
                    {
                        new() { AccountCode = "1230", Side = "debit", AmountFormula = "{amount}" },
                        new() { AccountCode = "5110", Side = "credit", AmountFormula = "{subtotal}" }
                    }
                })
            };
            await _rules.InsertAsync(saleRule, ct);
        }

        // --- VendorBillPosted: Dr Inventory / Cr AP ---
        var existingBill = (await _rules.ListActiveByEventAsync(TriggeringEvent.VendorBillPosted, ct))
            .FirstOrDefault();
        if (existingBill == null)
        {
            var billRule = new PostingRule
            {
                Id = Guid.NewGuid(),
                Name = "فاتورة مورّد (افتراضي - ليبيا)",
                Description = "ترحيل فاتورة مورّد: مدين مخزون / دائن ذمم دائنة",
                EventType = TriggeringEvent.VendorBillPosted,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TemplateJson = JsonSerializer.Serialize(new PostingRuleTemplate
                {
                    Description = "شراء",
                    Lines = new()
                    {
                        new() { AccountCode = "1240", Side = "debit", AmountFormula = "{amount}" },
                        new() { AccountCode = "2210", Side = "credit", AmountFormula = "{amount}" }
                    }
                })
            };
            await _rules.InsertAsync(billRule, ct);
        }

        // --- ReceiptPosted: Dr Cash / Cr AR (customer paid us) ---
        var existingReceipt = (await _rules.ListActiveByEventAsync(TriggeringEvent.ReceiptPosted, ct))
            .FirstOrDefault();
        if (existingReceipt == null)
        {
            var receiptRule = new PostingRule
            {
                Id = Guid.NewGuid(),
                Name = "سند قبض (افتراضي - ليبيا)",
                Description = "استلام دفعة من عميل: مدين نقدية / دائن ذمم مدينة",
                EventType = TriggeringEvent.ReceiptPosted,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TemplateJson = JsonSerializer.Serialize(new PostingRuleTemplate
                {
                    Description = "استلام دفعة",
                    Lines = new()
                    {
                        new() { AccountCode = "1210", Side = "debit", AmountFormula = "{amount}" },
                        new() { AccountCode = "1230", Side = "credit", AmountFormula = "{amount}" }
                    }
                })
            };
            await _rules.InsertAsync(receiptRule, ct);
        }

        // --- PaymentPosted: Dr AP / Cr Cash (we paid a vendor) ---
        var existingPayment = (await _rules.ListActiveByEventAsync(TriggeringEvent.PaymentPosted, ct))
            .FirstOrDefault();
        if (existingPayment == null)
        {
            var paymentRule = new PostingRule
            {
                Id = Guid.NewGuid(),
                Name = "دفع لمورّد (افتراضي - ليبيا)",
                Description = "دفع لمورّد: مدين ذمم دائنة / دائن نقدية",
                EventType = TriggeringEvent.PaymentPosted,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TemplateJson = JsonSerializer.Serialize(new PostingRuleTemplate
                {
                    Description = "دفع لمورّد",
                    Lines = new()
                    {
                        new() { AccountCode = "2210", Side = "debit", AmountFormula = "{amount}" },
                        new() { AccountCode = "1210", Side = "credit", AmountFormula = "{amount}" }
                    }
                })
            };
            await _rules.InsertAsync(paymentRule, ct);
        }
    }

    /// <summary>
    /// صيغ مبسّطة (token replacement):
    ///   {amount}     → payload.Amount (إجمالي)
    ///   {subtotal}   → payload.Subtotal (قبل الضريبة)
    ///   {tax}        → payload.TaxAmount (الضريبة)
    ///   {subtotal*0.05} → subtotal × 5% (مثال لـ VAT 5%)
    ///   أي رقم خام   → يفسَّر كقيمة ثابتة
    /// </summary>
    private decimal EvaluateFormula(string formula, EventPayload payload)
    {
        if (string.IsNullOrWhiteSpace(formula)) return 0;
        var trimmed = formula.Trim();

        // 1) خام رقمي
        if (decimal.TryParse(trimmed, out var n)) return n;

        // 2) Token بسيط
        if (trimmed == "{amount}") return payload.Amount;
        if (trimmed == "{subtotal}") return payload.Subtotal;
        if (trimmed == "{tax}") return payload.TaxAmount;
        if (trimmed == "{tax+subtotal}") return payload.TaxAmount + payload.Subtotal;

        // 3) Token مع ضرب (مثال: {subtotal}*0.05)
        if (trimmed.Contains('*'))
        {
            var parts = trimmed.Split('*');
            if (parts.Length == 2)
            {
                var left = EvaluateFormula(parts[0].Trim(), payload);
                if (decimal.TryParse(parts[1].Trim(), out var right))
                {
                    return left * right;
                }
            }
        }

        _logger.LogWarning("صيغة مبلغ غير معروفة: {Formula}", formula);
        return 0;
    }
}
