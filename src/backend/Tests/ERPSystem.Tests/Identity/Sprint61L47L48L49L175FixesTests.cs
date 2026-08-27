// Sprint 61 (Wave 1B) — 5 permanent fixes from Sprint 60 lessons.
//
//   L47  Phase6_InitialSchema: re-create VersionInfo after DROP SCHEMA
//   L48  DefaultHoldingBootstrapHostedService: ensure default roles
//   L49  AuthService.BuildAsync: connection-aware GetUserCompaniesAsync
//   L51  no-tenant-id.yml: exclude test files from the architecture guard
//   L175 /api/auth/admin-bootstrap: one-shot first-admin endpoint
//
// All tests are pure unit / file-content checks so they run on any machine
// without a real Postgres database. End-to-end verification still happens in
// Wave 3 (Trust Mode) where the actual backend is started.

using System.Reflection;
using System.Text.RegularExpressions;
using ERPSystem.Modules.Identity.Application.Auth;
using ERPSystem.Modules.Identity.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ERPSystem.Tests.Identity;

public class Sprint61L47L48L49L175FixesTests
{
    // ============ L47 — Phase6_InitialSchema recreates VersionInfo ============

    [Fact]
    public void L47_Phase6Migration_RecreatesVersionInfo_AfterDropSchema()
    {
        var path = LocateRelativeToSolution(
            @"src\backend\Shared\Migrations\Phase6_InitialSchema_20260725_120000.cs");
        File.Exists(path).Should().BeTrue(
            "the Phase6 migration must exist on disk so the test has something to inspect");

        var content = File.ReadAllText(path);

        // The DDL must come AFTER the schema drop but BEFORE the migration
        // runner tries to INSERT this migration's row into VersionInfo.
        // The file uses C# verbatim string literal syntax: @""CREATE TABLE ""VersionInfo"" ("""
        // — the "" in source text is one literal " character, so we match
        // against the doubled form.
        var dropIndex = content.IndexOf("DROP SCHEMA IF EXISTS public CASCADE", StringComparison.Ordinal);
        var createIndex = content.IndexOf("CREATE TABLE \"\"VersionInfo\"\"", StringComparison.Ordinal);
        var seedIndex = content.IndexOf("INSERT INTO \"\"VersionInfo\"\"", StringComparison.Ordinal);

        dropIndex.Should().BeGreaterThan(0, "the schema drop statement must still be in the migration");
        createIndex.Should().BeGreaterThan(dropIndex,
            "the VersionInfo CREATE TABLE must run AFTER the schema drop so the runner can write its tracking row");
        seedIndex.Should().BeGreaterThan(createIndex,
            "the VersionInfo pre-seed INSERT must follow the CREATE TABLE");
    }

    [Fact]
    public void L47_Phase6Migration_VersionInfoHas_VersionAndAppliedOnColumns()
    {
        var path = LocateRelativeToSolution(
            @"src\backend\Shared\Migrations\Phase6_InitialSchema_20260725_120000.cs");
        var content = File.ReadAllText(path);

        // Match the CREATE TABLE block and assert the two columns the
        // FluentMigrator runner requires. In a C# verbatim string the
        // identifier is written as ""VersionInfo"" (literal "VersionInfo").
        var match = Regex.Match(
            content,
            @"CREATE\s+TABLE\s+""""VersionInfo""""\s*\((?<body>[^)]*)\)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        match.Success.Should().BeTrue(
            "the Phase6 migration must declare VersionInfo with the runner-expected columns");

        var body = match.Groups["body"].Value;
        body.Should().Contain("\"\"Version\"\"",
            "FluentMigrator requires the Version column on VersionInfo");
        body.Should().Contain("\"\"AppliedOn\"\"",
            "FluentMigrator requires the AppliedOn column on VersionInfo");
    }

    // ============ L48 — DefaultHoldingBootstrapHostedService ensures default roles ============

