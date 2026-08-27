using Dapper;
using ERPSystem.Modules.AccountsReceivable.Application;
using ERPSystem.Modules.AccountsReceivable.Application.Services;
using ERPSystem.Modules.Projects.Application.Events;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Handlers;

public interface IBillingApprovedHandler
{
    Task HandleAsync(BillingApprovedEvent evt, CancellationToken ct);
}

/// <summary>
/// Sprint 65 / DEC-231 (Wave 1A): Subscribes to <see cref="BillingApprovedEvent"/>.
///
/// <para>Responsibility: ensure the AR side effects (Sales Invoice + Journal Entry) exist for a
/// freshly-approved Progress Billing. In the current flow, <c>BillingService.ApproveAsync</c>
/// already does this work inline (atomic transaction). This handler exists as the cross-module
/// extension point and as a safety net — it re-checks the billing's <c>InvoiceId</c> and no-ops
/// when the work is already done (idempotent).</para>
///
/// <para><b>L19 / DEC-095 compliance:</b> <c>CompanyId</c> from the event payload (already
/// resolved by the firing service from <c>ICompanyContext</c>). <c>UserId</c> from the event
/// payload (which was the JWT user in the firing service).</para>
/// </summary>
public sealed class BillingApprovedHandler : IBillingApprovedHandler
{
    private readonly ISalesInvoiceService _arInvoices;
    private readonly IBillingRepository _billings;
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<BillingApprovedHandler> _logger;

    public BillingApprovedHandler(
        ISalesInvoiceService arInvoices,
        IBillingRepository billings,
        IDbConnectionFactory db,
        ILogger<BillingApprovedHandler> logger)
    {
        _arInvoices = arInvoices;
        _billings = billings;
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(BillingApprovedEvent evt, CancellationToken ct)
    {
        // 1) Load billing — re-check after the event was fired
        var billing = await _billings.GetByIdAsync(evt.BillingId, ct);
        if (billing == null)
        {
            _logger.LogWarning("Finance trigger: billing {Id} not found (deleted?) — no-op", evt.BillingId);
            return;
        }

        // 2) Idempotency: if the inline approve flow already set the invoice, we're done.
        if (billing.InvoiceId.HasValue && billing.InvoiceId != Guid.Empty)
        {
            _logger.LogDebug("Finance trigger: billing {Id} already has invoice {InvoiceId} — no-op",
                billing.Id, billing.InvoiceId);
            return;
        }

        // 3) Resolve customer from project (the event does not carry customer id).
        Guid? customerId = await ResolveProjectCustomerIdAsync(evt.ProjectId, ct);
        if (customerId == null)
        {
            _logger.LogWarning("Finance trigger: project {ProjectId} has no customer — skipping AR for billing {Id}",
                evt.ProjectId, billing.Id);
            return;
        }

        // 4) Create the AR Sales Invoice (Draft, no post — the existing inline flow already
        //    posts via direct SQL).
        var invoiceReq = new CreateSalesInvoiceRequest
        {
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CustomerId = customerId.Value,
            CurrencyCode = "LYD",
            ExchangeRate = 1m,
            PostImmediately = false,
            Lines = new List<CreateSalesInvoiceLineRequest>
            {
                new()
                {
                    Description = $"Progress Billing {billing.BillingNumber}",
                    Quantity = 1m,
                    UnitPrice = evt.NetAmount,
                    TaxRate = 0m,
                }
            }
        };

        var invoiceResult = await _arInvoices.CreateAsync(evt.UserId, invoiceReq, ct);
        if (!invoiceResult.Succeeded)
        {
            _logger.LogError("Finance trigger: AR invoice create failed for billing {Id}: {Error}",
                billing.Id, invoiceResult.Error);
            return;
        }

        // 5) Persist the back-link. Use the existing UpdateStatusAsync (idempotent — same status).
        await _billings.UpdateStatusAsync(
            billing.Id,
            Entities.BillingStatus.Invoiced,
            invoiceResult.Value!.Id,
            billing.JournalEntryId, // Preserve the JE that the inline flow already created
            evt.UserId,
            ct);

        _logger.LogInformation(
            "Finance trigger: ProgressBilling {Id} → AR Invoice {InvoiceId} (inline JE preserved: {JeId})",
            billing.Id, invoiceResult.Value.Id, billing.JournalEntryId);
    }

    private async Task<Guid?> ResolveProjectCustomerIdAsync(Guid projectId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<Guid?>(
            new CommandDefinition(
                "SELECT customer_id FROM projects WHERE id = @ProjectId LIMIT 1",
                new { ProjectId = projectId },
                cancellationToken: ct));
    }
}
