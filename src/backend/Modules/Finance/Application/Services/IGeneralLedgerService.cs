namespace ERPSystem.Modules.Finance.Application.Services;

public interface IGeneralLedgerService
{
    /// <summary>أرصدة كل الحسابات (postable فقط) في تاريخ محدد — لتوليد Trial Balance</summary>
    Task<FinanceResult<IReadOnlyList<AccountBalanceResponse>>> GetAccountBalancesAsync(DateTime? asOf, CancellationToken ct);

    /// <summary>دفتر الأستاذ العام لحساب معين — كل الحركات بترتيب زمني + رصيد جاري</summary>
    Task<FinanceResult<IReadOnlyList<LedgerLineResponse>>> GetAccountLedgerAsync(Guid accountId, DateTime? from, DateTime? to, CancellationToken ct);

    /// <summary>Trial Balance — إجمالي مدين/دائن لكل الحسابات</summary>
    Task<FinanceResult<IReadOnlyList<AccountBalanceResponse>>> GetTrialBalanceAsync(DateTime? asOf, CancellationToken ct);
}
