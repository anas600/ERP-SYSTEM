using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ERPSystem.Modules.Payments.Entities;

namespace ERPSystem.Modules.Payments.Infrastructure;

/// <summary>Repository contracts لـ Payment + Allocations.</summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Payment?> GetByPaymentNumberAsync(string paymentNumber, CancellationToken ct);
    Task<IReadOnlyList<Payment>> ListAsync(string? partyType, Guid? partyId, PaymentStatus? status, int skip, int take, CancellationToken ct);
    Task InsertAsync(Payment payment, CancellationToken ct);
    Task UpdateAsync(Payment payment, CancellationToken ct);
    Task InsertAllocationsAsync(Guid paymentId, IEnumerable<PaymentAllocation> allocations, CancellationToken ct);
    Task<IReadOnlyList<PaymentAllocation>> GetAllocationsAsync(Guid paymentId, CancellationToken ct);
    Task<decimal> SumAllocationsForRefAsync(string refType, Guid refId, CancellationToken ct);
}
