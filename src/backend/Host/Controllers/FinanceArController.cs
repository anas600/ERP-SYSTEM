using System.Security.Claims;
using ERPSystem.Host.Authorization;
using ERPSystem.Modules.AccountsReceivable.Application;
using ERPSystem.Modules.AccountsReceivable.Application.Services;
using ERPSystem.Modules.AccountsReceivable.Entities;
using ERPSystem.Shared.CompanyContext;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// AR API — customers, sales invoices, receipts, aging report.
/// يتبع نفس نمط ProcurementController: UserId من JWT claims،
/// Result pattern عبر ArResult&lt;T&gt;، و FluentValidation في الـ entry point.
/// </summary>
[ApiController]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.FinanceWrite)]
[RequirePermission("ar.customers.view")]
public class FinanceArController : ControllerBase
{
    private readonly ICustomerService _customers;
    private readonly ISalesInvoiceService _invoices;
    private readonly IReceiptService _receipts;
    // Sprint 36 (DEC-122): customer statement (AR aging + invoice/receipt detail).
    private readonly ICustomerStatementService _customerStatements;
    // Sprint 56 (DEC-149 + DEC-150): Top Customers + Top Items reports.
    private readonly ITopCustomersReportService _topCustomers;
    private readonly ICompanyContext _companyContext;

    private readonly IValidator<CreateCustomerRequest> _createCustomerV;
    private readonly IValidator<UpdateCustomerRequest> _updateCustomerV;
    private readonly IValidator<CreateSalesInvoiceRequest> _createInvoiceV;
    private readonly IValidator<UpdateSalesInvoiceRequest> _updateInvoiceV;
    private readonly IValidator<CreateReceiptRequest> _createReceiptV;

    public FinanceArController(
        ICustomerService customers,
        ISalesInvoiceService invoices,
        IReceiptService receipts,
        ICustomerStatementService customerStatements,
        ITopCustomersReportService topCustomers,
        IValidator<CreateCustomerRequest> createCustomerV,
        IValidator<UpdateCustomerRequest> updateCustomerV,
        IValidator<CreateSalesInvoiceRequest> createInvoiceV,
        IValidator<UpdateSalesInvoiceRequest> updateInvoiceV,
        IValidator<CreateReceiptRequest> createReceiptV,
        ICompanyContext companyContext)
    {
        _customers = customers; _invoices = invoices; _receipts = receipts;
        _customerStatements = customerStatements;
        _topCustomers = topCustomers;
        _companyContext = companyContext;
        _createCustomerV = createCustomerV; _updateCustomerV = updateCustomerV;
        _createInvoiceV = createInvoiceV; _updateInvoiceV = updateInvoiceV; _createReceiptV = createReceiptV;
    }

    private Guid UserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")!.Value);

    // ============== Customers ==============

