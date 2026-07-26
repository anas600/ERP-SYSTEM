using System.Data;
using Npgsql;

namespace ERPSystem.Shared.Infrastructure;

/// <summary>
/// مصنع اتصالات قاعدة البيانات
/// يوفّر connection واحد لكل عملية (Scoped) لتجنّب مشاكل الـ connection pooling
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>إنشاء اتصال جديد على قاعدة OLTP (Postgres الرئيسي)</summary>
    Task<IDbConnection> CreateOltpConnectionAsync(CancellationToken ct = default);

    /// <summary>إنشاء اتصال على قاعدة الـ Event Store (MartenDB schema)</summary>
    Task<IDbConnection> CreateEventStoreConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// ينشئ اتصالاً واحداً مباشراً (بدون pool) — للاستخدام في الـ bootstrap فقط.
    /// السبب: Supabase pgbouncer transaction-mode pool (port 6543) يقفل الـ backend
    /// connections بعد كل transaction. لو الـ bootstrap فتح N connections متتالية
    /// من الـ client pool، الـ acquire الثاني قد ينتظر 5+ دقائق حتى pgbouncer
    /// يجد backend connection متاح. الحل: اتصال واحد مباشر لكل عملية bootstrap.
    /// </summary>
    Task<IDbConnection> CreateEphemeralOltpConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// اتصال مباشر (port 5432, بدون Supavisor/pgbouncer) مخصّص للـ schema migrations
    /// (FluentMigrator + DataTypeMigrator). السبب: pgbouncer transaction-mode
    /// (port 6543) يـ release الـ backend بعد كل transaction، فـ DDL statements
    /// المتعاقبة ممكن توصل backends مختلفة → CREATE TABLE users على backend A،
    /// ALTER TABLE users ADD COLUMN على backend B ما شافش الـ table → "42P01:
    /// relation users does not exist". الحل الرسمي (Supabase docs): use direct
    /// connection on port 5432 for migrations. يُرجع null لو ما في
    /// ConnectionStrings:Migrations معرّف (الـ migrator يستخدم ephemeral OLTP كـ fallback).
    /// </summary>
    Task<IDbConnection?> CreateEphemeralMigrationConnectionAsync(CancellationToken ct = default);
}
