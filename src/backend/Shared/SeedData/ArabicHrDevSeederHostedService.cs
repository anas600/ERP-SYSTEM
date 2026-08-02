// Sprint 27: Arabic HR Dev-Environment Seeder (POC #2 for the seeder pattern).
// Why this exists: same rationale as ArabicDevSeederHostedService (Sprint 26), but
// for the HR module. Reads UTF-8 JSON + UPSERTs departments + employees via Dapper.
// Idempotent. Dev environment only.
//
// This is the SECOND seeder in the same pattern (after Sprint 26's customer/vendor/item
// seeder), which establishes the "seed framework":
//   1. JSON file (UTF-8) as the data source
//   2. C# hosted service (IHostedService) as the runner
//   3. UPSERT (INSERT-or-UPDATE by natural key) for idempotency
//   4. Direct Dapper SQL (no service layer) for simplicity
//   5. Double-gated (IsDevelopment() + configFlag) for safety
//   6. appsettings.Development.json.example as the template
//
// Scope (Sprint 27): HR master data only — 5 departments + 10 employees.
// Department.manager_id references an employee (FK to employees), so we need
// 3 passes:
//   1. UPSERT departments (no manager_id yet — the referenced employee may not exist)
//   2. UPSERT employees (department_id resolved from department code)
//   3. UPDATE departments.manager_id from the manager's employee_number
// This is necessary because of the cyclic FK between departments and employees
// (department.manager_id → employee.id, employee.department_id → department.id).
//
// The cyclic FK is resolved by ordering: departments first (without managers),
// then employees (with their department), then departments get their manager.

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Shared.SeedData;

/// <summary>
/// خدمة تعمل مرة واحدة عند بدء التطبيق — تبذر HR master data (departments + employees)
/// بأسماء عربية صحيحة، بشكل idempotent. مفعّلة فقط في بيئة التطوير وعند ضبط
/// <c>Bootstrap:SeedHrScenario=true</c>.
/// <para>
/// <b>DEC-091</b>: تأتي بعد إصلاح Constitution Article 3 في الـ HR module (Sprint 27).
/// قبل Sprint 27، كانت كل الـ 4 entities + services + repos في HR مفيهاش company_id،
/// فالـ seeder كان هيكسر الـ NOT NULL constraint. الحين نقدر نـ INSERT بشكل آمن.
/// </para>
/// </summary>
public sealed class ArabicHrDevSeederHostedService : IHostedService
{
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ArabicHrDevSeederHostedService> _logger;

    public ArabicHrDevSeederHostedService(
        IDbConnectionFactory db,
        IConfiguration config,
        IHostEnvironment env,
        ILogger<ArabicHrDevSeederHostedService> logger)
    {
        _db = db;
        _config = config;
        _env = env;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
        {
            _logger.LogInformation("[Sprint27] ArabicHrDevSeeder: skipped (env={Env}, Development only)", _env.EnvironmentName);
            return;
        }

        var enabled = _config.GetValue<bool>("Bootstrap:SeedHrScenario", false);
        if (!enabled)
        {
            _logger.LogInformation("[Sprint27] ArabicHrDevSeeder: skipped (Bootstrap:SeedHrScenario=false)");
            return;
        }

        _logger.LogInformation("[Sprint27] ArabicHrDevSeeder: starting (env=Development, flag=true)");

        var dataFile = ResolveDataFile();
        if (dataFile == null || !File.Exists(dataFile))
        {
            _logger.LogError("[Sprint27] ArabicHrDevSeeder: data file not found (tried {File})", dataFile);
            return;
        }
        _logger.LogInformation("[Sprint27] ArabicHrDevSeeder: loading data from {File}", dataFile);

        ArabicHrDevData data;
        try
        {
            var json = await File.ReadAllTextAsync(dataFile, cancellationToken);
            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
            data = JsonSerializer.Deserialize<ArabicHrDevData>(json, opts)
                   ?? throw new InvalidOperationException("Empty or null JSON");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Sprint27] ArabicHrDevSeeder: failed to parse data file");
            return;
        }

        var holdingId = await ResolveHoldingIdAsync(cancellationToken);
        if (holdingId == Guid.Empty)
        {
            _logger.LogError("[Sprint27] ArabicHrDevSeeder: no Holding found — run DefaultHoldingBootstrap first");
            return;
        }

