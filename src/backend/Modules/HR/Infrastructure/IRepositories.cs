using System;
using System.Collections.Generic;
using ERPSystem.Modules.HR.Entities;

namespace ERPSystem.Modules.HR.Infrastructure;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Department?> GetByCodeAsync(Guid tenantId, string code, CancellationToken ct);
    Task<IReadOnlyList<Department>> ListAsync(Guid tenantId, bool includeInactive, CancellationToken ct);
    Task InsertAsync(Department dept, CancellationToken ct);
    Task UpdateAsync(Department dept, CancellationToken ct);
}

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Employee?> GetByNumberAsync(Guid tenantId, string number, CancellationToken ct);
    Task<Employee?> GetByEmailAsync(Guid tenantId, string email, CancellationToken ct);
    Task<IReadOnlyList<Employee>> ListAsync(Guid tenantId, Guid? departmentId, bool includeInactive, int skip, int take, CancellationToken ct);
    Task InsertAsync(Employee emp, CancellationToken ct);
    Task UpdateAsync(Employee emp, CancellationToken ct);
}

public interface IAttendanceRepository
{
    Task<Attendance?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Attendance?> GetLastForEmployeeAsync(Guid tenantId, Guid employeeId, CancellationToken ct);
    Task<IReadOnlyList<Attendance>> ListAsync(Guid tenantId, Guid? employeeId, DateTime? from, DateTime? to, int skip, int take, CancellationToken ct);
    Task InsertAsync(Attendance att, CancellationToken ct);
}

public interface ILeaveRequestRepository
{
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<LeaveRequest>> ListAsync(Guid tenantId, Guid? employeeId, LeaveStatus? status, int skip, int take, CancellationToken ct);
    Task<bool> HasOverlappingApprovedAsync(Guid employeeId, DateTime start, DateTime end, CancellationToken ct);
    Task InsertAsync(LeaveRequest leave, CancellationToken ct);
    Task UpdateAsync(LeaveRequest leave, CancellationToken ct);
}

public interface IHRDocumentSequenceRepository
{
    Task<string> GetNextEmployeeNumberAsync(Guid tenantId, CancellationToken ct);
}
