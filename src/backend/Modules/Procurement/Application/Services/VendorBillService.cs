using Dapper;
using ERPSystem.Modules.Finance.Application;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Procurement.Application;
using ERPSystem.Modules.Procurement.Entities;
using ERPSystem.Modules.Procurement.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Procurement.Application.Services;

public interface IVendorBillService
{
    Task<ProcurementResult<VendorBillResponse>> CreateAsync(Guid userId, CreateVendorBillRequest req, CancellationToken ct);
    Task<ProcurementResult<VendorBillResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ProcurementResult<IReadOnlyList<VendorBillResponse>>> ListAsync(Guid? vendorId, Guid? grId, VendorBillStatus? status, int skip, int take, CancellationToken ct);
    Task<ProcurementResult<VendorBillResponse>> PostAsync(Guid userId, Guid id, CancellationToken ct);
}

public sealed class VendorBillService : IVendorBillService
{
    private readonly IVendorBillRepository _bills;
    private readonly IGoodsReceiptRepository _grs;
    private readonly IPurchaseOrderRepository _pos;
    private readonly IDocumentSequenceRepository _seq;
    private readonly IJournalEntryService _journalSvc;  // DEC-075: AP posting
    private readonly IDbConnectionFactory _db;          // DEC-075: account lookup
    private readonly IPostingRulesService _postingRules; // Sprint 21
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<VendorBillService> _logger;

    public VendorBillService(IVendorBillRepository bills, IGoodsReceiptRepository grs, IPurchaseOrderRepository pos,
        IDocumentSequenceRepository seq, IJournalEntryService journalSvc, IDbConnectionFactory db,
        IPostingRulesService postingRules,
        ICompanyContext companyContext,
        ILogger<VendorBillService> logger)
    {
        _bills = bills; _grs = grs; _pos = pos; _seq = seq;
        _journalSvc = journalSvc; _db = db; _postingRules = postingRules;
        _companyContext = companyContext; _logger = logger;
    }