        var adminUserId = await ResolveAdminUserIdAsync(cancellationToken);
        if (adminUserId == Guid.Empty)
        {
            _logger.LogWarning("[Sprint27] ArabicHrDevSeeder: no user found — created_by will be empty GUID");
        }

        using var conn = await _db.CreateEphemeralOltpConnectionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        int deptUpdated = 0, deptInserted = 0, empUpdated = 0, empInserted = 0;

        // ===== Pass 1: UPSERT departments (no manager_id yet) =====
        // First we need the departments to exist so employees can reference them.
        var departments = data.Departments ?? new List<ArabicHrDevDepartment>();
        foreach (var d in departments)
        {
            var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM departments WHERE code = @Code LIMIT 1",
                new { Code = d.Code }, cancellationToken: cancellationToken));

            if (existing.HasValue)
            {
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE departments SET
                        name = @Name, parent_id = @ParentId,
                        is_active = true, updated_at = @Now
                    WHERE id = @Id",
                    new
                    {
                        Id = existing.Value,
                        Name = d.Name,
                        ParentId = ResolveParentId(departments, d.ParentCode, conn, cancellationToken),
                        Now = now,
                    }, cancellationToken: cancellationToken));
                deptUpdated++;
            }
            else
            {
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO departments
                        (id, company_id, code, name, parent_id, manager_id, is_active, created_at, updated_at)
                    VALUES
                        (@Id, @HoldingId, @Code, @Name, @ParentId, NULL, true, @Now, @Now)",
                    new
                    {
                        Id = Guid.NewGuid(),
                        HoldingId = holdingId,
                        d.Code, d.Name,
                        ParentId = ResolveParentId(departments, d.ParentCode, conn, cancellationToken),
                        Now = now,
                    }, cancellationToken: cancellationToken));
                deptInserted++;
            }
        }

        // ===== Pass 2: UPSERT employees (department_id from code) =====
        var employees = data.Employees ?? new List<ArabicHrDevEmployee>();
        foreach (var e in employees)
        {
            var existing = await conn.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM employees WHERE employee_number = @Number LIMIT 1",
                new { Number = e.EmployeeNumber }, cancellationToken: cancellationToken));

            var departmentId = ResolveDepartmentId(departments, e.DepartmentCode, conn, cancellationToken);

            if (existing.HasValue)
            {
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE employees SET
                        full_name = @FullName, email = @Email, phone = @Phone, national_id = @NationalId,
                        department_id = @DepartmentId, job_title = @JobTitle,
                        base_salary = @BaseSalary, is_active = true,
                        updated_at = @Now, updated_by = @UpdatedBy
                    WHERE id = @Id",
                    new
                    {
                        Id = existing.Value,
                        e.FullName, e.Email, e.Phone, e.NationalId,
                        DepartmentId = departmentId, e.JobTitle, e.BaseSalary,
                        Now = now, UpdatedBy = adminUserId,
                    }, cancellationToken: cancellationToken));
                empUpdated++;
            }
            else
            {
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO employees
                        (id, company_id, employee_number, full_name, email, phone, national_id,
                         department_id, job_title, hire_date, termination_date, base_salary,
                         is_active, created_at, created_by, updated_at, updated_by)
                    VALUES
                        (@Id, @HoldingId, @Number, @FullName, @Email, @Phone, @NationalId,
                         @DepartmentId, @JobTitle, @HireDate, NULL, @BaseSalary,
                         true, @Now, @CreatedBy, @Now, @CreatedBy)",
                    new
                    {
                        Id = Guid.NewGuid(),
                        HoldingId = holdingId,
                        Number = e.EmployeeNumber,
                        e.FullName, e.Email, e.Phone, e.NationalId,
                        DepartmentId = departmentId, e.JobTitle,
                        HireDate = DateTime.Parse(e.HireDate, System.Globalization.CultureInfo.InvariantCulture),
                        e.BaseSalary,
                        Now = now, CreatedBy = adminUserId,
                    }, cancellationToken: cancellationToken));
                empInserted++;
            }
        }

        // ===== Pass 3: UPDATE departments.manager_id from the manager's employeeNumber =====
        // Now that both departments and employees exist, we can wire up the manager FK.
        int managerAssigned = 0;
        foreach (var d in departments)
        {
            if (string.IsNullOrWhiteSpace(d.ManagerEmployeeNumber)) continue;
            var managerId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM employees WHERE employee_number = @Number LIMIT 1",
                new { Number = d.ManagerEmployeeNumber }, cancellationToken: cancellationToken));
            if (!managerId.HasValue)
            {
                _logger.LogWarning(
                    "[Sprint27] Manager for department {Code} not found (employeeNumber={Number}) — manager_id left NULL",
                    d.Code, d.ManagerEmployeeNumber);
                continue;
            }
            await conn.ExecuteAsync(new CommandDefinition(@"
                UPDATE departments SET manager_id = @ManagerId, updated_at = @Now WHERE code = @Code",
                new { ManagerId = managerId.Value, Now = now, Code = d.Code },
                cancellationToken: cancellationToken));
            managerAssigned++;
        }

        _logger.LogInformation(
            "[Sprint27] ArabicHrDevSeeder: completed — departments updated={DU} inserted={DI}, employees updated={EU} inserted={EI}, manager links assigned={MA}",
            deptUpdated, deptInserted, empUpdated, empInserted, managerAssigned);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private Guid? ResolveParentId(List<ArabicHrDevDepartment> all, string? parentCode, IDbConnection conn, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parentCode)) return null;
        return conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM departments WHERE code = @Code LIMIT 1",
            new { Code = parentCode }, cancellationToken: ct)).GetAwaiter().GetResult();
    }

    private Guid? ResolveDepartmentId(List<ArabicHrDevDepartment> all, string departmentCode, IDbConnection conn, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(departmentCode)) return null;
        return conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM departments WHERE code = @Code LIMIT 1",
            new { Code = departmentCode }, cancellationToken: ct)).GetAwaiter().GetResult();
    }

    private string? ResolveDataFile()
    {
        var configured = _config.GetValue<string>("ArabicSeeder:HrDataFile");
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            candidates.Add(configured);
            if (!Path.IsPathRooted(configured))
            {
                candidates.Add(Path.Combine(AppContext.BaseDirectory, configured));
                candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), configured));
            }
        }
        else
        {
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "Shared", "SeedData", "ArabicHrDevData.json"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "ArabicHrDevData.json"));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "Shared", "SeedData", "ArabicHrDevData.json"));
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Shared", "SeedData", "ArabicHrDevData.json"));
        }
        foreach (var path in candidates)
        {
            if (File.Exists(path)) return Path.GetFullPath(path);
        }
        return null;
    }

    private async Task<Guid> ResolveHoldingIdAsync(CancellationToken ct)
    {
        using var conn = await _db.CreateEphemeralOltpConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            @"SELECT id FROM companies
              WHERE is_group = true
                AND parent_company_id IS NULL
                AND code = '000'
              LIMIT 1",
            cancellationToken: ct));
        return id ?? Guid.Empty;
    }

    private async Task<Guid> ResolveAdminUserIdAsync(CancellationToken ct)
    {
        using var conn = await _db.CreateEphemeralOltpConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM users ORDER BY created_at LIMIT 1",
            cancellationToken: ct));
        return id ?? Guid.Empty;
    }
}

