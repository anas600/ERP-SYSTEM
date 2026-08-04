using Dapper;
using ERPSystem.Modules.HR.Application;
using ERPSystem.Modules.HR.Entities;
using ERPSystem.Modules.HR.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.HR.Application.Services;

public sealed class HRResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public HRErrorCode? ErrorCode { get; init; }
    public static HRResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static HRResult<T> Fail(string e, HRErrorCode c) => new() { Succeeded = false, Error = e, ErrorCode = c };
}

public enum HRErrorCode
{
    NotFound, AlreadyExists, ValidationError, InvalidStatusTransition, BusinessRuleViolation, Internal
}

public interface IDepartmentService
{
    Task<HRResult<DepartmentResponse>> CreateAsync(CreateDepartmentRequest req, CancellationToken ct);
    Task<HRResult<DepartmentResponse>> UpdateAsync(Guid id, UpdateDepartmentRequest req, CancellationToken ct);
    Task<HRResult<DepartmentResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<HRResult<IReadOnlyList<DepartmentResponse>>> ListAsync(bool includeInactive, CancellationToken ct);
    Task<HRResult<bool>> DeactivateAsync(Guid id, CancellationToken ct);
}

public sealed class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repo;
    private readonly ICompanyContext _companyContext;
    private readonly IDbConnectionFactory _db;
    public DepartmentService(IDepartmentRepository repo, ICompanyContext companyContext, IDbConnectionFactory db) { _repo = repo; _companyContext = companyContext; _db = db; }

    public async Task<HRResult<DepartmentResponse>> CreateAsync(CreateDepartmentRequest req, CancellationToken ct)
    {
        if (await _repo.GetByCodeAsync(req.Code, ct) != null)
            return HRResult<DepartmentResponse>.Fail("كود القسم مستخدم.", HRErrorCode.AlreadyExists);
        if (req.ParentId.HasValue)
        {
            var parent = await _repo.GetByIdAsync(req.ParentId.Value, ct);
            if (parent == null)
                return HRResult<DepartmentResponse>.Fail("القسم الأب غير موجود.", HRErrorCode.NotFound);
        }
        // Sprint 27 (DEC-091): Constitution Article 3 — read company_id from context.
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");
        var now = DateTime.UtcNow;
        var d = new Department
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = req.Code.Trim(), Name = req.Name.Trim(),
            ParentId = req.ParentId, ManagerId = req.ManagerId,
            IsActive = true, CreatedAt = now, UpdatedAt = now
        };
        await _repo.InsertAsync(d, ct);
        return HRResult<DepartmentResponse>.Ok(await MapToResponseAsync(d, ct));
    }

    public async Task<HRResult<DepartmentResponse>> UpdateAsync(Guid id, UpdateDepartmentRequest req, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(id, ct);
        if (d == null)
            return HRResult<DepartmentResponse>.Fail("غير موجود.", HRErrorCode.NotFound);
        d.Name = req.Name.Trim();
        d.ParentId = req.ParentId;
        d.ManagerId = req.ManagerId;
        d.IsActive = req.IsActive;
        d.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(d, ct);
        return HRResult<DepartmentResponse>.Ok(await MapToResponseAsync(d, ct));
    }

    public async Task<HRResult<DepartmentResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(id, ct);
        if (d == null)
            return HRResult<DepartmentResponse>.Fail("غير موجود.", HRErrorCode.NotFound);
        return HRResult<DepartmentResponse>.Ok(await MapToResponseAsync(d, ct));
    }

    public async Task<HRResult<IReadOnlyList<DepartmentResponse>>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        var list = await _repo.ListAsync(includeInactive, ct);

        // Sprint 31 (DEC-107): single-batch manager lookup + employee counts (L40 pattern).
        var managerIds = list.Where(d => d.ManagerId.HasValue).Select(d => d.ManagerId!.Value).Distinct().ToList();
        var managerMap = new Dictionary<Guid, (string Name, string Code)>();
        if (managerIds.Count > 0)
        {
            using var conn = await _db.CreateEphemeralOltpConnectionAsync(ct);
            var rows = await conn.QueryAsync<(Guid Id, string Name, string EmployeeNumber)>(
                "SELECT id, full_name, employee_number FROM employees WHERE id = ANY(@Ids)",
                new { Ids = managerIds.ToArray() });
            managerMap = rows.ToDictionary(r => r.Id, r => (r.Name, r.EmployeeNumber));
        }

        // Employee counts per department
        using var conn2 = await _db.CreateEphemeralOltpConnectionAsync(ct);
        var counts = (await conn2.QueryAsync<(Guid DepartmentId, int Count)>(
            "SELECT department_id, COUNT(*)::int FROM employees WHERE department_id = ANY(@Ids) AND is_active = true GROUP BY department_id",
            new { Ids = list.Select(d => d.Id).ToArray() })).ToDictionary(t => t.DepartmentId, t => t.Count);

        var responses = list.Select(d =>
        {
            var resp = MapToResponse(d);
            if (d.ManagerId.HasValue && managerMap.TryGetValue(d.ManagerId.Value, out var mgr))
            {
                resp.ManagerName = mgr.Name;
                resp.ManagerCode = mgr.Code;
            }
            resp.EmployeeCount = counts.TryGetValue(d.Id, out var c) ? c : 0;
            return resp;
        }).ToList();

        return HRResult<IReadOnlyList<DepartmentResponse>>.Ok(responses);
    }

    public async Task<HRResult<bool>> DeactivateAsync(Guid id, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(id, ct);
        if (d == null)
            return HRResult<bool>.Fail("غير موجود.", HRErrorCode.NotFound);
        d.IsActive = false;
        d.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(d, ct);
        return HRResult<bool>.Ok(true);
    }

    private static DepartmentResponse MapToResponse(Department d) => new()
    {
        Id = d.Id, Code = d.Code, Name = d.Name,
        ParentId = d.ParentId, ManagerId = d.ManagerId, IsActive = d.IsActive
    };

    // Sprint 31 (DEC-107): single-item enrichment with manager name + employee count.
    private async Task<DepartmentResponse> MapToResponseAsync(Department d, CancellationToken ct)
    {
        var resp = MapToResponse(d);
        if (d.ManagerId.HasValue)
        {
            using var conn = await _db.CreateEphemeralOltpConnectionAsync(ct);
            var row = await conn.QueryFirstOrDefaultAsync<(string Name, string EmployeeNumber)?>(
                "SELECT full_name, employee_number FROM employees WHERE id = @Id",
                new { Id = d.ManagerId.Value });
            if (row.HasValue)
            {
                resp.ManagerName = row.Value.Name;
                resp.ManagerCode = row.Value.EmployeeNumber;
            }
        }
        using var conn2 = await _db.CreateEphemeralOltpConnectionAsync(ct);
        resp.EmployeeCount = await conn2.ExecuteScalarAsync<int?>(
            "SELECT COUNT(*)::int FROM employees WHERE department_id = @Id AND is_active = true",
            new { Id = d.Id }) ?? 0;
        return resp;
    }
}

