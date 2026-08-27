using System.Data;
using Dapper;
using ERPSystem.Modules.Finance.Infrastructure; // IAccountRepository
using ERPSystem.Modules.Projects.Application;
using ERPSystem.Modules.Projects.Application.Events;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Application.Services;

public interface IBillingService
{
    Task<ProjectResult<IReadOnlyList<ProgressBillingResponse>>> ListByProjectAsync(Guid projectId, CancellationToken ct);
    Task<ProjectResult<ProgressBillingResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    /// <summary>يحسب الأرقام المتوقعة بدون إنشاء فعلي — يُستخدم في الـ UI live preview.</summary>
    Task<ProjectResult<BillingPreviewResponse>> PreviewAsync(Guid contractId, decimal workCompletedPercent, CancellationToken ct);
    Task<ProjectResult<ProgressBillingResponse>> CreateAsync(Guid userId, Guid projectId, CreateBillingRequest req, CancellationToken ct);
    Task<ProjectResult<ProgressBillingResponse>> ApproveAsync(Guid userId, Guid billingId, CancellationToken ct);
    Task<ProjectResult<ProgressBillingResponse>> CancelAsync(Guid userId, Guid billingId, CancellationToken ct);
    /// <summary>Sprint 58 / DEC-165: WIP = totalCosts − totalBilledNet.</summary>
    Task<ProjectResult<WipResponse>> GetWipAsync(Guid projectId, CancellationToken ct);
}

/// <summary>
/// Sprint 58 / DEC-164: Progress Billing service.
///
/// الـ billing algorithm:
///   gross = contract.contract_value × (work_completed_percent / 100)
///   previous_advance_sum = SUM(advance_deducted) WHERE status != 'CANCELLED'
///   total_advance = contract.contract_value × (contract.advance_percent / 100)
///   remaining_advance = MAX(0, total_advance − previous_advance_sum)
///   advance_deducted = MIN(gross, remaining_advance)  // تُخصم مرة واحدة فقط
///
///   next_billing_number = COUNT(*) + 1 WHERE status != 'CANCELLED'
///   retention_deducted = (next_billing_number >= contract.retention_start_billing)
///       ? gross × (contract.retention_percent / 100) : 0
///   net = gross − advance_deducted − retention_deducted
///
/// الـ Approve flow (atomic): ينشئ sales_invoice + journal_entry + يحدّث الـ billing
/// في transaction واحد. لو فشل أي شيء، كله يتراجع.
/// </summary>
public sealed class BillingService : IBillingService
{
    private readonly IBillingRepository _billings;
    private readonly IContractRepository _contracts;
    private readonly IProjectRepository _projects;
    private readonly IAccountRepository _accounts; // من Finance module
    private readonly IRegionalPremiumService _regionalPremiums; // Sprint 62 / DEC-197
    private readonly IDbConnectionFactory _db;
    private readonly ICompanyContext _companyContext;
    private readonly IProjectEventBus _eventBus; // Sprint 65 / DEC-231
    private readonly ILogger<BillingService> _logger;

    // COA codes المستخدمة في الـ approve
    // Sprint 58b: updated to professional 4-level CoA. 1201 = AR (control), 4301 = Project Revenue (control).
    // The billing service resolves to a specific L4 detail account (e.g. 1201-001) at runtime via
    // the project's customer → detail-account mapping in the seeder.
    private const string ArControlCode = "1201";       // AR control account
    private const string RevenueControlCode = "4301";  // Project Revenue control account
    private const string ArAccountCode = ArControlCode;       // legacy alias kept for callers
    private const string RevenueAccountCode = RevenueControlCode; // legacy alias kept for callers

