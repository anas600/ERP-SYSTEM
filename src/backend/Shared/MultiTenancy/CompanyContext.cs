namespace ERPSystem.Shared.MultiTenancy;

public sealed class CompanyContext : ICompanyContext
{
    private static readonly AsyncLocal<CompanyHolder> _holder = new();

    public Guid? CompanyId => _holder.Value?.CompanyId;
    public Guid? UserId => _holder.Value?.UserId;
    public IReadOnlyList<Guid> CompanyIds => _holder.Value?.CompanyIds ?? Array.Empty<Guid>();
    public bool IsResolved => _holder.Value is { CompanyId: not null, UserId: not null };

    public void Set(Guid companyId, Guid userId, IReadOnlyList<Guid> companyIds)
    {
        _holder.Value = new CompanyHolder(companyId, userId, companyIds);
    }

    public void Clear()
    {
        _holder.Value = null!;
    }

    private sealed class CompanyHolder
    {
        public Guid? CompanyId { get; }
        public Guid? UserId { get; }
        public IReadOnlyList<Guid> CompanyIds { get; }

        public CompanyHolder(Guid companyId, Guid userId, IReadOnlyList<Guid> companyIds)
        {
            CompanyId = companyId;
            UserId = userId;
            CompanyIds = companyIds;
        }
    }
}