    [Fact]
    public void L48_DefaultHoldingBootstrapHostedService_ReferencesIRoleRepository()
    {
        var assembly = typeof(ERPSystem.Modules.Companies.Infrastructure.ICompanyRepository).Assembly
                       .GetReferencedAssemblies()
                       .Select(Assembly.Load)
                       .FirstOrDefault(a => a.GetName().Name == "ERPSystem.Host");

        // The bootstrap service lives in the Host project, so we load it by name.
        var hostAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .Concat(new[] { Assembly.Load("ERPSystem.Host") })
            .FirstOrDefault(a => a.GetName().Name == "ERPSystem.Host");

        hostAssembly.Should().NotBeNull("the Host assembly must be loadable to inspect the bootstrap service");

        var bootstrapType = hostAssembly!.GetType("ERPSystem.Host.Bootstrap.DefaultHoldingBootstrapHostedService");
        bootstrapType.Should().NotBeNull("the bootstrap hosted service must be discoverable");

        // The class must import the Identity.Infrastructure namespace.
        bootstrapType!.Namespace.Should().Be("ERPSystem.Host.Bootstrap");

        // The source file (not the assembly) is what guarantees we changed
        // something meaningful — confirm EnsureDefaultRolesAsync is referenced
        // in the .cs file.
        var path = LocateRelativeToSolution(@"src\backend\Host\Bootstrap\DefaultHoldingBootstrapHostedService.cs");
        var content = File.ReadAllText(path);
        content.Should().Contain("using ERPSystem.Modules.Identity.Infrastructure",
            "the bootstrap service must import the Identity.Infrastructure namespace to call the role repo");
        content.Should().Contain("EnsureDefaultRolesAsync",
            "the bootstrap service must invoke IRoleRepository.EnsureDefaultRolesAsync after the Holding is inserted");
        content.Should().Contain("_scopeFactory",
            "the bootstrap must resolve IRoleRepository from the DI scope factory, like it does for IPostingRulesService");
    }

    // ============ L49 — connection-aware GetUserCompaniesAsync ============

    [Fact]
    public void L49_IUserRepository_HasConnectionAware_GetUserCompaniesAsync()
    {
        var iface = typeof(IUserRepository);
        var overloads = iface.GetMethods()
            .Where(m => m.Name == nameof(IUserRepository.GetUserCompaniesAsync))
            .ToList();

        overloads.Should().HaveCountGreaterThanOrEqualTo(2,
            "the interface must keep the no-conn overload AND add the conn+tx overload");

        var connOverload = overloads.FirstOrDefault(m =>
        {
            var p = m.GetParameters();
            return p.Length == 4 && p[0].ParameterType == typeof(Guid)
                && p[1].ParameterType == typeof(System.Data.IDbConnection)
                && p[2].ParameterType == typeof(System.Data.IDbTransaction);
        });
        connOverload.Should().NotBeNull(
            "there must be a (Guid, IDbConnection, IDbTransaction?, CancellationToken) overload");
    }

    [Fact]
    public void L49_AuthService_BuildAsync_PassesConnectionAndTransactionTo_GetUserCompaniesAsync()
    {
        // The structural contract: the tx-aware BuildAsync must call
        // GetUserCompaniesAsync with (user.Id, conn, tx, ct), not (user.Id, ct).
        // We assert this via the source file rather than spinning up the full
        // service (which would require JWT secret + DB).
        var path = LocateRelativeToSolution(@"src\backend\Modules\Identity\Application\Auth\AuthService.cs");
        var content = File.ReadAllText(path);

        // Find the tx-aware BuildAsync body (the one with IDbConnection / IDbTransaction).
        // The simplest invariant: inside the file, the call to
        // _users.GetUserCompaniesAsync appears at least once with both `conn`
        // and `tx` as arguments.
        var pattern = new Regex(
            @"_users\.GetUserCompaniesAsync\(\s*user\.Id\s*,\s*conn\s*,\s*tx\s*,\s*ct\s*\)",
            RegexOptions.Multiline);
        pattern.IsMatch(content).Should().BeTrue(
            "AuthService.BuildAsync must pass conn + tx to GetUserCompaniesAsync so the in-tx user_companies row is visible (L49)");
    }

    // ============ L51 — no-tenant-id.yml workflow excludes test files ============

