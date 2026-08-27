// Sprint 61 Wave 1A (DEC-192, DEC-193, DEC-194) — Tests for the EngineerReport
// entities (no DB, no FluentMigrator — pure C# object behavior).
//
// Coverage:
//   1. EngineerReport — defaults (Status = Draft, WorkDone = "")
//   2. EngineerReportStatus — enum values (Draft, Submitted, Approved, Rejected)
//   3. EngineerReport — CompanyId is Guid (NOT Guid?) per DEC-097
//   4. EngineerReportPhoto — CompanyId is Guid (NOT Guid?) per L19
//   5. EngineerReportSignoff — CompanyId is Guid (NOT Guid?) per L19
//   6. EngineerReportSignoff — Approved is a non-nullable bool (required field)
//   7. DTOs — SignoffRequest / CreateEngineerReportRequest / UpdateEngineerReportRequest
//      exist and have the expected fields

using System;
using System.Linq;
using System.Reflection;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Entities;
using FluentAssertions;

namespace ERPSystem.Tests.Projects;

public class Sprint61EngineerReportEntitiesTests
{
    // ============== 1. EngineerReport defaults ==============

    [Fact]
    public void EngineerReport_Default_Status_Is_Draft()
    {
        var report = new EngineerReport();

        report.Status.Should().Be(EngineerReportStatus.Draft,
            "a newly constructed EngineerReport must default to Draft (engineer is still writing)");
    }

    [Fact]
    public void EngineerReport_Default_WorkDone_Is_Empty_String_Not_Null()
    {
        // The DB column is NOT NULL, so the in-memory default must not be null either
        // (would cause NRE when the controller serializes to JSON before the service
        // assigns the user-supplied value). The string is empty by default.
        var report = new EngineerReport();

        report.WorkDone.Should().NotBeNull(
            "work_done is a required DB column; the entity default must be an empty string, not null");
        report.WorkDone.Should().Be(string.Empty);
    }

    // ============== 2. EngineerReportStatus enum ==============

    [Fact]
    public void EngineerReportStatus_Has_All_4_Expected_Members()
    {
        var names = Enum.GetNames<EngineerReportStatus>();

        names.Should().BeEquivalentTo(
            new[] { "Draft", "Submitted", "Approved", "Rejected" },
            "the status enum must expose all 4 DEC-192 workflow states");
    }

    [Fact]
    public void EngineerReportStatus_Values_Are_Stable_For_Backward_Compatibility()
    {
        // The numeric values are persisted nowhere (status is stored as TEXT per DEC-192),
        // but we still pin them so accidental reordering does not silently break any
        // (future) int-backed column or external API consumer.
        ((int)EngineerReportStatus.Draft).Should().Be(1);
        ((int)EngineerReportStatus.Submitted).Should().Be(2);
        ((int)EngineerReportStatus.Approved).Should().Be(3);
        ((int)EngineerReportStatus.Rejected).Should().Be(4);
    }

    // ============== 3-5. CompanyId is Guid (not Guid?) ==============

    [Theory]
    [InlineData(typeof(EngineerReport))]
    [InlineData(typeof(EngineerReportPhoto))]
    [InlineData(typeof(EngineerReportSignoff))]
    public void Entity_CompanyId_Is_NonNullable_Guid(Type entityType)
    {
        // Per Constitution Article 3 + L19 + DEC-097, the CompanyId property on every
        // entity backed by a `company_id NOT NULL` DB column must be a non-nullable Guid.
        // A nullable type would NRE at runtime on the first access and is a code-level
        // inconsistency (the column is NOT NULL).
        var prop = entityType.GetProperty("CompanyId", BindingFlags.Public | BindingFlags.Instance);

        prop.Should().NotBeNull($"{entityType.Name} must declare a public CompanyId property");
        prop!.PropertyType.Should().Be(typeof(Guid),
            $"{entityType.Name}.CompanyId must be a non-nullable Guid (L19 / DEC-097), " +
            $"not Guid? (the DB column is NOT NULL)");
    }

    // ============== 6. EngineerReportSignoff.Approved is bool (not bool?) ==============

    [Fact]
    public void EngineerReportSignoff_Approved_Is_NonNullable_Bool()
    {
        // The DB column `approved BOOLEAN NOT NULL` requires the in-memory default
        // to also be non-nullable. A null bool would NRE on the first read and break
        // JSON serialization to the FE.
        var prop = typeof(EngineerReportSignoff)
            .GetProperty("Approved", BindingFlags.Public | BindingFlags.Instance);

        prop.Should().NotBeNull();
        prop!.PropertyType.Should().Be(typeof(bool),
            "engineer_report_signoffs.approved is NOT NULL; EngineerReportSignoff.Approved must be non-nullable bool");
    }

    // ============== 7. DTOs exist with the expected fields ==============

    [Fact]
    public void Dtos_Expose_All_Expected_Records_And_Fields()
    {
        // The service layer (Wave 2A) maps entity → DTO. Asserting the DTO surface
        // here guards against accidental rename that would break the (future) controller.
        var dtoTypes = new[]
        {
            typeof(EngineerReportResponse),
            typeof(EngineerReportPhotoResponse),
            typeof(EngineerReportSignoffResponse),
            typeof(CreateEngineerReportRequest),
            typeof(UpdateEngineerReportRequest),
            typeof(SignoffRequest)
        };

        foreach (var t in dtoTypes)
        {
            t.Should().NotBeNull($"{t.Name} must exist as a public record in EngineerReportDtos.cs");
            t.IsClass.Should().BeTrue($"{t.Name} must be a class (record compiles to a class)");
        }

        // SignoffRequest must expose the 4 DEC-194 fields the FE will POST.
        var signoffProps = typeof(SignoffRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet();
        signoffProps.Should().BeEquivalentTo(
            new[] { "SignerRole", "SignatureText", "Comment", "Approved" },
            "SignoffRequest must expose the 4 DEC-194 fields the FE will POST");
    }
}
