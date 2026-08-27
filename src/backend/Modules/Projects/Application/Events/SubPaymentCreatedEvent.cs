namespace ERPSystem.Modules.Projects.Application.Events;

/// <summary>
/// Sprint 65 / DEC-232: Published by <c>SubPaymentService.CreateAsync</c> when a new sub-payment
/// to a subcontractor is recorded. The <c>SubPaymentCreatedHandler</c> subscribes to this event
/// and creates the corresponding AP Vendor Bill + Journal Entry.
///
/// <para><b>Two bills per payment:</b> the base payment amount and any retention released are
/// tracked separately so finance can see them as distinct cost lines.</para>
///
/// <para><b>L19 / DEC-095 compliance:</b> <c>CompanyId</c> and <c>UserId</c> come from the firing
/// service (which read them from <c>ICompanyContext</c> and the JWT).</para>
/// </summary>
public sealed class SubPaymentCreatedEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid SubPaymentId { get; }
    public Guid SubContractId { get; }
    public Guid SubcontractorId { get; }
    public Guid CompanyId { get; }
    public decimal Amount { get; }
    public decimal RetentionReleased { get; }
    public Guid UserId { get; }
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public SubPaymentCreatedEvent(
        Guid subPaymentId,
        Guid subContractId,
        Guid subcontractorId,
        Guid companyId,
        decimal amount,
        decimal retentionReleased,
        Guid userId)
    {
        SubPaymentId = subPaymentId;
        SubContractId = subContractId;
        SubcontractorId = subcontractorId;
        CompanyId = companyId;
        Amount = amount;
        RetentionReleased = retentionReleased;
        UserId = userId;
    }
}
