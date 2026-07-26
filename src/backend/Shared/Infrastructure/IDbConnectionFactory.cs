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
}
