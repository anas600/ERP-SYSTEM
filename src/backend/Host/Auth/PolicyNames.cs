// DEC-053: Centralized policy names for role-based access control.
// Policies map to a set of allowed roles. Use via [Authorize(Policy = "...")].

namespace ERPSystem.Host.Auth;

public static class PolicyNames
{
    // Role-based policies
    public const string AdminOnly = "AdminOnly";
    public const string AdminOrAccountant = "AdminOrAccountant";
    public const string AdminOrProjectManager = "AdminOrProjectManager";
    public const string AnyAuthenticated = "AnyAuthenticated";

    // Resource-action policies
    public const string ReadAccess = "ReadAccess";          // Admin, Accountant, ProjectManager, Viewer
    public const string WriteFinance = "WriteFinance";      // Admin, Accountant
    public const string WriteProjects = "WriteProjects";    // Admin, ProjectManager
    public const string WriteStock = "WriteStock";          // Admin, Accountant, ProjectManager
    public const string WriteMasterData = "WriteMasterData"; // Admin only
    public const string WriteAdmin = "WriteAdmin";          // Admin only

    // Module-level aliases (DEC-053 P1.5)
    public const string HRWrite = "HR.Write";                  // Admin only
    public const string FinanceWrite = "Finance.Write";        // Admin, Accountant
    public const string ProcurementWrite = "Procurement.Write"; // Admin, Accountant
    public const string InventoryWrite = "Inventory.Write";     // Admin, Accountant, ProjectManager
    public const string EventsWrite = "Events.Write";          // Admin, Accountant
    public const string AuditRead = "Audit.Read";               // Admin, Accountant
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Accountant = "Accountant";
    public const string ProjectManager = "ProjectManager";
    public const string Viewer = "Viewer";
}
