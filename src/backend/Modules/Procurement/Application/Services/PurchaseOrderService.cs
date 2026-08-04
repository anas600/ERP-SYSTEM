using Dapper;
using ERPSystem.Modules.Procurement.Application;
using ERPSystem.Modules.Procurement.Entities;
using ERPSystem.Modules.Procurement.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Procurement.Application.Services;

public interface IPurchaseOrderService
{
    Task<ProcurementResult<PurchaseOrderResponse>> CreateAsync(Guid userId, CreatePurchaseOrderRequest req, CancellationToken ct);
    Task<ProcurementResult<PurchaseOrderResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ProcurementResult<IReadOnlyList<PurchaseOrderResponse>>> ListAsync(Guid? vendorId, PurchaseOrderStatus? status, int skip, int take, CancellationToken ct);
    Task<ProcurementResult<PurchaseOrderResponse>> ApproveAsync(Guid userId, Guid id, CancellationToken ct);
    Task<ProcurementResult<PurchaseOrderResponse>> SendAsync(Guid userId, Guid id, CancellationToken ct);
}

public sealed class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _pos;
    private readonly IVendorRepository _vendors;
    private readonly IDocumentSequenceRepository _seq;
    private readonly ICompanyContext _companyContext;
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(IPurchaseOrderRepository pos, IVendorRepository vendors, IDocumentSequenceRepository seq, ICompanyContext companyContext, IDbConnectionFactory db, ILogger<PurchaseOrderService> logger)
    { _pos = pos; _vendors = vendors; _seq = seq; _companyContext = companyContext; _db = db; _logger = logger; }

    public async Task<ProcurementResult<PurchaseOrderResponse>> CreateAsync(Guid userId, CreatePurchaseOrderRequest req, CancellationToken ct)
    {
        // التحقق من وجود المورّد
        var vendor = await _vendors.GetByIdAsync(req.VendorId, ct);
        if (vendor == null)
            return ProcurementResult<PurchaseOrderResponse>.Fail("المورّد غير موجود.", ProcurementErrorCode.NotFound);
        if (!vendor.IsActive)
            return ProcurementResult<PurchaseOrderResponse>.Fail("المورّد غير نشط.", ProcurementErrorCode.BusinessRuleViolation);

        // توليد رقم PO تلقائي
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");
        var poNumber = await _seq.GetNextNumberAsync("PO", ct);

        // حساب المبالغ
        decimal subTotal = 0, taxAmount = 0;
        var lineEntities = new List<PurchaseOrderLine>();
        for (int i = 0; i < req.Lines.Count; i++)
        {
            var l = req.Lines[i];
            var lineSub = l.Quantity * l.UnitPrice;
            var lineTax = lineSub * l.TaxRate;
            subTotal += lineSub;
            taxAmount += lineTax;
            lineEntities.Add(new PurchaseOrderLine
            {
                Id = Guid.NewGuid(),
                ItemId = l.ItemId, Quantity = l.Quantity, UnitPrice = l.UnitPrice,
                TaxRate = l.TaxRate, SubTotal = lineSub, LineOrder = i
            });
        }
        var total = subTotal + taxAmount;

        var now = DateTime.UtcNow;
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,  // Sprint 25 fix (DEC-085 audit) — was missing
            PoNumber = poNumber, VendorId = req.VendorId,
            Status = PurchaseOrderStatus.Draft,
            OrderDate = req.OrderDate, ExpectedDate = req.ExpectedDate,
            Currency = req.Currency.ToUpperInvariant(),
            SubTotal = subTotal, TaxAmount = taxAmount, TotalAmount = total,
            Notes = req.Notes,
            CreatedAt = now, CreatedBy = userId, UpdatedAt = now, UpdatedBy = userId
        };

        await _pos.InsertAsync(po, ct);
        await _pos.InsertLinesAsync(po.Id, lineEntities, ct);
        po.Lines = lineEntities;

        _logger.LogInformation("تم إنشاء PO {PoNumber} بقيمة {Total}", poNumber, total);
        return ProcurementResult<PurchaseOrderResponse>.Ok(MapToResponse(po));
    }

    public async Task<ProcurementResult<PurchaseOrderResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var po = await _pos.GetByIdAsync(id, ct);
        if (po == null)
            return ProcurementResult<PurchaseOrderResponse>.Fail("غير موجود.", ProcurementErrorCode.NotFound);
        var resp = MapToResponse(po);

        // Sprint 30 (DEC-105a): enrich with vendor name + code.
        var vendorMap = await BuildVendorMapAsync(new[] { po.VendorId }, ct);
        if (vendorMap.TryGetValue(po.VendorId, out var v))
        {
            resp.VendorName = v.Name;
            resp.VendorCode = v.Code;
        }

        return ProcurementResult<PurchaseOrderResponse>.Ok(resp);
    }

    public async Task<ProcurementResult<IReadOnlyList<PurchaseOrderResponse>>> ListAsync(Guid? vendorId, PurchaseOrderStatus? status, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        var list = await _pos.ListAsync(vendorId, status, skip, take, ct);
        var responses = list.Select(MapToResponse).ToList();

        // Sprint 30 (DEC-105a): enrich with vendor name + code so FE doesn't show raw GUIDs.
        var vendorMap = await BuildVendorMapAsync(list.Select(p => p.VendorId), ct);
        foreach (var r in responses)
        {
            if (vendorMap.TryGetValue(r.VendorId, out var v))
            {
                r.VendorName = v.Name;
                r.VendorCode = v.Code;
            }
        }

        return ProcurementResult<IReadOnlyList<PurchaseOrderResponse>>.Ok(responses);
    }

    public async Task<ProcurementResult<PurchaseOrderResponse>> ApproveAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var po = await _pos.GetByIdAsync(id, ct);
        if (po == null)
            return ProcurementResult<PurchaseOrderResponse>.Fail("غير موجود.", ProcurementErrorCode.NotFound);

        // Business rule: يمكن الموافقة فقط من Draft أو Pending
        if (po.Status != PurchaseOrderStatus.Draft && po.Status != PurchaseOrderStatus.Pending)
            return ProcurementResult<PurchaseOrderResponse>.Fail(
                $"لا يمكن الموافقة على PO في حالة {po.Status}.", ProcurementErrorCode.InvalidStatusTransition);

        po.Status = PurchaseOrderStatus.Approved;
        po.ApprovedAt = DateTime.UtcNow;
        po.ApprovedBy = userId;
        po.UpdatedAt = DateTime.UtcNow;
        po.UpdatedBy = userId;
        await _pos.UpdateAsync(po, ct);
        _logger.LogInformation("تمت الموافقة على PO {PoNumber} من المستخدم {UserId}", po.PoNumber, userId);
        return ProcurementResult<PurchaseOrderResponse>.Ok(MapToResponse(po));
    }

    public async Task<ProcurementResult<PurchaseOrderResponse>> SendAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var po = await _pos.GetByIdAsync(id, ct);
        if (po == null)
            return ProcurementResult<PurchaseOrderResponse>.Fail("غير موجود.", ProcurementErrorCode.NotFound);

        // Business rule: يمكن الإرسال فقط بعد الموافقة
        if (po.Status != PurchaseOrderStatus.Approved)
            return ProcurementResult<PurchaseOrderResponse>.Fail(
                $"لا يمكن إرسال PO في حالة {po.Status} (يجب أن يكون Approved).", ProcurementErrorCode.InvalidStatusTransition);

        po.Status = PurchaseOrderStatus.Sent;
        po.SentAt = DateTime.UtcNow;
        po.UpdatedAt = DateTime.UtcNow;
        po.UpdatedBy = userId;
        await _pos.UpdateAsync(po, ct);
        _logger.LogInformation("تم إرسال PO {PoNumber} للمورّد", po.PoNumber);
        return ProcurementResult<PurchaseOrderResponse>.Ok(MapToResponse(po));
    }

    private static PurchaseOrderResponse MapToResponse(PurchaseOrder po) => new()
    {
        Id = po.Id, PoNumber = po.PoNumber, VendorId = po.VendorId,
        Status = po.Status, OrderDate = po.OrderDate, ExpectedDate = po.ExpectedDate,
        Currency = po.Currency, SubTotal = po.SubTotal, TaxAmount = po.TaxAmount, TotalAmount = po.TotalAmount,
        Notes = po.Notes, ApprovedAt = po.ApprovedAt, ApprovedBy = po.ApprovedBy, SentAt = po.SentAt,
        CreatedAt = po.CreatedAt,
        Lines = po.Lines.Select(l => new PurchaseOrderLineResponse
        {
            Id = l.Id, ItemId = l.ItemId, Quantity = l.Quantity, UnitPrice = l.UnitPrice,
            TaxRate = l.TaxRate, SubTotal = l.SubTotal, LineOrder = l.LineOrder
        }).ToList()
    };

    // Sprint 30 (DEC-105a): one-batch vendor lookup for enrichment. Same pattern as
    // VendorBillService.BuildVendorMapAsync (DEC-104) — single query instead of N+1.
    private async Task<IReadOnlyDictionary<Guid, (string Name, string Code)>> BuildVendorMapAsync(
        IEnumerable<Guid> vendorIds, CancellationToken ct)
    {
        var ids = vendorIds.Distinct().Where(id => id != Guid.Empty).ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, (string Name, string Code)>();

        // Use Dapper directly to avoid a circular dependency on the vendor service.
        using var conn = await _db.CreateEphemeralOltpConnectionAsync(ct);
        var rows = await conn.QueryAsync<(Guid Id, string Name, string Code)>(
            "SELECT id, name, code FROM vendors WHERE id = ANY(@Ids)",
            new { Ids = ids.ToArray() });
        return rows.ToDictionary(r => r.Id, r => (r.Name, r.Code));
    }
}