public interface IEmployeeService
{
    Task<HRResult<EmployeeResponse>> CreateAsync(Guid userId, CreateEmployeeRequest req, CancellationToken ct);
    Task<HRResult<EmployeeResponse>> UpdateAsync(Guid userId, Guid id, UpdateEmployeeRequest req, CancellationToken ct);
    Task<HRResult<EmployeeResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<HRResult<IReadOnlyList<EmployeeResponse>>> ListAsync(Guid? departmentId, bool includeInactive, int skip, int take, CancellationToken ct);
    Task<HRResult<bool>> DeactivateAsync(Guid userId, Guid id, CancellationToken ct);
}

public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repo;
    private readonly IHRDocumentSequenceRepository _seq;
    private readonly ICompanyContext _companyContext;
    public EmployeeService(IEmployeeRepository repo, IHRDocumentSequenceRepository seq, ICompanyContext companyContext) { _repo = repo; _seq = seq; _companyContext = companyContext; }

    public async Task<HRResult<EmployeeResponse>> CreateAsync(Guid userId, CreateEmployeeRequest req, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(req.Email) && await _repo.GetByEmailAsync(req.Email, ct) != null)
            return HRResult<EmployeeResponse>.Fail("البريد الإلكتروني مستخدم.", HRErrorCode.AlreadyExists);

        // Sprint 27 (DEC-091): Constitution Article 3 — read company_id from context.
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");
        var empNumber = await _seq.GetNextEmployeeNumberAsync(ct);
        var now = DateTime.UtcNow;
        var e = new Employee
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            EmployeeNumber = empNumber, FullName = req.FullName.Trim(),
            Email = req.Email, Phone = req.Phone, NationalId = req.NationalId,
            DepartmentId = req.DepartmentId, JobTitle = req.JobTitle,
            HireDate = req.HireDate, BaseSalary = req.BaseSalary,
            IsActive = true, CreatedAt = now, CreatedBy = userId, UpdatedAt = now, UpdatedBy = userId
        };
        await _repo.InsertAsync(e, ct);
        return HRResult<EmployeeResponse>.Ok(MapToResponse(e));
    }

    public async Task<HRResult<EmployeeResponse>> UpdateAsync(Guid userId, Guid id, UpdateEmployeeRequest req, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(id, ct);
        if (e == null)
            return HRResult<EmployeeResponse>.Fail("غير موجود.", HRErrorCode.NotFound);
        if (!string.IsNullOrEmpty(req.Email) && !string.Equals(e.Email, req.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _repo.GetByEmailAsync(req.Email, ct) != null)
                return HRResult<EmployeeResponse>.Fail("البريد الإلكتروني مستخدم.", HRErrorCode.AlreadyExists);
        }
        e.FullName = req.FullName.Trim();
        e.Email = req.Email; e.Phone = req.Phone; e.NationalId = req.NationalId;
        e.DepartmentId = req.DepartmentId; e.JobTitle = req.JobTitle;
        e.TerminationDate = req.TerminationDate;
        e.BaseSalary = req.BaseSalary;
        e.IsActive = req.IsActive;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedBy = userId;
        await _repo.UpdateAsync(e, ct);
        return HRResult<EmployeeResponse>.Ok(MapToResponse(e));
    }

    public async Task<HRResult<EmployeeResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(id, ct);
        if (e == null)
            return HRResult<EmployeeResponse>.Fail("غير موجود.", HRErrorCode.NotFound);
        return HRResult<EmployeeResponse>.Ok(MapToResponse(e));
    }

    public async Task<HRResult<IReadOnlyList<EmployeeResponse>>> ListAsync(Guid? departmentId, bool includeInactive, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        var list = await _repo.ListAsync(departmentId, includeInactive, skip, take, ct);
        return HRResult<IReadOnlyList<EmployeeResponse>>.Ok(list.Select(MapToResponse).ToList());
    }

    public async Task<HRResult<bool>> DeactivateAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(id, ct);
        if (e == null)
            return HRResult<bool>.Fail("غير موجود.", HRErrorCode.NotFound);
        e.IsActive = false;
        e.TerminationDate = e.TerminationDate ?? DateTime.UtcNow;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedBy = userId;
        await _repo.UpdateAsync(e, ct);
        return HRResult<bool>.Ok(true);
    }

    private static EmployeeResponse MapToResponse(Employee e) => new()
    {
        Id = e.Id, EmployeeNumber = e.EmployeeNumber, FullName = e.FullName,
        Email = e.Email, Phone = e.Phone, NationalId = e.NationalId,
        DepartmentId = e.DepartmentId, JobTitle = e.JobTitle,
        HireDate = e.HireDate, TerminationDate = e.TerminationDate,
        BaseSalary = e.BaseSalary, IsActive = e.IsActive
    };
}

