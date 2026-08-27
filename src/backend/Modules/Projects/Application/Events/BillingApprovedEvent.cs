namespace ERPSystem.Modules.Projects.Application.Events;

/// <summary>
/// Sprint 65 / DEC-231: Published by <c>BillingService.ApproveAsync</c> after a Progress Billing
/// transitions from DRAFT to INVOICED. The <c>BillingApprovedHandler</c> subscribes to this event
/// and orchestrates downstream AR/Finance side effects.
///
/// <para><b>L19 / DEC-095 compliance:</b> <c>CompanyId</c> and <c>UserId</c> are captured from the
/// service that fires the event (which read them from <c>ICompanyContext</c> and the JWT
/// respectively). They are NEVER taken from a request DTO.</para>
/// </summary>
public sealed class BillingApprovedEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid BillingId { get; }
    public Guid ProjectId { get; }
    public Guid CompanyId { get; }
    public decimal NetAmount { get; }
    public Guid UserId { get; }
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public BillingApprovedEvent(
        Guid billingId,
        Guid projectId,
        Guid companyId,
        decimal netAmount,
        Guid userId)
    {
        BillingId = billingId;
        ProjectId = projectId;
        CompanyId = companyId;
        NetAmount = netAmount;
        UserId = userId;
    }
}
