using ERPSystem.Modules.Procurement.Application;
using ERPSystem.Modules.Procurement.Entities;
using ERPSystem.Modules.Procurement.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Procurement.Application.Services;

public interface IVendorBillService
{
    Task<ProcurementResult<VendorBillResponse>> CreateAsync(Guid tenantId, Guid userId, CreateVendorBillRequest req, CancellationToken ct);
    Task<ProcurementResult<VendorBillResponse>> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct);
    Task<ProcurementResult<IReadOnlyList<VendorBillResponse>>> ListAsync(Guid tenantId, Guid? vendorId, Guid? grId, VendorBillStatus? status, int skip, int take, CancellationToken ct);
    Task<ProcurementResult<VendorBillResponse>> PostAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct);
}

public sealed class VendorBillService : IVendorBillService
{
    private readonly IVendorBillRepository _bills;
    private readonly IGoodsReceiptRepository _grs;
    private readonly IPurchaseOrderRepository _pos;
    private readonly IDocumentSequenceRepository _seq;
    private readonly ILogger<VendorBillService> _logger;

    public VendorBillService(IVendorBillRepository bills, IGoodsReceiptRepository grs, IPurchaseOrderRepository pos,
        IDocumentSequenceRepository seq, ILogger<VendorBillService> logger)
    { _bills = bills; _grs = grs; _pos = pos; _seq = seq; _logger = logger; }

    public async Task<ProcurementResult<VendorBillResponse>> CreateAsync(Guid tenantId, Guid userId, CreateVendorBillRequest req, CancellationToken ct)
    {
        // Business rule: Bill لا يُنشأ إلا لـ GR في حالة Received
        var gr = await _grs.GetByIdAsync(req.GoodsReceiptId, ct);
        if (gr == null || gr.TenantId != tenantId)
            return ProcurementResult<VendorBillResponse>.Fail("GR غير موجود.", ProcurementErrorCode.NotFound);
        if (gr.Status != GoodsReceiptStatus.Received)
            return ProcurementResult<VendorBillResponse>.Fail(
                $"لا يمكن إنشاء Bill لـ GR في حالة {gr.Status} (يجب Received).", ProcurementErrorCode.BusinessRuleViolation);

        // VendorId يُجلب من PO المرتبط بالـ GR (denormalized على الـ Bill للـ queries السريعة)
        var po = await _pos.GetByIdAsync(gr.PurchaseOrderId, ct);
        var vendorId = po?.VendorId ?? Guid.Empty;

        var billNumber = await _seq.GetNextNumberAsync(tenantId, "BILL", ct);

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
                Id = Guid.NewGuid(), TenantId = tenantId, VendorId = vendorId,
                ItemId = l.ItemId, Quantity = l.Quantity, UnitPrice = l.UnitPrice,
                TaxRate = l.TaxRate, SubTotal = lineSub, LineOrder = i
            });
        }
        var total = subTotal + taxAmount;

        var now = DateTime.UtcNow;
        var bill = new VendorBill
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            BillNumber = billNumber, GoodsReceiptId = gr.Id, VendorId = vendorId,
            Status = VendorBillStatus.Draft,
            BillDate = req.BillDate, DueDate = req.DueDate,
            Currency = req.Currency.ToUpperInvariant(),
            SubTotal = subTotal, TaxAmount = taxAmount, TotalAmount = total,
            Notes = req.Notes,
            CreatedAt = now, CreatedBy = userId, UpdatedAt = now, UpdatedBy = userId
        };
        await _bills.InsertAsync(bill, ct);
        await _bills.InsertLinesAsync(tenantId, bill.Id, lineEntities, ct);
        bill.Lines = lineEntities;
        return ProcurementResult<VendorBillResponse>.Ok(MapToResponse(bill));
    }

    public async Task<ProcurementResult<VendorBillResponse>> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        var b = await _bills.GetByIdAsync(id, ct);
        if (b == null || b.TenantId != tenantId)
            return ProcurementResult<VendorBillResponse>.Fail("غير موجود.", ProcurementErrorCode.NotFound);
        return ProcurementResult<VendorBillResponse>.Ok(MapToResponse(b));
    }

    public async Task<ProcurementResult<IReadOnlyList<VendorBillResponse>>> ListAsync(Guid tenantId, Guid? vendorId, Guid? grId, VendorBillStatus? status, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        var list = await _bills.ListAsync(tenantId, vendorId, grId, status, skip, take, ct);
        return ProcurementResult<IReadOnlyList<VendorBillResponse>>.Ok(list.Select(MapToResponse).ToList());
    }

    /// <summary>
    /// ترحيل Bill (Draft → Posted).
    /// MVP: نُحدّث الحالة فقط. إنشاء JournalEntry التفصيلي (Dr Inventory / Cr A/P) قادم في Phase 3.1
    /// عندما نضيف PostingRule خاص بـ VendorBillPosting event.
    /// </summary>
    public async Task<ProcurementResult<VendorBillResponse>> PostAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct)
    {
        var b = await _bills.GetByIdAsync(id, ct);
        if (b == null || b.TenantId != tenantId)
            return ProcurementResult<VendorBillResponse>.Fail("غير موجود.", ProcurementErrorCode.NotFound);
        if (b.Status != VendorBillStatus.Draft)
            return ProcurementResult<VendorBillResponse>.Fail(
                $"لا يمكن ترحيل Bill في حالة {b.Status}.", ProcurementErrorCode.InvalidStatusTransition);

        b.Status = VendorBillStatus.Posted;
        b.PostedAt = DateTime.UtcNow;
        b.UpdatedAt = DateTime.UtcNow;
        b.UpdatedBy = userId;
        await _bills.UpdateAsync(b, ct);
        _logger.LogInformation("تم ترحيل Bill {BillNumber} بقيمة {Total}", b.BillNumber, b.TotalAmount);
        return ProcurementResult<VendorBillResponse>.Ok(MapToResponse(b));
    }

    private static VendorBillResponse MapToResponse(VendorBill b) => new()
    {
        Id = b.Id, TenantId = b.TenantId, BillNumber = b.BillNumber, GoodsReceiptId = b.GoodsReceiptId,
        VendorId = b.VendorId, Status = b.Status, BillDate = b.BillDate, DueDate = b.DueDate,
        Currency = b.Currency, SubTotal = b.SubTotal, TaxAmount = b.TaxAmount, TotalAmount = b.TotalAmount,
        Notes = b.Notes, JournalEntryId = b.JournalEntryId, PostedAt = b.PostedAt, CreatedAt = b.CreatedAt,
        Lines = b.Lines.Select(l => new VendorBillLineResponse
        {
            Id = l.Id, ItemId = l.ItemId, Quantity = l.Quantity, UnitPrice = l.UnitPrice,
            TaxRate = l.TaxRate, SubTotal = l.SubTotal, LineOrder = l.LineOrder
        }).ToList()
    };
}