    public async Task<ProcurementResult<VendorBillResponse>> CreateAsync(Guid userId, CreateVendorBillRequest req, CancellationToken ct)
    {
        // Business rule: Bill لا يُنشأ إلا لـ GR في حالة Received
        var gr = await _grs.GetByIdAsync(req.GoodsReceiptId, ct);
        if (gr == null)
            return ProcurementResult<VendorBillResponse>.Fail("GR غير موجود.", ProcurementErrorCode.NotFound);
        if (gr.Status != GoodsReceiptStatus.Received)
            return ProcurementResult<VendorBillResponse>.Fail(
                $"لا يمكن إنشاء Bill لـ GR في حالة {gr.Status} (يجب Received).", ProcurementErrorCode.BusinessRuleViolation);

        // VendorId يُجلب من PO المرتبط بالـ GR (denormalized على الـ Bill للـ queries السريعة)
        var po = await _pos.GetByIdAsync(gr.PurchaseOrderId, ct);
        var vendorId = po?.VendorId ?? Guid.Empty;

        var billNumber = await _seq.GetNextNumberAsync("BILL", ct);

        decimal subTotal = 0, taxAmount = 0;
        var lineEntities = new List<VendorBillLine>();
        for (int i = 0; i < req.Lines.Count; i++)
        {
            var l = req.Lines[i];
            var lineSub = l.Quantity * l.UnitPrice;
            var lineTax = lineSub * l.TaxRate;
            subTotal += lineSub; taxAmount += lineTax;
            lineEntities.Add(new VendorBillLine
            {
                Id = Guid.NewGuid(), VendorId = vendorId,
                ItemId = l.ItemId, Quantity = l.Quantity, UnitPrice = l.UnitPrice,
                TaxRate = l.TaxRate, SubTotal = lineSub, LineOrder = i
            });
        }
        var total = subTotal + taxAmount;

        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved — cannot create bill without company_id (Constitution Article 3).");
        var now = DateTime.UtcNow;
        var bill = new VendorBill
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,  // Sprint 25 fix (DEC-085 audit)
            BillNumber = billNumber, GoodsReceiptId = gr.Id, VendorId = vendorId,
            Status = VendorBillStatus.Draft,
            BillDate = req.BillDate, DueDate = req.DueDate,
            Currency = req.Currency.ToUpperInvariant(),
            SubTotal = subTotal, TaxAmount = taxAmount, TotalAmount = total,
            Notes = req.Notes,
            CreatedAt = now, CreatedBy = userId, UpdatedAt = now, UpdatedBy = userId
        };
        await _bills.InsertAsync(bill, ct);
        await _bills.InsertLinesAsync(bill.Id, lineEntities, ct);
        bill.Lines = lineEntities;
        return ProcurementResult<VendorBillResponse>.Ok(MapToResponse(bill));
    }

    public async Task<ProcurementResult<VendorBillResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var b = await _bills.GetByIdAsync(id, ct);
        if (b == null)
            return ProcurementResult<VendorBillResponse>.Fail("غير موجود.", ProcurementErrorCode.NotFound);
        // Sprint 30 (DEC-104): include vendor name in the single-record response.
        var vendorMap = await BuildVendorMapAsync(new[] { b.VendorId }, ct);
        return ProcurementResult<VendorBillResponse>.Ok(MapToResponse(b, vendorMap));
    }

    public async Task<ProcurementResult<IReadOnlyList<VendorBillResponse>>> ListAsync(Guid? vendorId, Guid? grId, VendorBillStatus? status, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        var list = await _bills.ListAsync(vendorId, grId, status, skip, take, ct);
        // Sprint 30 (DEC-104): enrich each bill with its vendor name/code (single batch query).
        var vendorMap = await BuildVendorMapAsync(list.Select(b => b.VendorId), ct);
        return ProcurementResult<IReadOnlyList<VendorBillResponse>>.Ok(list.Select(b => MapToResponse(b, vendorMap)).ToList());
    }

    /// <summary>
    /// ترحيل Bill (Draft → Posted) — DEC-075: الآن ينشئ JournalEntry تلقائياً.
    /// Dr Inventory (1240) / Cr Accounts Payable (2210)
    /// </summary>
    public async Task<ProcurementResult<VendorBillResponse>> PostAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var b = await _bills.GetByIdAsync(id, ct);
        if (b == null)
            return ProcurementResult<VendorBillResponse>.Fail("غير موجود.", ProcurementErrorCode.NotFound);
        if (b.Status != VendorBillStatus.Draft)
            return ProcurementResult<VendorBillResponse>.Fail(
                $"لا يمكن ترحيل Bill في حالة {b.Status}.", ProcurementErrorCode.InvalidStatusTransition);

        // DEC-075: Idempotency check — skip if already posted with JE
        if (b.JournalEntryId.HasValue && b.JournalEntryId != Guid.Empty)
        {
            _logger.LogInformation("Bill {BillNumber} already has JE {JE}, skipping post", b.BillNumber, b.JournalEntryId);
            return ProcurementResult<VendorBillResponse>.Ok(MapToResponse(b));
        }

        // ===== Sprint 21: Posting Rules Engine (preferred path) =====
        var payload = new EventPayload
        {
            Amount = b.TotalAmount,
            Subtotal = b.SubTotal,
            TaxAmount = b.TaxAmount,
            Currency = b.Currency,
            Description = $"Vendor Bill {b.BillNumber}",
            Reference = $"BILL-{b.BillNumber}",
            EntryDate = b.BillDate
        };

        var ruleResult = await _postingRules.ApplyRulesAndReturnAsync(userId, TriggeringEvent.VendorBillPosted, payload, ct);
        if (ruleResult.Succeeded && ruleResult.Value!.EntriesCreated > 0)
        {
            // الـ engine أنشأ القيد — استخدمه
            b.Status = VendorBillStatus.Posted;
            b.JournalEntryId = ruleResult.Value.FirstJournalEntryId;
            b.PostedAt = DateTime.UtcNow;
            b.UpdatedAt = DateTime.UtcNow;
            b.UpdatedBy = userId;
            await _bills.UpdateAsync(b, ct);
            _logger.LogInformation("Sprint 21: Bill {N} posted via rules engine — JE={JE}",
                b.BillNumber, ruleResult.Value.FirstEntryNumber);
            return ProcurementResult<VendorBillResponse>.Ok(MapToResponse(b));
        }

        // ===== Fallback: DEC-075 hardcoded path (لو ما في rules نشطة) =====
        _logger.LogWarning("Sprint 21: no active rules for VendorBillPosted; using DEC-075 fallback for bill {N}",
            b.BillNumber);

        // DEC-075: Look up account IDs for Inventory (1240) and AP (2210)
        Guid? inventoryAcctId = null, apAcctId = null;
        try
        {
            using var conn = await _db.CreateOltpConnectionAsync(ct);
            var accts = await conn.QueryAsync<(string Code, Guid AcctId)>(new CommandDefinition(
                "SELECT code, id FROM accounts WHERE code IN ('1240', '2210')",
                cancellationToken: ct));
            foreach (var (code, acctId) in accts)
            {
                if (code == "1240") inventoryAcctId = acctId;
                if (code == "2210") apAcctId = acctId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DEC-075: failed to look up accounts for bill posting");
        }

        if (inventoryAcctId == null || apAcctId == null)
        {
            _logger.LogWarning("DEC-075: missing accounts (1240={Inv}, 2210={AP}); posting bill {N} WITHOUT JE",
                inventoryAcctId, apAcctId, b.BillNumber);
            b.Status = VendorBillStatus.Posted;
            b.PostedAt = DateTime.UtcNow;
            b.UpdatedAt = DateTime.UtcNow;
            b.UpdatedBy = userId;
            await _bills.UpdateAsync(b, ct);
            return ProcurementResult<VendorBillResponse>.Ok(MapToResponse(b));
        }

        var je = await _journalSvc.CreateDraftAsync(userId, new PostJournalEntryRequest
        {
            EntryDate = b.BillDate,
            Description = $"Vendor Bill {b.BillNumber}",
            Reference = $"BILL-{b.BillNumber}",
            Lines = new List<PostJournalLineRequest>
            {
                new() { AccountId = inventoryAcctId.Value, Debit = b.TotalAmount, Credit = 0m,
                        Description = $"Inventory received — Bill {b.BillNumber}" },
                new() { AccountId = apAcctId.Value, Debit = 0m, Credit = b.TotalAmount,
                        Description = $"A/P to vendor — Bill {b.BillNumber}" }
            }
        }, ct);

        if (!je.Succeeded)
        {
            _logger.LogError("DEC-075: JE creation failed for bill {N}: {Err}", b.BillNumber, je.Error);
            return ProcurementResult<VendorBillResponse>.Fail(
                $"فشل إنشاء القيد المحاسبي: {je.Error}", ProcurementErrorCode.BusinessRuleViolation);
        }

        var postJe = await _journalSvc.PostAsync(userId, je.Value!.Id, ct);
        if (!postJe.Succeeded)
        {
            _logger.LogError("DEC-075: JE post failed for bill {N}: {Err}", b.BillNumber, postJe.Error);
            return ProcurementResult<VendorBillResponse>.Fail(
                $"فشل ترحيل القيد: {postJe.Error}", ProcurementErrorCode.BusinessRuleViolation);
        }

        b.Status = VendorBillStatus.Posted;
        b.JournalEntryId = je.Value!.Id;
        b.PostedAt = DateTime.UtcNow;
        b.UpdatedAt = DateTime.UtcNow;
        b.UpdatedBy = userId;
        await _bills.UpdateAsync(b, ct);

        _logger.LogInformation("DEC-075: Bill {N} posted — Dr Inv={D}, Cr AP={C}, JE={JE}",
            b.BillNumber, b.TotalAmount, b.TotalAmount, je.Value.Id);
        return ProcurementResult<VendorBillResponse>.Ok(MapToResponse(b));
    }

    private static VendorBillResponse MapToResponse(VendorBill b, IReadOnlyDictionary<Guid, (string Name, string Code)>? vendorMap = null) => new()
    {
        Id = b.Id, BillNumber = b.BillNumber, GoodsReceiptId = b.GoodsReceiptId,
        VendorId = b.VendorId,
        VendorName = vendorMap != null && vendorMap.TryGetValue(b.VendorId, out var v) ? v.Name : null,
        VendorCode = vendorMap != null && vendorMap.TryGetValue(b.VendorId, out var vc) ? vc.Code : null,
        Status = b.Status, BillDate = b.BillDate, DueDate = b.DueDate,
        Currency = b.Currency, SubTotal = b.SubTotal, TaxAmount = b.TaxAmount, TotalAmount = b.TotalAmount,
        Notes = b.Notes, JournalEntryId = b.JournalEntryId, PostedAt = b.PostedAt, CreatedAt = b.CreatedAt,
        Lines = b.Lines.Select(l => new VendorBillLineResponse
        {
            Id = l.Id, ItemId = l.ItemId, Quantity = l.Quantity, UnitPrice = l.UnitPrice,
            TaxRate = l.TaxRate, SubTotal = l.SubTotal, LineOrder = l.LineOrder
        }).ToList()
    };

    /// <summary>
    /// Sprint 30 (DEC-104): single batch lookup of vendor name + code for a list of vendor IDs.
    /// Returns a dictionary keyed by vendor ID. Used to enrich VendorBillResponse so the FE
    /// can show "VEND-001 — شركة النور" instead of a raw GUID.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, (string Name, string Code)>> BuildVendorMapAsync(
        IEnumerable<Guid> vendorIds, CancellationToken ct)
    {
        var ids = vendorIds.Distinct().Where(id => id != Guid.Empty).ToList();
        if (ids.Count == 0) return new Dictionary<Guid, (string, string)>();
        // Use Dapper directly to avoid a circular dependency on the vendor service.
        using var conn = await _db.CreateEphemeralOltpConnectionAsync(ct);
        var rows = await conn.QueryAsync<(Guid Id, string Name, string Code)>(
            "SELECT id, name, code FROM vendors WHERE id = ANY(@Ids)",
            new { Ids = ids.ToArray() });
        return rows.ToDictionary(r => r.Id, r => (r.Name, r.Code));
    }
}
