using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Finance.Infrastructure;
using ERPSystem.Shared.CompanyContext;

namespace ERPSystem.Modules.Finance.Application.Services;

public sealed class ChartOfAccountsService : IChartOfAccountsService
{
    private readonly IAccountRepository _accounts;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<ChartOfAccountsService> _logger;

    public ChartOfAccountsService(
        IAccountRepository accounts,
        ICompanyContext companyContext,
        ILogger<ChartOfAccountsService> logger)
    {
        _accounts = accounts;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task<FinanceResult<AccountResponse>> CreateAsync(CreateAccountRequest request, CancellationToken ct)
    {
        // Sprint 41 (DEC-127): resolve companyId from JWT context — never trust the DTO.
        if (!_companyContext.IsResolved || _companyContext.CompanyId is not { } companyId)
        {
            return FinanceResult<AccountResponse>.Fail(
                "لا يمكن إنشاء حساب بدون تحديد الشركة النشطة. أعد تسجيل الدخول أو اختر شركة من القائمة.",
                FinanceErrorCode.ValidationError);
        }

        if (await _accounts.GetByCodeAsync(request.Code, companyId, ct) != null)
        {
            return FinanceResult<AccountResponse>.Fail(
                $"كود الحساب '{request.Code}' مستخدم بالفعل.",
                FinanceErrorCode.AlreadyExists);
        }

        Guid? parentId = null;
        if (request.ParentAccountId.HasValue)
        {
            var parent = await _accounts.GetByIdAsync(request.ParentAccountId.Value, ct);
            if (parent == null)
            {
                return FinanceResult<AccountResponse>.Fail("الحساب الأب غير موجود.", FinanceErrorCode.NotFound);
            }
            // Parent must belong to the same company.
            if (parent.CompanyId != companyId)
            {
                return FinanceResult<AccountResponse>.Fail("الحساب الأب لا ينتمي للشركة النشطة.", FinanceErrorCode.TenantMismatch);
            }
            parentId = parent.Id;
        }

        var now = DateTime.UtcNow;
        var acc = new Account
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Type = request.Type,
            NormalBalance = request.Type switch
            {
                AccountType.Asset or AccountType.Expense => NormalBalance.Debit,
                _ => NormalBalance.Credit
            },
            ParentAccountId = parentId,
            IsPostable = request.IsPostable,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _accounts.InsertAsync(acc, ct);
        _logger.LogInformation("تم إنشاء حساب جديد {Code} في الشركة {CompanyId}", acc.Code, companyId);
        return FinanceResult<AccountResponse>.Ok(MapToResponse(acc));
    }

    public async Task<FinanceResult<AccountResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var acc = await _accounts.GetByIdAsync(id, ct);
        if (acc == null)
        {
            return FinanceResult<AccountResponse>.Fail("الحساب غير موجود.", FinanceErrorCode.NotFound);
        }
        return FinanceResult<AccountResponse>.Ok(MapToResponse(acc));
    }

    public async Task<FinanceResult<AccountResponse>> GetByCodeAsync(string code, CancellationToken ct)
    {
        var acc = await _accounts.GetByCodeAsync(code, ct);
        if (acc == null) return FinanceResult<AccountResponse>.Fail("الحساب غير موجود.", FinanceErrorCode.NotFound);
        return FinanceResult<AccountResponse>.Ok(MapToResponse(acc));
    }

    public async Task<FinanceResult<IReadOnlyList<AccountResponse>>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        var list = await _accounts.ListAsync(includeInactive, ct);
        return FinanceResult<IReadOnlyList<AccountResponse>>.Ok(list.Select(MapToResponse).ToList());
    }

    public async Task<FinanceResult<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        var acc = await _accounts.GetByIdAsync(id, ct);
        if (acc == null)
        {
            return FinanceResult<bool>.Fail("الحساب غير موجود.", FinanceErrorCode.NotFound);
        }

        // ممنوع الحذف إن كان عليه حركات
        var postings = await _accounts.CountPostingsAsync(id, ct);
        if (postings > 0)
        {
            return FinanceResult<bool>.Fail("لا يمكن حذف حساب عليه حركات — استخدم IsActive=false بدلاً من ذلك.",
                FinanceErrorCode.InUse);
        }

        // ممنوع الحذف إن كان له حسابات فرعية
        var children = await _accounts.ListChildrenAsync(id, ct);
        if (children.Count > 0)
        {
            return FinanceResult<bool>.Fail("لا يمكن حذف حساب له حسابات فرعية.", FinanceErrorCode.HasChildren);
        }

        // soft-delete عبر IsActive=false (لا نحذف فعلياً للحفاظ على الـ audit trail)
        acc.IsActive = false;
        acc.UpdatedAt = DateTime.UtcNow;
        await _accounts.UpdateAsync(acc, ct);
        return FinanceResult<bool>.Ok(true);
    }

    private static AccountResponse MapToResponse(Account a) => new()
    {
        Id = a.Id,
        Code = a.Code,
        Name = a.Name,
        Description = a.Description,
        Type = a.Type,
        NormalBalance = a.NormalBalance,
        ParentAccountId = a.ParentAccountId,
        IsPostable = a.IsPostable,
        IsActive = a.IsActive,
    };
}