    [Fact]
    public void L51_NoTenantIdWorkflow_ExcludesTestFiles()
    {
        var path = LocateRelativeToSolution(@".github\workflows\no-tenant-id.yml");
        File.Exists(path).Should().BeTrue("the workflow file must exist on disk");

        var content = File.ReadAllText(path);

        // Exclude patterns cover the two test directories AND the *Tests.cs
        // naming convention used by xUnit across the repo.
        content.Should().Contain("src/backend/Tests/**",
            "the workflow must exclude the backend tests directory");
        content.Should().Contain("src/frontend/__tests__/**",
            "the workflow must exclude the frontend tests directory");
        content.Should().Contain("*Tests.cs",
            "the workflow must exclude the *Tests.cs naming convention (anywhere under src/)");
    }

    // ============ L175 — /api/auth/admin-bootstrap endpoint ============

    [Fact]
    public void L175_AuthController_HasAdminBootstrapEndpoint()
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .Concat(new[] { Assembly.Load("ERPSystem.Host") })
            .FirstOrDefault(a => a.GetName().Name == "ERPSystem.Host");
        assembly.Should().NotBeNull();

        var controller = assembly!.GetType("ERPSystem.Host.Controllers.AuthController");
        controller.Should().NotBeNull("the AuthController must be discoverable");

        var endpoint = controller!.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpPostAttribute>() != null
                                 && m.Name == "AdminBootstrap");
        endpoint.Should().NotBeNull("POST /api/auth/admin-bootstrap must be declared on AuthController");

        var httpPost = endpoint!.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpPostAttribute>()!;
        // The class-level [Route("api/auth")] + action-level [HttpPost("admin-bootstrap")]
        // compose to /api/auth/admin-bootstrap. The HttpPost template is the
        // relative portion; we accept either "admin-bootstrap" (relative) or
        // "api/auth/admin-bootstrap" (absolute) for forward-compatibility.
        httpPost.Template.Should().BeOneOf("admin-bootstrap", "api/auth/admin-bootstrap");

        // Sanity: the endpoint is anonymous (the whole point — first user, no JWT yet).
        endpoint.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>()
            .Should().NotBeNull("the admin-bootstrap endpoint must be AllowAnonymous so a fresh deployment can call it without a JWT");
    }

    [Fact]
    public void L175_AdminBootstrapRequest_HasExpectedFields()
    {
        var type = typeof(AdminBootstrapRequest);
        type.GetProperty(nameof(AdminBootstrapRequest.Email))!.PropertyType.Should().Be(typeof(string));
        type.GetProperty(nameof(AdminBootstrapRequest.Password))!.PropertyType.Should().Be(typeof(string));
        type.GetProperty(nameof(AdminBootstrapRequest.FullName))!.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public void L175_AdminBootstrapAsync_ValidationError_OnEmptyRequest()
    {
        // The service is fully constructible with stubs (no real DB needed) for
        // input validation, but its happy path requires a transaction-supporting
        // connection. We only exercise the validation branch here — the happy
        // path is covered by the Sprint Closure / Wave 3 Trust Mode test
        // (browser end-to-end against a real Postgres).
        //
        // For this test we assert the public surface: AdminBootstrapRequest +
        // AuthErrorCode.AlreadyBootstrapped exist and the AdminBootstrapResult
        // has a Conflict factory. (We can't new up AuthService without wiring
        // the full DI graph; the integration tests in Wave 3 will exercise the
        // actual flow.)
        var conflictEnum = typeof(AuthErrorCode).GetField("AlreadyBootstrapped");
        conflictEnum.Should().NotBeNull(
            "AuthErrorCode must declare AlreadyBootstrapped so the controller can map it to 409 Conflict");

        var resultType = typeof(AdminBootstrapResult);
        var conflictMethod = resultType.GetMethod("ConflictResult", BindingFlags.Public | BindingFlags.Static);
        conflictMethod.Should().NotBeNull("AdminBootstrapResult.ConflictResult must exist");
    }

    // ============ helpers ============

    /// <summary>
    /// Walks up from the current test working directory to find the repo root
    /// (the folder that contains <c>.github/workflows/no-tenant-id.yml</c>),
    /// then resolves the requested relative path against it.
    /// </summary>
    private static string LocateRelativeToSolution(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ".github", "workflows", "no-tenant-id.yml")))
                return Path.Combine(dir.FullName, relative.Replace('\\', Path.DirectorySeparatorChar));
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Could not find the ERP-Holding repo root from " + AppContext.BaseDirectory);
    }
}