    public BillingService(
        IBillingRepository billings,
        IContractRepository contracts,
        IProjectRepository projects,
        IAccountRepository accounts,
        IRegionalPremiumService regionalPremiums,
        IDbConnectionFactory db,
        ICompanyContext companyContext,
        IProjectEventBus eventBus, // Sprint 65 / DEC-231
        ILogger<BillingService> logger)
    {
        _billings = billings; _contracts = contracts; _projects = projects;
        _accounts = accounts; _regionalPremiums = regionalPremiums;
        _db = db; _companyContext = companyContext; _logger = logger;
        _accounts = accounts; _db = db; _companyContext = companyContext;
        _eventBus = eventBus; // Sprint 65 / DEC-231
        _logger = logger;
    }

    public async Task<ProjectResult<IReadOnlyList<ProgressBillingResponse>>> ListByProjectAsync(Guid projectId, CancellationToken ct)
    {
        var rows = await _billings.ListByProjectAsync(projectId, ct);
        return ProjectResult<IReadOnlyList<ProgressBillingResponse>>.Ok(rows.Select(MapToResponse).ToList());
    }

    public async Task<ProjectResult<ProgressBillingResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var b = await _billings.GetByIdAsync(id, ct);
        if (b == null)
            return ProjectResult<ProgressBillingResponse>.Fail("المستخلص غير موجود.", ProjectErrorCode.NotFound);
        return ProjectResult<ProgressBillingResponse>.Ok(MapToResponse(b));
    }

    public async Task<ProjectResult<BillingPreviewResponse>> PreviewAsync(Guid contractId, decimal workCompletedPercent, CancellationToken ct)
    {
        var contract = await _contracts.GetByIdAsync(contractId, ct);
        if (contract == null)
            return ProjectResult<BillingPreviewResponse>.Fail("العقد غير موجود.", ProjectErrorCode.NotFound);

        var calc = await CalculateAmountsAsync(contract, workCompletedPercent, ct);
        // Sprint 62 / DEC-197: regional premium applied on gross, after advance/retention.
        var premium = await _regionalPremiums.CalculateDeductionAsync(contract.ProjectId, calc.gross, ct);
        return ProjectResult<BillingPreviewResponse>.Ok(new BillingPreviewResponse
        {
            GrossAmount = calc.gross,
            AdvanceDeducted = calc.advance,
            RetentionDeducted = calc.retention,
            NetAmount = calc.net,
            RegionalPremiumDeducted = premium,
            NetAmountAfterPremium = Math.Round(calc.net - premium, 4),
            PreviousMaxPercent = calc.prevMax,
            NextBillingNumber = calc.nextNumber,
        });
    }

    public async Task<ProjectResult<ProgressBillingResponse>> CreateAsync(Guid userId, Guid projectId, CreateBillingRequest req, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        // 1) validation
        if (string.IsNullOrWhiteSpace(req.BillingNumber))
            return ProjectResult<ProgressBillingResponse>.Fail("رقم المستخلص مطلوب.", ProjectErrorCode.ValidationError);
        if (req.WorkCompletedPercent <= 0 || req.WorkCompletedPercent > 100)
            return ProjectResult<ProgressBillingResponse>.Fail("نسبة الإنجاز يجب أن تكون بين 0 و 100.", ProjectErrorCode.ValidationError);

        // 2) المشروع + العقد
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project == null)
            return ProjectResult<ProgressBillingResponse>.Fail("المشروع غير موجود.", ProjectErrorCode.NotFound);

        var contract = await _contracts.GetByProjectAsync(projectId, ct);
        if (contract == null)
            return ProjectResult<ProgressBillingResponse>.Fail("لا يوجد عقد على هذا المشروع. أنشئ عقد أولاً.", ProjectErrorCode.NotFound);

        // 3) رقم المستخلص unique per company
        if (await _billings.BillingNumberExistsAsync(req.BillingNumber, companyId, ct))
            return ProjectResult<ProgressBillingResponse>.Fail("رقم المستخلص مستخدم بالفعل.", ProjectErrorCode.AlreadyExists);

        // 4) حساب الأرقام
        var c = await CalculateAmountsAsync(contract, req.WorkCompletedPercent, ct);
        if (req.WorkCompletedPercent < c.prevMax)
            return ProjectResult<ProgressBillingResponse>.Fail(
                $"نسبة الإنجاز ({req.WorkCompletedPercent}%) أقل من أعلى نسبة سابقة ({c.prevMax}%).", ProjectErrorCode.ValidationError);

        // 4b) Sprint 62 / DEC-197: regional premium (NDB + CIT + SS) on gross.
        var regionalPremiumDeducted = await _regionalPremiums.CalculateDeductionAsync(projectId, c.gross, ct);
        var netAfterPremium = Math.Round(c.net - regionalPremiumDeducted, 4);

        // 5) إنشاء المسودة
        var now = DateTime.UtcNow;
        var billing = new ProgressBilling
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProjectId = projectId,
            ContractId = contract.Id,
            BillingNumber = req.BillingNumber.Trim(),
            BillingDate = req.BillingDate,
            PeriodFrom = req.PeriodFrom,
            PeriodTo = req.PeriodTo,
            WorkCompletedPercent = req.WorkCompletedPercent,
            GrossAmount = c.gross,
            AdvanceDeducted = c.advance,
            RetentionDeducted = c.retention,
            NetAmount = c.net,
            RegionalPremiumDeducted = regionalPremiumDeducted,
            NetAmountAfterPremium = netAfterPremium,
            Status = BillingStatus.Draft,
            Notes = req.Notes?.Trim(),
            CreatedAt = now, CreatedBy = userId, UpdatedAt = now, UpdatedBy = userId,
        };
        await _billings.InsertAsync(billing, ct);
        _logger.LogInformation("تم إنشاء مسودة مستخلص {Number} للمشروع {ProjectId}: net={Net}, premium={Premium}, netAfter={NetAfter}",
            billing.BillingNumber, projectId, billing.NetAmount, billing.RegionalPremiumDeducted, billing.NetAmountAfterPremium);
        return ProjectResult<ProgressBillingResponse>.Ok(MapToResponse(billing));
    }

    public async Task<ProjectResult<ProgressBillingResponse>> ApproveAsync(Guid userId, Guid billingId, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        var billing = await _billings.GetByIdAsync(billingId, ct);
        if (billing == null)
            return ProjectResult<ProgressBillingResponse>.Fail("المستخلص غير موجود.", ProjectErrorCode.NotFound);
        if (billing.Status != BillingStatus.Draft)
            return ProjectResult<ProgressBillingResponse>.Fail("المستخلص ليس في حالة مسودة.", ProjectErrorCode.InvalidStatusTransition);

        var project = await _projects.GetByIdAsync(billing.ProjectId, ct);
        if (project == null)
            return ProjectResult<ProgressBillingResponse>.Fail("المشروع غير موجود.", ProjectErrorCode.NotFound);
        if (project.CustomerId == null)
            return ProjectResult<ProgressBillingResponse>.Fail("المشروع لا يحتوي على عميل — لا يمكن إصدار فاتورة.", ProjectErrorCode.ValidationError);

        // AR + Revenue accounts
        var arAccount = await _accounts.GetByCodeAsync(ArAccountCode, ct);
        var revenueAccount = await _accounts.GetByCodeAsync(RevenueAccountCode, ct);
        if (arAccount == null || revenueAccount == null)
            return ProjectResult<ProgressBillingResponse>.Fail(
                $"حساب ضروري غير موجود: {(arAccount == null ? ArAccountCode : RevenueAccountCode)}. أضفه في شجرة الحسابات أولاً.",
                ProjectErrorCode.Internal);

        // Atomic transaction
        using var conn = (Npgsql.NpgsqlConnection)await _db.CreateOltpConnectionAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // 1) Create sales_invoice (Posted مباشرة)
            var invoiceId = Guid.NewGuid();
            var invoiceNumber = billing.BillingNumber; // نستخدم نفس رقم المستخلص
            const string invoiceSql = @"
                INSERT INTO sales_invoices (id, company_id, customer_id, invoice_number, invoice_date,
                                            due_date, currency_code, exchange_rate, subtotal, tax_amount, total_amount,
                                            paid_amount, status, is_deleted, project_id, posted_at, posted_by,
                                            created_at, created_by, updated_at, updated_by)
                VALUES (@Id, @CompanyId, @CustomerId, @InvoiceNumber, @InvoiceDate,
                        @DueDate, 'LYD', 1, @Subtotal, 0, @TotalAmount,
                        0, 'Posted', false, @ProjectId, NOW(), @PostedBy,
                        NOW(), @CreatedBy, NOW(), @UpdatedBy)";
            await conn.ExecuteAsync(new CommandDefinition(invoiceSql, new
            {
                Id = invoiceId,
                CompanyId = companyId,
                CustomerId = project.CustomerId.Value,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = billing.BillingDate,
                DueDate = billing.BillingDate.AddDays(30),
                Subtotal = billing.NetAmount,
                TotalAmount = billing.NetAmount,
                ProjectId = billing.ProjectId,
                PostedBy = userId,
                CreatedBy = userId,
                UpdatedBy = userId,
            }, transaction: tx, cancellationToken: ct));

            // 2) Create journal_entry (DR 1103 AR / CR 4101 Revenue) + 2 lines
            var jeId = Guid.NewGuid();
            var jeNumber = await GetNextJournalEntryNumberAsync(conn, tx, ct);
            const string jeHeaderSql = @"
                INSERT INTO journal_entries (id, entry_number, company_id, project_id, entry_date, description, reference,
                                            status, created_by_user_id, posted_at, created_at, updated_at)
                VALUES (@Id, @EntryNumber, @CompanyId, @ProjectId, @EntryDate, @Description, @Reference,
                        2, @CreatedBy, NOW(), NOW(), NOW())";
            await conn.ExecuteAsync(new CommandDefinition(jeHeaderSql, new
            {
                Id = jeId,
                EntryNumber = jeNumber,
                CompanyId = companyId,
                ProjectId = billing.ProjectId,
                EntryDate = billing.BillingDate,
                Description = $"مستخلص {billing.BillingNumber} - مشروع {project.Code}",
                Reference = billing.BillingNumber,
                CreatedBy = userId,
            }, transaction: tx, cancellationToken: ct));

            const string lineSql = @"
                INSERT INTO journal_lines (id, journal_entry_id, account_id, company_id, debit, credit, description, line_number)
                VALUES (@Id, @JournalEntryId, @AccountId, @CompanyId, @Debit, @Credit, @Description, @LineNumber)";
            // Line 1: DR AR
            await conn.ExecuteAsync(new CommandDefinition(lineSql, new
            {
                Id = Guid.NewGuid(),
                JournalEntryId = jeId,
                AccountId = arAccount.Id,
                CompanyId = companyId,
                Debit = billing.NetAmount,
                Credit = 0m,
                Description = $"مدينون - مستخلص {billing.BillingNumber}",
                LineNumber = 1,
            }, transaction: tx, cancellationToken: ct));
            // Line 2: CR Revenue
            await conn.ExecuteAsync(new CommandDefinition(lineSql, new
            {
                Id = Guid.NewGuid(),
                JournalEntryId = jeId,
                AccountId = revenueAccount.Id,
                CompanyId = companyId,
                Debit = 0m,
                Credit = billing.NetAmount,
                Description = $"إيرادات مبيعات - مستخلص {billing.BillingNumber}",
                LineNumber = 2,
            }, transaction: tx, cancellationToken: ct));

            // 3) Update sales_invoice.journal_entry_id (back-link)
            const string invoiceUpdateSql = "UPDATE sales_invoices SET journal_entry_id = @JeId WHERE id = @InvoiceId";
            await conn.ExecuteAsync(new CommandDefinition(invoiceUpdateSql,
                new { JeId = jeId, InvoiceId = invoiceId }, transaction: tx, cancellationToken: ct));

            // 4) Update billing status
            const string billingUpdateSql = @"
                UPDATE progress_billings
                SET status = 'INVOICED', invoice_id = @InvoiceId, journal_entry_id = @JeId,
                    updated_at = NOW(), updated_by = @UpdatedBy
                WHERE id = @Id AND status = 'DRAFT'";
            await conn.ExecuteAsync(new CommandDefinition(billingUpdateSql, new
            {
                Id = billing.Id, InvoiceId = invoiceId, JeId = jeId, UpdatedBy = userId
            }, transaction: tx, cancellationToken: ct));

            await tx.CommitAsync(ct);
            _logger.LogInformation("تم ترحيل المستخلص {Number}: invoice={InvoiceId}, je={JeId}", billing.BillingNumber, invoiceId, jeId);

            // ===== Sprint 65 / DEC-231: fire BillingApprovedEvent (Wave 1A) =====
            // The handler is idempotent — it re-checks billing.InvoiceId and no-ops if the
            // inline flow above already produced the AR invoice.
            try
            {
                await _eventBus.PublishAsync(new BillingApprovedEvent(
                    billingId: billing.Id,
                    projectId: billing.ProjectId,
                    companyId: billing.CompanyId,
                    netAmount: billing.NetAmount,
                    userId: userId), ct);
            }
            catch (Exception evtEx)
            {
                // Event publish failure must not break the approve result — the inline work
                // is the source of truth. The handler is a safety net; the log entry makes
                // the gap auditable.
                _logger.LogError(evtEx,
                    "Sprint 65: BillingApprovedEvent publish failed for billing {Id} — inline work is committed, handler may be stale",
                    billing.Id);
            }
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "فشل ترحيل المستخلص {Id}", billing.Id);
            return ProjectResult<ProgressBillingResponse>.Fail($"فشل الترحيل: {ex.Message}", ProjectErrorCode.Internal);
        }

        // Re-load
        var updated = await _billings.GetByIdAsync(billingId, ct);
        return ProjectResult<ProgressBillingResponse>.Ok(MapToResponse(updated!));
    }

    public async Task<ProjectResult<ProgressBillingResponse>> CancelAsync(Guid userId, Guid billingId, CancellationToken ct)
    {
        var billing = await _billings.GetByIdAsync(billingId, ct);
        if (billing == null)
            return ProjectResult<ProgressBillingResponse>.Fail("المستخلص غير موجود.", ProjectErrorCode.NotFound);
        if (billing.Status != BillingStatus.Draft)
            return ProjectResult<ProgressBillingResponse>.Fail("يمكن إلغاء المسودة فقط — هذا المستخلص مُرحّل بالفعل.", ProjectErrorCode.InvalidStatusTransition);

        await _billings.UpdateStatusAsync(billingId, BillingStatus.Cancelled, null, null, userId, ct);
        _logger.LogInformation("تم إلغاء المستخلص {Id} بواسطة {UserId}", billingId, userId);
        var updated = await _billings.GetByIdAsync(billingId, ct);
        return ProjectResult<ProgressBillingResponse>.Ok(MapToResponse(updated!));
    }

    public async Task<ProjectResult<WipResponse>> GetWipAsync(Guid projectId, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project == null)
            return ProjectResult<WipResponse>.Fail("المشروع غير موجود.", ProjectErrorCode.NotFound);

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // 1) Total costs — من journal_lines على Expense accounts، مرتبطة بـ journal_entries.project_id
        const string costsSql = @"
            SELECT COALESCE(SUM(GREATEST(jl.debit - jl.credit, 0)), 0) AS TotalCosts
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a ON a.id = jl.account_id
            WHERE je.project_id = @ProjectId
              AND je.status = 2
              AND a.type = 5;";
        var totalCosts = await conn.ExecuteScalarAsync<decimal>(
            new CommandDefinition(costsSql, new { ProjectId = projectId }, cancellationToken: ct));

        // 2) Total billed net + total retention held — من billings المُرحّلة
        const string billedSql = @"
            SELECT COALESCE(SUM(net_amount), 0) AS TotalBilledNet,
                   COALESCE(SUM(retention_deducted), 0) AS TotalRetentionHeld
            FROM progress_billings
            WHERE project_id = @ProjectId
              AND status = 'INVOICED';";  // status column is varchar
        var billed = await conn.QueryFirstOrDefaultAsync<(decimal TotalBilledNet, decimal TotalRetentionHeld)>(
            new CommandDefinition(billedSql, new { ProjectId = projectId }, cancellationToken: ct));

        var wip = totalCosts - billed.TotalBilledNet;
        var status = wip > 0 ? "COSTS_EXCEED_BILLED"
                    : wip < 0 ? "BILLED_EXCEED_COSTS"
                    : "BALANCED";

        return ProjectResult<WipResponse>.Ok(new WipResponse
        {
            ProjectId = projectId,
            ProjectCode = project.Code,
            ProjectName = project.Name,
            TotalCosts = totalCosts,
            TotalBilledNet = billed.TotalBilledNet,
            TotalRetentionHeld = billed.TotalRetentionHeld,
            Wip = wip,
            Status = status,
        });
    }

    // ===== Helpers =====

    private async Task<(decimal gross, decimal advance, decimal retention, decimal net, decimal prevMax, int nextNumber)>
        CalculateAmountsAsync(Contract contract, decimal workCompletedPercent, CancellationToken ct)
    {
        var previousAdvance = await _billings.SumAdvanceDeductedAsync(contract.Id, ct);
        var prevMax = await _billings.MaxPercentAsync(contract.Id, ct);
        var nonCancelledCount = await _billings.CountNonCancelledAsync(contract.Id, ct);
        var nextNumber = nonCancelledCount + 1;

        var gross = Math.Round(contract.ContractValue * (workCompletedPercent / 100m), 4);
        var totalAdvance = Math.Round(contract.ContractValue * (contract.AdvancePercent / 100m), 4);
        var remainingAdvance = Math.Max(0m, totalAdvance - previousAdvance);
        var advance = Math.Min(gross, remainingAdvance);
        var retention = nextNumber >= contract.RetentionStartBilling
            ? Math.Round(gross * (contract.RetentionPercent / 100m), 4)
            : 0m;
        var net = Math.Round(gross - advance - retention, 4);

        return (gross, advance, retention, net, prevMax, nextNumber);
    }

    private async Task<string> GetNextJournalEntryNumberAsync(IDbConnection conn, IDbTransaction tx, CancellationToken ct)
    {
        const string sql = @"
            SELECT entry_number FROM journal_entries
            WHERE entry_number LIKE @Prefix
            ORDER BY entry_number DESC LIMIT 1";
        var year = DateTime.UtcNow.Year;
        var prefix = $"JE-{year}-";
        var last = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(sql,
            new { Prefix = prefix + "%" }, transaction: tx, cancellationToken: ct));
        if (last == null) return $"{prefix}0001";
        var lastSeq = last.Substring(prefix.Length);
        return int.TryParse(lastSeq, out var n) ? $"{prefix}{(n + 1):D4}" : $"{prefix}0001";
    }

    private static ProgressBillingResponse MapToResponse(ProgressBilling b) => new()
    {
        Id = b.Id, CompanyId = b.CompanyId, ProjectId = b.ProjectId, ContractId = b.ContractId,
        BillingNumber = b.BillingNumber, BillingDate = b.BillingDate,
        PeriodFrom = b.PeriodFrom, PeriodTo = b.PeriodTo,
        WorkCompletedPercent = b.WorkCompletedPercent,
        GrossAmount = b.GrossAmount, AdvanceDeducted = b.AdvanceDeducted,
        RetentionDeducted = b.RetentionDeducted, NetAmount = b.NetAmount,
        RegionalPremiumDeducted = b.RegionalPremiumDeducted,
        NetAmountAfterPremium = b.NetAmountAfterPremium,
        Status = (int)b.Status,
        InvoiceId = b.InvoiceId, JournalEntryId = b.JournalEntryId, Notes = b.Notes,
        CreatedAt = b.CreatedAt, UpdatedAt = b.UpdatedAt,
    };
}