public interface IAttendanceService
{
    Task<HRResult<AttendanceResponse>> RecordAsync(CheckInOutRequest req, string? ipAddress, CancellationToken ct);
    Task<HRResult<AttendanceResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<HRResult<IReadOnlyList<AttendanceResponse>>> ListAsync(Guid? employeeId, DateTime? from, DateTime? to, int skip, int take, CancellationToken ct);
}

public sealed class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _repo;
    private readonly IEmployeeRepository _employees;
    private readonly ICompanyContext _companyContext;
    public AttendanceService(IAttendanceRepository repo, IEmployeeRepository employees, ICompanyContext companyContext) { _repo = repo; _employees = employees; _companyContext = companyContext; }

    public async Task<HRResult<AttendanceResponse>> RecordAsync(CheckInOutRequest req, string? ipAddress, CancellationToken ct)
    {
        var emp = await _employees.GetByIdAsync(req.EmployeeId, ct);
        if (emp == null)
            return HRResult<AttendanceResponse>.Fail("الموظف غير موجود.", HRErrorCode.NotFound);

        // Business Rule: لا تكرار من نفس النوع متتالياً
        var last = await _repo.GetLastForEmployeeAsync(req.EmployeeId, ct);
        if (last != null && last.Type == req.Type && (DateTime.UtcNow - last.Timestamp).TotalHours < 12)
            return HRResult<AttendanceResponse>.Fail(
                $"لا يمكن تسجيل {req.Type} متتالي بدون النوع المعاكس.", HRErrorCode.BusinessRuleViolation);

        // Sprint 27 (DEC-091): Constitution Article 3 — read company_id from context
        // (use the employee's company for cross-tenant safety, falling back to the
        // active context — both are guaranteed to match in single-deployment mode).
        var companyId = emp.CompanyId != Guid.Empty ? emp.CompanyId : _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");
        var att = new Attendance
        {
            Id = Guid.NewGuid(), CompanyId = companyId, EmployeeId = req.EmployeeId,
            Type = req.Type, Timestamp = DateTime.UtcNow, Notes = req.Notes, IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
        await _repo.InsertAsync(att, ct);
        return HRResult<AttendanceResponse>.Ok(MapToResponse(att));
    }

    public async Task<HRResult<AttendanceResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var a = await _repo.GetByIdAsync(id, ct);
        if (a == null)
            return HRResult<AttendanceResponse>.Fail("غير موجود.", HRErrorCode.NotFound);
        return HRResult<AttendanceResponse>.Ok(MapToResponse(a));
    }

    public async Task<HRResult<IReadOnlyList<AttendanceResponse>>> ListAsync(Guid? employeeId, DateTime? from, DateTime? to, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        var list = await _repo.ListAsync(employeeId, from, to, skip, take, ct);
        return HRResult<IReadOnlyList<AttendanceResponse>>.Ok(list.Select(MapToResponse).ToList());
    }

    private static AttendanceResponse MapToResponse(Attendance a) => new()
    {
        Id = a.Id, EmployeeId = a.EmployeeId, Type = a.Type, Timestamp = a.Timestamp,
        Notes = a.Notes, IpAddress = a.IpAddress
    };
}