// ============ DTOs ============

public sealed class ArabicHrDevData
{
    [JsonPropertyName("departments")]
    public List<ArabicHrDevDepartment>? Departments { get; set; }

    [JsonPropertyName("employees")]
    public List<ArabicHrDevEmployee>? Employees { get; set; }
}

public sealed class ArabicHrDevDepartment
{
    [JsonPropertyName("code")]                    public string Code { get; set; } = "";
    [JsonPropertyName("name")]                    public string Name { get; set; } = "";
    [JsonPropertyName("nameEn")]                  public string? NameEn { get; set; }
    [JsonPropertyName("parentCode")]              public string? ParentCode { get; set; }
    [JsonPropertyName("managerEmployeeNumber")]   public string? ManagerEmployeeNumber { get; set; }
}

public sealed class ArabicHrDevEmployee
{
    [JsonPropertyName("employeeNumber")]  public string EmployeeNumber { get; set; } = "";
    [JsonPropertyName("fullName")]        public string FullName { get; set; } = "";
    [JsonPropertyName("email")]           public string? Email { get; set; }
    [JsonPropertyName("phone")]           public string? Phone { get; set; }
    [JsonPropertyName("nationalId")]      public string? NationalId { get; set; }
    [JsonPropertyName("departmentCode")]  public string DepartmentCode { get; set; } = "";
    [JsonPropertyName("jobTitle")]        public string? JobTitle { get; set; }
    [JsonPropertyName("hireDate")]        public string HireDate { get; set; } = "";
    [JsonPropertyName("baseSalary")]      public decimal BaseSalary { get; set; }
}
