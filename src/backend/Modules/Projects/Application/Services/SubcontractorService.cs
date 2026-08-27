using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Services;

// =============================================================================
// Sprint 64 / DEC-221 — Subcontractor service.
//
// Manages CRUD on subcontractors. CompanyId always comes from the JWT context
// (L19 / DEC-095), never from the request DTO.
// =============================================================================

/// <summary>Result envelope for Subcontractor service calls.</summary>
public sealed class SubcontractorResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public SubcontractorErrorCode? ErrorCode { get; init; }
    public static SubcontractorResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static SubcontractorResult<T> Fail(string e, SubcontractorErrorCode c) =>
        new() { Succeeded = false, Error = e, ErrorCode = c };
}

public enum SubcontractorErrorCode
{
    NotFound, AlreadyExists, ValidationError, Internal
}

public interface ISubcontractorService
{
    Task<SubcontractorResult<SubcontractorResponse>> CreateAsync(
        Guid userId, CreateSubcontractorRequest req, CancellationToken ct);

    Task<SubcontractorResult<SubcontractorResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateSubcontractorRequest req, CancellationToken ct);

    Task<SubcontractorResult<SubcontractorResponse>> GetByIdAsync(
        Guid id, CancellationToken ct);

    Task<SubcontractorResult<IReadOnlyList<SubcontractorResponse>>> ListAsync(
        bool? isActive, string? tradeSpecialty, int skip, int take, CancellationToken ct);

    Task<SubcontractorResult<bool>> SoftDeleteAsync(
        Guid userId, Guid id, CancellationToken ct);
}

/// <summary>
/// Sprint 64 / DEC-221 — Subcontractor service.
///
/// <para><b>Validation rules</b>:</para>
/// <list type="bullet">
///   <item>Code is unique within the company (DB-enforced UNIQUE).</item>
///   <item>Name is required.</item>
///   <item>Email (if provided) must be a valid format.</item>
/// </list>
///
/// <para><b>L19 / DEC-095</b>: CompanyId comes from <see cref="ICompanyContext"/>,
/// never from the request DTO. The list/get queries are scoped by company where
/// applicable.</para>
/// </summary>
public sealed class SubcontractorService : ISubcontractorService
{
    private readonly ISubcontractorRepository _subcontractors;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<SubcontractorService> _logger;

    public SubcontractorService(
        ISubcontractorRepository subcontractors,
        ICompanyContext companyContext,
        ILogger<SubcontractorService> logger)
    {
        _subcontractors = subcontractors;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task<SubcontractorResult<SubcontractorResponse>> CreateAsync(
        Guid userId, CreateSubcontractorRequest req, CancellationToken ct)
    {
        // L19 / DEC-095: CompanyId from JWT, not from request.
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        // Validation
        if (string.IsNullOrWhiteSpace(req.Code))
            return SubcontractorResult<SubcontractorResponse>.Fail(
                "الرمز مطلوب.", SubcontractorErrorCode.ValidationError);
        if (string.IsNullOrWhiteSpace(req.Name))
            return SubcontractorResult<SubcontractorResponse>.Fail(
                "الاسم مطلوب.", SubcontractorErrorCode.ValidationError);
        if (!string.IsNullOrWhiteSpace(req.Email) && !IsValidEmail(req.Email))
            return SubcontractorResult<SubcontractorResponse>.Fail(
                "البريد الإلكتروني غير صالح.", SubcontractorErrorCode.ValidationError);

        // UNIQUE (company_id, code) — check for an existing row with the same code.
        var existing = await _subcontractors.GetByCodeAsync(companyId, req.Code.Trim(), ct);
        if (existing != null)
            return SubcontractorResult<SubcontractorResponse>.Fail(
                $"يوجد بالفعل مقاول باطن بنفس الرمز ({req.Code}).",
                SubcontractorErrorCode.AlreadyExists);

        var now = DateTime.UtcNow;
        var sub = new Subcontractor
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            NameAr = req.NameAr?.Trim(),
            ContactPerson = req.ContactPerson?.Trim(),
            Phone = req.Phone?.Trim(),
            Email = req.Email?.Trim(),
            TradeSpecialty = req.TradeSpecialty?.Trim(),
            TaxId = req.TaxId?.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _subcontractors.InsertAsync(sub, ct);
        _logger.LogInformation("Subcontractor created {Id} code={Code} by {UserId}",
            sub.Id, sub.Code, userId);
        return SubcontractorResult<SubcontractorResponse>.Ok(MapToResponse(sub));
    }

    public async Task<SubcontractorResult<SubcontractorResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateSubcontractorRequest req, CancellationToken ct)
    {
        var sub = await _subcontractors.GetByIdAsync(id, ct);
        if (sub == null)
            return SubcontractorResult<SubcontractorResponse>.Fail(
                "المقاول الباطن غير موجود.", SubcontractorErrorCode.NotFound);

        if (string.IsNullOrWhiteSpace(req.Name))
            return SubcontractorResult<SubcontractorResponse>.Fail(
                "الاسم مطلوب.", SubcontractorErrorCode.ValidationError);
        if (!string.IsNullOrWhiteSpace(req.Email) && !IsValidEmail(req.Email))
            return SubcontractorResult<SubcontractorResponse>.Fail(
                "البريد الإلكتروني غير صالح.", SubcontractorErrorCode.ValidationError);

        sub.Name = req.Name.Trim();
        sub.NameAr = req.NameAr?.Trim();
        sub.ContactPerson = req.ContactPerson?.Trim();
        sub.Phone = req.Phone?.Trim();
        sub.Email = req.Email?.Trim();
        sub.TradeSpecialty = req.TradeSpecialty?.Trim();
        sub.TaxId = req.TaxId?.Trim();
        sub.IsActive = req.IsActive;
        sub.UpdatedAt = DateTime.UtcNow;
        await _subcontractors.UpdateAsync(sub, ct);
        _logger.LogInformation("Subcontractor updated {Id} by {UserId}", sub.Id, userId);
        return SubcontractorResult<SubcontractorResponse>.Ok(MapToResponse(sub));
    }

