namespace ERPSystem.Shared.SeedData;

/// <summary>
/// Static state tracker for RealisticSeedHostedService (DEC-069).
/// Exposes progress via /api/debug/seed-status endpoint.
/// </summary>
public static class SeedDebugState
{
    public static bool ServiceConstructed { get; set; }
    public static bool ExecuteAsyncCalled { get; set; }
    public static bool SeedEnabled { get; set; }
    public static bool ConnectivityCheckPassed { get; set; }
    public static Guid? TenantId { get; set; }
    public static string CurrentStep { get; set; } = "(none)";
    public static int CompaniesInserted { get; set; }
    public static int VendorsInserted { get; set; }
    public static int CustomersInserted { get; set; }
    public static int ProjectsInserted { get; set; }
    public static int ItemsInserted { get; set; }
    public static int GoodsReceiptsInserted { get; set; }
    public static int BillsInserted { get; set; }
    public static int SalesInvoicesInserted { get; set; }
    public static int JournalEntriesInserted { get; set; }
    public static DateTime? StartedAt { get; set; }
    public static DateTime? CompletedAt { get; set; }
    public static string LastError { get; set; }
}
