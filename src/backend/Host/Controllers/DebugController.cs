using Microsoft.AspNetCore.Authorization;
using ERPSystem.Shared.SeedData;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Debug endpoints — exposes internal state for diagnostics (DEC-069).
/// Admin-only.
/// </summary>
[ApiController]
[Route("api/debug")]
[Authorize(Roles = "Admin")]
public class DebugController : ControllerBase
{
    [HttpGet("seed-status")]
    public IActionResult GetSeedStatus()
    {
        return Ok(new
        {
            seed_debug = new
            {
                service_constructed = SeedDebugState.ServiceConstructed,
                execute_async_called = SeedDebugState.ExecuteAsyncCalled,
                seed_enabled = SeedDebugState.SeedEnabled,
                connectivity_check_passed = SeedDebugState.ConnectivityCheckPassed,
                tenant_id = SeedDebugState.TenantId,
                current_step = SeedDebugState.CurrentStep,
                counts = new
                {
                    companies = SeedDebugState.CompaniesInserted,
                    vendors = SeedDebugState.VendorsInserted,
                    customers = SeedDebugState.CustomersInserted,
                    projects = SeedDebugState.ProjectsInserted,
                    items = SeedDebugState.ItemsInserted,
                    goods_receipts = SeedDebugState.GoodsReceiptsInserted,
                    bills = SeedDebugState.BillsInserted,
                    sales_invoices = SeedDebugState.SalesInvoicesInserted,
                    journal_entries = SeedDebugState.JournalEntriesInserted
                },
                started_at = SeedDebugState.StartedAt,
                completed_at = SeedDebugState.CompletedAt,
                last_error = SeedDebugState.LastError
            }
        });
    }
}
