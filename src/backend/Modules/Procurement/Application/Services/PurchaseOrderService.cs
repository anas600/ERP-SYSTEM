using ERPSystem.Modules.Procurement.Application;
using ERPSystem.Modules.Procurement.Entities;
using ERPSystem.Modules.Procurement.Infrastructure;
using ERPSystem.Shared.MultiTenancy;
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
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(IPurchaseOrderRepository pos, IVendorRepository vendors, IDocumentSequenceRepository seq, ICompanyContext companyContext, ILogger<PurchaseOrderService> logger)
    { _pos = pos; _vendors = vendors; _seq = seq; _companyContext = companyContext; _logger = logger; }

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
        return ProcurementResult<PurchaseOrderResponse>.Ok(MapToResponse(po));
    }

    public async Task<ProcurementResult<IReadOnlyList<PurchaseOrderResponse>>> ListAsync(Guid? vendorId, PurchaseOrderStatus? status, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        var list = await _pos.ListAsync(vendorId, status, skip, take, ct);
        return ProcurementResult<IReadOnlyList<PurchaseOrderResponse>>.Ok(list.Select(MapToResponse).ToList());
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
}