    [HttpGet("api/ar/customers")]
    [RequirePermission("ar.customers.view")]
    public async Task<IActionResult> ListCustomers(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int skip = 0, [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var r = await _customers.ListAsync(includeInactive, skip, take, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("api/ar/customers/{id:guid}")]
    [RequirePermission("ar.customers.view")]
    public async Task<IActionResult> GetCustomer(Guid id, CancellationToken ct)
    {
        var r = await _customers.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    /// <summary>Sprint 36 (DEC-122): كشف حساب عميل (opening + invoices + receipts + closing).</summary>
    [HttpGet("api/ar/customers/{id:guid}/statement")]
    [RequirePermission("ar.customers.view")]
    public async Task<IActionResult> GetCustomerStatement(
        Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        var r = await _customerStatements.GetStatementAsync(id, from, to, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost("api/ar/customers")]
    [RequirePermission("ar.customers.create")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest req, CancellationToken ct)
    {
        var v = await _createCustomerV.ValidateAsync(req, ct);
        if (!v.IsValid) return BadRequest(ValidationProblem(v));
        var r = await _customers.CreateAsync(UserId, req, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetCustomer), new { id = r.Value!.Id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpPut("api/ar/customers/{id:guid}")]
    [RequirePermission("ar.customers.update")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest req, CancellationToken ct)
    {
        var v = await _updateCustomerV.ValidateAsync(req, ct);
        if (!v.IsValid) return BadRequest(ValidationProblem(v));
        var r = await _customers.UpdateAsync(UserId, id, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpDelete("api/ar/customers/{id:guid}")]
    [RequirePermission("ar.customers.update")] // deactivate is an update-style write
    public async Task<IActionResult> DeactivateCustomer(Guid id, CancellationToken ct)
    {
        var r = await _customers.DeactivateAsync(UserId, id, ct);
        return r.Succeeded ? NoContent() : BadRequest(Problem(r));
    }

    // ============== Sales Invoices ==============

    [HttpGet("api/ar/sales-invoices")]
    [RequirePermission("ar.invoices.view")]
    public async Task<IActionResult> ListInvoices(
        [FromQuery] Guid? customerId, [FromQuery] SalesInvoiceStatus? status,
        [FromQuery] int skip = 0, [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var r = await _invoices.ListAsync(customerId, status, skip, take, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("api/ar/sales-invoices/{id:guid}")]
    [RequirePermission("ar.invoices.view")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct)
    {
        var r = await _invoices.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost("api/ar/sales-invoices")]
    [RequirePermission("ar.invoices.create")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateSalesInvoiceRequest req, CancellationToken ct)
    {
        var v = await _createInvoiceV.ValidateAsync(req, ct);
        if (!v.IsValid) return BadRequest(ValidationProblem(v));
        var r = await _invoices.CreateAsync(UserId, req, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetInvoice), new { id = r.Value!.Id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpPut("api/ar/sales-invoices/{id:guid}")]
    [RequirePermission("ar.invoices.create")] // edit before posting
    public async Task<IActionResult> UpdateInvoice(Guid id, [FromBody] UpdateSalesInvoiceRequest req, CancellationToken ct)
    {
        var v = await _updateInvoiceV.ValidateAsync(req, ct);
        if (!v.IsValid) return BadRequest(ValidationProblem(v));
        var r = await _invoices.UpdateAsync(UserId, id, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpPut("api/ar/sales-invoices/{id:guid}/post")]
    [RequirePermission("ar.invoices.post")]
    public async Task<IActionResult> PostInvoice(Guid id, CancellationToken ct)
    {
        var r = await _invoices.PostAsync(UserId, id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpPut("api/ar/sales-invoices/{id:guid}/cancel")]
    [RequirePermission("ar.invoices.create")]
    public async Task<IActionResult> CancelInvoice(Guid id, CancellationToken ct)
    {
        var r = await _invoices.CancelAsync(UserId, id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    // ============== Receipts ==============

    [HttpGet("api/ar/receipts")]
    [RequirePermission("ar.receipts.create")]
    public async Task<IActionResult> ListReceipts(
        [FromQuery] Guid? customerId,
        [FromQuery] int skip = 0, [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var r = await _receipts.ListAsync(customerId, skip, take, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpGet("api/ar/receipts/{id:guid}")]
    [RequirePermission("ar.receipts.create")]
    public async Task<IActionResult> GetReceipt(Guid id, CancellationToken ct)
    {
        var r = await _receipts.GetByIdAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(Problem(r));
    }

    [HttpPost("api/ar/receipts")]
    [RequirePermission("ar.receipts.create")]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateReceiptRequest req, CancellationToken ct)
    {
        var v = await _createReceiptV.ValidateAsync(req, ct);
        if (!v.IsValid) return BadRequest(ValidationProblem(v));
        var r = await _receipts.CreateAsync(UserId, req, ct);
        return r.Succeeded
            ? CreatedAtAction(nameof(GetReceipt), new { id = r.Value!.Id }, r.Value)
            : BadRequest(Problem(r));
    }

    [HttpPut("api/ar/receipts/{id:guid}/post")]
    [RequirePermission("ar.receipts.create")] // posting receipts = part of the receipt workflow
    public async Task<IActionResult> PostReceipt(Guid id, CancellationToken ct)
    {
        var r = await _receipts.PostAsync(UserId, id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    [HttpPut("api/ar/receipts/{id:guid}/reverse")]
    [RequirePermission("ar.receipts.create")]
    public async Task<IActionResult> ReverseReceipt(Guid id, CancellationToken ct)
    {
        var r = await _receipts.ReverseAsync(UserId, id, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    // ============== Aging ==============

    [HttpGet("api/ar/aging")]
    [RequirePermission("ar.invoices.view")]
    public async Task<IActionResult> GetAging([FromQuery] DateTime? asOfDate, CancellationToken ct = default)
    {
        var asOf = asOfDate ?? DateTime.UtcNow;
        var r = await _invoices.GetAgingReportAsync(asOf, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(Problem(r));
    }

    // ============== Sprint 56 (DEC-149 + DEC-150) — Top Customers + Top Items ==============

    [HttpGet("api/ar/reports/top-customers")]
    [RequirePermission("ar.invoices.view")]
    public async Task<IActionResult> GetTopCustomers(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var toDate = (to ?? DateTime.UtcNow).Date;
        var fromDate = (from ?? toDate.AddYears(-1)).Date;
        var r = await _topCustomers.GetTopCustomersAsync(CompanyId, fromDate, toDate, Math.Clamp(top, 1, 100), ct);
        return Ok(r);
    }

    [HttpGet("api/ar/reports/top-items")]
    [RequirePermission("ar.invoices.view")]
    public async Task<IActionResult> GetTopItems(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var toDate = (to ?? DateTime.UtcNow).Date;
        var fromDate = (from ?? toDate.AddYears(-1)).Date;
        var r = await _topCustomers.GetTopItemsAsync(CompanyId, fromDate, toDate, Math.Clamp(top, 1, 100), ct);
        return Ok(r);
    }

    // ============== Reports (Phase 6.1 — 20 mandatory reports) ==============

    private Guid CompanyId => _companyContext.CompanyId ?? throw new UnauthorizedAccessException();

    // Sprint 22: complex AR reports (Sales by Customer/Item, Top Customers) removed.
    // Reports live in their parent module. Add back later if needed.

    // ============== Helpers ==============

    private static ValidationProblemDetails ValidationProblem(FluentValidation.Results.ValidationResult v) =>
        new(v.Errors.GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
    private static ProblemDetails Problem<T>(ArResult<T> r) => new()
    {
        Title = "AR Error",
        Status = StatusCodes.Status400BadRequest,
        Detail = r.Error,
    };
}
