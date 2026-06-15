using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Modules.Reports.Application;
using ERPSystem.Modules.Reports.Application.Services;
using ERPSystem.Tests.Common;
using FluentAssertions;

namespace ERPSystem.Tests.Reports;

/// <summary>اختبارات خدمة تقارير Project — تجمع FakeDbConnectionFactory + Fake repos</summary>
public class ProjectReportServiceTests
{
    private sealed class FakeProjectRepository : IProjectRepository
    {
        public Dictionary<Guid, Project> Items { get; } = new();
        public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Items.TryGetValue(id, out var p) ? p : null);
        public Task<Project?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct) =>
            Task.FromResult(Items.Values.FirstOrDefault(p => p.TenantId == tenantId && p.Code == code));
        public Task<IReadOnlyList<Project>> ListAsync(Guid tenantId, Guid? companyId, ProjectStatus? status, bool includeInactive, int skip, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Project>>(Items.Values.Where(p => p.TenantId == tenantId).ToList());
        public Task InsertAsync(Project project, CancellationToken ct) { Items[project.Id] = project; return Task.CompletedTask; }
        public Task UpdateAsync(Project project, CancellationToken ct) { Items[project.Id] = project; return Task.CompletedTask; }
    }

    private sealed class FakeProjectBudgetRepository : IProjectBudgetRepository
    {
        public Dictionary<Guid, ProjectBudget> Items { get; } = new();
        public Task<ProjectBudget?> GetByProjectAsync(Guid projectId, CancellationToken ct) =>
            Task.FromResult(Items.Values.FirstOrDefault(b => b.ProjectId == projectId));
        public Task<ProjectBudget?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Items.TryGetValue(id, out var b) ? b : null);
        public Task InsertAsync(ProjectBudget budget, CancellationToken ct) { Items[budget.Id] = budget; return Task.CompletedTask; }
        public Task UpdateAsync(ProjectBudget budget, CancellationToken ct) { Items[budget.Id] = budget; return Task.CompletedTask; }
        public Task<decimal> RecalculateSpentAsync(Guid projectId, Guid costCenterId, CancellationToken ct)
        {
            var b = Items.Values.FirstOrDefault(x => x.ProjectId == projectId);
            return Task.FromResult(b?.SpentAmount ?? 0);
        }
    }

    private static (ProjectReportService svc, FakeDbConnectionFactory db, FakeProjectRepository projects,
                    FakeProjectBudgetRepository budgets, Guid tenantId) Build()
    {
        var tenant = Guid.NewGuid();
        var db = new FakeDbConnectionFactory();
        var projects = new FakeProjectRepository();
        var budgets = new FakeProjectBudgetRepository();
        return (new ProjectReportService(db, projects, budgets), db, projects, budgets, tenant);
    }

    [Fact]
    public async Task GetProjectPnL_RevenueAndMaterial_CalculatesCorrectly()
    {
        var (svc, db, projects, _, tenant) = Build();
        var projectId = Guid.NewGuid();
        var costCenterId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);

        projects.Items[projectId] = new Project
        {
            Id = projectId, TenantId = tenant, CompanyId = companyId, CostCenterId = costCenterId,
            Code = "PRJ-001", Name = "مشروع اختبار", Status = ProjectStatus.Active
        };

        var revenue = Guid.NewGuid();
        var material = Guid.NewGuid();
        db.AddRow("accounts", "id", revenue, "tenant_id", tenant, "code", "4100", "name", "إيرادات", "type", (int)AccountType.Revenue);
        db.AddRow("accounts", "id", material, "tenant_id", tenant, "code", "4110", "name", "مواد", "type", (int)AccountType.Expense);
        var je = Guid.NewGuid();
        db.AddRow("journal_entries", "id", je, "tenant_id", tenant, "status", (int)JournalEntryStatus.Posted, "entry_date", to);
        db.AddRow("journal_lines", "journal_entry_id", je, "account_id", revenue, "cost_center_id", costCenterId,
            "debit", 0m, "credit", 5000m);
        db.AddRow("journal_lines", "journal_entry_id", je, "account_id", material, "cost_center_id", costCenterId,
            "debit", 2000m, "credit", 0m);

        var pnl = await svc.GetProjectPnLAsync(tenant, projectId, from, to, CancellationToken.None);

        pnl.ProjectId.Should().Be(projectId);
        pnl.Revenue.Should().Be(5000);
        pnl.MaterialCost.Should().Be(2000);
        pnl.DirectCosts.Should().Be(2000, "MaterialCost فقط، لا توجد تكاليف أخرى");
        pnl.NetProfit.Should().Be(3000, "5000 - 2000 = 3000");
    }

    [Fact]
    public async Task GetProjectPnL_ProjectNotFound_ReturnsPlaceholder()
    {
        var (svc, _, _, _, tenant) = Build();

        var pnl = await svc.GetProjectPnLAsync(tenant, Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, CancellationToken.None);

        pnl.ProjectCode.Should().Be("—");
        pnl.Revenue.Should().Be(0);
        pnl.NetProfit.Should().Be(0);
    }

    [Fact]
    public async Task GetBudgetVsActual_WithBudget_ReturnsSpentAndAvailable()
    {
        var (svc, _, projects, budgets, tenant) = Build();
        var projectId = Guid.NewGuid();

        projects.Items[projectId] = new Project
        {
            Id = projectId, TenantId = tenant, CompanyId = Guid.NewGuid(), CostCenterId = Guid.NewGuid(),
            Code = "PRJ-A", Name = "مشروع A"
        };
        budgets.Items[Guid.NewGuid()] = new ProjectBudget
        {
            Id = Guid.NewGuid(), ProjectId = projectId, TenantId = tenant,
            BudgetAmount = 100_000m, SpentAmount = 30_000m, CommittedAmount = 10_000m
        };

        var bva = await svc.GetBudgetVsActualAsync(tenant, projectId, CancellationToken.None);

        bva.BudgetAmount.Should().Be(100_000);
        bva.SpentAmount.Should().Be(30_000);
        bva.AvailableAmount.Should().Be(60_000, "100k - 30k - 10k = 60k");
        bva.Variance.Should().Be(70_000);
        bva.UtilizationPercent.Should().Be(30m);
    }

    [Fact]
    public async Task GetProjectsSummary_ActiveProjects_ReturnsMargin()
    {
        var (svc, db, projects, _, tenant) = Build();
        var projectId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var costCenterId = Guid.NewGuid();

        projects.Items[projectId] = new Project
        {
            Id = projectId, TenantId = tenant, CompanyId = companyId, CostCenterId = costCenterId,
            Code = "PRJ-X", Name = "مشروع X", Status = ProjectStatus.Active, Budget = 50_000m, IsActive = true
        };
        // simulate spent via project_budgets (اختياري - SQL تعتمد على LEFT JOIN)
        db.AddRow("projects", "id", projectId, "tenant_id", tenant, "company_id", companyId, "code", "PRJ-X", "name", "مشروع X", "status", 2, "budget", 50000m, "is_active", true);
        db.AddRow("project_budgets", "project_id", projectId, "spent_amount", 10000m);

        var summaries = await svc.GetProjectsSummaryAsync(tenant, null, CancellationToken.None);

        summaries.Should().HaveCount(1);
        summaries[0].Budget.Should().Be(50_000);
        summaries[0].Spent.Should().Be(10_000);
        summaries[0].MarginPercent.Should().Be(80m, "(50000 - 10000) / 50000 * 100");
    }

    [Fact]
    public void ProjectPnL_Dto_MarginPercent_CalculatesCorrectly()
    {
        var pnl = new ProjectPnL { Revenue = 1000, MaterialCost = 300, LaborCost = 200, SubcontractorCost = 100, AllocatedOverhead = 50 };
        pnl.DirectCosts.Should().Be(600);
        pnl.NetProfit.Should().Be(350, "1000 - 600 - 50");
        pnl.MarginPercent.Should().Be(35m);
    }

    [Fact]
    public void ProjectBudgetVsActual_Dto_VariancePercent_CalculatesCorrectly()
    {
        var bva = new ProjectBudgetVsActual { BudgetAmount = 200_000, SpentAmount = 50_000, CommittedAmount = 20_000 };
        bva.Variance.Should().Be(150_000);
        bva.VariancePercent.Should().Be(75m);
        bva.UtilizationPercent.Should().Be(25m);
    }
}