    public async Task<SubcontractorResult<SubcontractorResponse>> GetByIdAsync(
        Guid id, CancellationToken ct)
    {
        var sub = await _subcontractors.GetByIdAsync(id, ct);
        if (sub == null)
            return SubcontractorResult<SubcontractorResponse>.Fail(
                "المقاول الباطن غير موجود.", SubcontractorErrorCode.NotFound);
        return SubcontractorResult<SubcontractorResponse>.Ok(MapToResponse(sub));
    }

    public async Task<SubcontractorResult<IReadOnlyList<SubcontractorResponse>>> ListAsync(
        bool? isActive, string? tradeSpecialty, int skip, int take, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        var rows = await _subcontractors.ListAsync(companyId, isActive, tradeSpecialty, skip, take, ct);
        var list = rows.Select(MapToResponse).ToList();
        return SubcontractorResult<IReadOnlyList<SubcontractorResponse>>.Ok(list);
    }

    public async Task<SubcontractorResult<bool>> SoftDeleteAsync(
        Guid userId, Guid id, CancellationToken ct)
    {
        var sub = await _subcontractors.GetByIdAsync(id, ct);
        if (sub == null)
            return SubcontractorResult<bool>.Fail(
                "المقاول الباطن غير موجود.", SubcontractorErrorCode.NotFound);
        if (!sub.IsActive)
            return SubcontractorResult<bool>.Fail(
                "المقاول الباطن معطّل بالفعل.", SubcontractorErrorCode.ValidationError);

        var ok = await _subcontractors.SoftDeleteAsync(id, ct);
        if (!ok)
            return SubcontractorResult<bool>.Fail(
                "فشل تعطيل المقاول الباطن.", SubcontractorErrorCode.Internal);

        _logger.LogInformation("Subcontractor soft-deleted {Id} by {UserId}", id, userId);
        return SubcontractorResult<bool>.Ok(true);
    }

    // ===== Helpers =====

    private static SubcontractorResponse MapToResponse(Subcontractor s) => new(
        s.Id, s.CompanyId, s.Code, s.Name, s.NameAr,
        s.ContactPerson, s.Phone, s.Email,
        s.TradeSpecialty, s.TaxId, s.IsActive,
        s.CreatedAt, s.UpdatedAt);

    // Minimal email regex — covers common shapes without being overly strict.
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled);

    private static bool IsValidEmail(string email) => EmailRegex.IsMatch(email);
}