public interface ILeaveRequestService
{
    Task<HRResult<LeaveRequestResponse>> CreateAsync(Guid userId, CreateLeaveRequestDto req, CancellationToken ct);
    Task<HRResult<LeaveRequestResponse>> ApproveAsync(Guid userId, Guid id, CancellationToken ct);
    Task<HRResult<LeaveRequestResponse>> RejectAsync(Guid userId, Guid id, CancellationToken ct);
    Task<HRResult<LeaveRequestResponse>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<HRResult<IReadOnlyList<LeaveRequestResponse>>> ListAsync(Guid? employeeId, LeaveStatus? status, int skip, int take, CancellationToken ct);
}

public sealed class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _repo;
    private readonly IEmployeeRepository _employees;
    private readonly ICompanyContext _companyContext;
    public LeaveRequestService(ILeaveRequestRepository repo, IEmployeeRepository employees, ICompanyContext companyContext) { _repo = repo; _employees = employees; _companyContext = companyContext; }

    public async Task<HRResult<LeaveRequestResponse>> CreateAsync(Guid userId, CreateLeaveRequestDto req, CancellationToken ct)
    {
        var emp = await _employees.GetByIdAsync(req.EmployeeId, ct);
        if (emp == null)
            return HRResult<LeaveRequestResponse>.Fail("الموظف غير موجود.", HRErrorCode.NotFound);

        // Business Rule: لا تتعارض مع إجازة Approved أخرى
        if (await _repo.HasOverlappingApprovedAsync(req.EmployeeId, req.StartDate, req.EndDate, ct))
            return HRResult<LeaveRequestResponse>.Fail("يوجد إجازة معتمدة أخرى للموظف في نفس الفترة.", HRErrorCode.BusinessRuleViolation);

        var totalDays = (int)(req.EndDate.Date - req.StartDate.Date).TotalDays + 1;
        // Sprint 27 (DEC-091): Constitution Article 3 — same pattern as Attendance.
        var companyId = emp.CompanyId != Guid.Empty ? emp.CompanyId : _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");
        var now = DateTime.UtcNow;
        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(), CompanyId = companyId, EmployeeId = req.EmployeeId,
            LeaveType = req.LeaveType, StartDate = req.StartDate, EndDate = req.EndDate,
            TotalDays = totalDays, Status = LeaveStatus.Pending,
            Reason = req.Reason, Notes = req.Notes,
            CreatedAt = now, CreatedBy = userId, UpdatedAt = now
        };
        await _repo.InsertAsync(leave, ct);
        return HRResult<LeaveRequestResponse>.Ok(MapToResponse(leave));
    }

    public async Task<HRResult<LeaveRequestResponse>> ApproveAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var l = await _repo.GetByIdAsync(id, ct);
        if (l == null)
            return HRResult<LeaveRequestResponse>.Fail("غير موجود.", HRErrorCode.NotFound);
        if (l.Status != LeaveStatus.Pending)
            return HRResult<LeaveRequestResponse>.Fail($"لا يمكن الموافقة على إجازة في حالة {l.Status}.", HRErrorCode.InvalidStatusTransition);
        l.Status = LeaveStatus.Approved;
        l.ApproverId = userId;
        l.ApprovedAt = DateTime.UtcNow;
        l.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(l, ct);
        return HRResult<LeaveRequestResponse>.Ok(MapToResponse(l));
    }

    public async Task<HRResult<LeaveRequestResponse>> RejectAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var l = await _repo.GetByIdAsync(id, ct);
        if (l == null)
            return HRResult<LeaveRequestResponse>.Fail("غير موجود.", HRErrorCode.NotFound);
        if (l.Status != LeaveStatus.Pending)
            return HRResult<LeaveRequestResponse>.Fail($"لا يمكن رفض إجازة في حالة {l.Status}.", HRErrorCode.InvalidStatusTransition);
        l.Status = LeaveStatus.Rejected;
        l.ApproverId = userId;
        l.ApprovedAt = DateTime.UtcNow;
        l.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(l, ct);
        return HRResult<LeaveRequestResponse>.Ok(MapToResponse(l));
    }

    public async Task<HRResult<LeaveRequestResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var l = await _repo.GetByIdAsync(id, ct);
        if (l == null)
            return HRResult<LeaveRequestResponse>.Fail("غير موجود.", HRErrorCode.NotFound);
        return HRResult<LeaveRequestResponse>.Ok(MapToResponse(l));
    }

    public async Task<HRResult<IReadOnlyList<LeaveRequestResponse>>> ListAsync(Guid? employeeId, LeaveStatus? status, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        var list = await _repo.ListAsync(employeeId, status, skip, take, ct);
        return HRResult<IReadOnlyList<LeaveRequestResponse>>.Ok(list.Select(MapToResponse).ToList());
    }

    private static LeaveRequestResponse MapToResponse(LeaveRequest l) => new()
    {
        Id = l.Id, EmployeeId = l.EmployeeId, LeaveType = l.LeaveType,
        StartDate = l.StartDate, EndDate = l.EndDate, TotalDays = l.TotalDays,
        Status = l.Status, Reason = l.Reason, ApproverId = l.ApproverId,
        ApprovedAt = l.ApprovedAt, Notes = l.Notes
    };
}
