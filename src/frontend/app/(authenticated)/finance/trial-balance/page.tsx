'use client';

// Sprint 36 (DEC-122): Trial Balance (ميزان المراجعة)
// Per-account balances at a given date.
// Standard accounting rule: Total Debits = Total Credits (Balanced).

import { useEffect, useState, useMemo } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Scale, RefreshCw, Calendar, AlertCircle, CheckCircle2, XCircle, Wallet } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import {
  financeApi,
  TrialBalanceRow,
  AccountTypeName,
  ACCOUNT_TYPE_LABELS,
  ACCOUNT_TYPE_ORDER,
  getErrorMessage,
} from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { formatNumber } from '@/lib/format';

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export default function TrialBalancePage() {
  const router = useRouter();
  const [rows, setRows] = useState<TrialBalanceRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [asOf, setAsOf] = useState<string>(todayIso());

  const load = async (date?: string) => {
    setLoading(true);
    setError(null);
    try {
      const r = await financeApi.getTrialBalance(date);
      setRows(r);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل ميزان المراجعة.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const onApply = () => {
    load(asOf || undefined);
  };

  const onClear = () => {
    setAsOf('');
    load(undefined);
  };

  // Group by account type
  const grouped = useMemo(() => {
    const map = new Map<AccountTypeName, TrialBalanceRow[]>();
    for (const r of rows) {
      if (!map.has(r.type)) map.set(r.type, []);
      map.get(r.type)!.push(r);
    }
    // Sort each group by accountCode
    for (const list of map.values()) {
      list.sort((a, b) => a.accountCode.localeCompare(b.accountCode));
    }
    return map;
  }, [rows]);

  // Totals
  const totals = useMemo(() => {
    let totalDebit = 0;
    let totalCredit = 0;
    for (const r of rows) {
      totalDebit += r.totalDebit;
      totalCredit += r.totalCredit;
    }
    // Balanced iff totalDebit ≈ totalCredit (within 0.0001)
    const balanced = Math.abs(totalDebit - totalCredit) < 0.0001;
    return { totalDebit, totalCredit, balanced };
  }, [rows]);

  return (
    <div>
      <PageHeader
        title="ميزان المراجعة"
        description="أرصدة كل الحسابات في تاريخ معين — مجموع المدين يجب أن يساوي مجموع الدائن"
        actions={
          <Link href="/finance/accounts">
            <Button variant="secondary" iconLeft={<Wallet className="h-4 w-4" />}>
              دليل الحسابات
            </Button>
          </Link>
        }
      />

      {/* Date filter */}
      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">كما في تاريخ</label>
            <input
              type="date"
              value={asOf}
              onChange={(e) => setAsOf(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <Button variant="primary" onClick={onApply} iconLeft={<Calendar className="h-4 w-4" />}>
            تطبيق
          </Button>
          <Button variant="secondary" onClick={onClear}>
            كل الأرصدة
          </Button>
          <Button variant="ghost" onClick={() => load(asOf || undefined)} iconLeft={<RefreshCw className="h-4 w-4" />}>
            إعادة تحميل
          </Button>
        </div>
      </Card>

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm flex items-start gap-2">
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <span>{error}</span>
        </div>
      )}

      {loading ? (
        <div className="text-center py-12 text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </div>
      ) : rows.length === 0 ? (
        <Card className="p-12 text-center text-gray-500">
          <Scale className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          لا توجد حسابات في ميزان المراجعة.
        </Card>
      ) : (
        <>
          {/* Balance Bar */}
          <Card
            className={`p-4 mb-4 ${
              totals.balanced
                ? 'border-r-4 border-green-500 bg-green-50/40'
                : 'border-r-4 border-danger-500 bg-red-50/40'
            }`}
          >
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                {totals.balanced ? (
                  <CheckCircle2 className="h-7 w-7 text-green-600" />
                ) : (
                  <XCircle className="h-7 w-7 text-danger-600" />
                )}
                <div>
                  <p
                    className={`text-lg font-bold ${
                      totals.balanced ? 'text-green-800' : 'text-red-800'
                    }`}
                  >
                    {totals.balanced ? 'ميزان متوازن ✓' : 'ميزان غير متوازن ✗'}
                  </p>
                  <p className="text-xs text-gray-500">
                    مجموع المدين = مجموع الدائن (
                    {totals.balanced
                      ? 'الفرق = 0.00'
                      : `الفرق = ${formatNumber(Math.abs(totals.totalDebit - totals.totalCredit))} LYD`}
                    )
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-6 text-sm">
                <div>
                  <p className="text-xs text-gray-500">إجمالي المدين</p>
                  <p className="font-mono font-bold text-blue-700">{formatNumber(totals.totalDebit)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">إجمالي الدائن</p>
                  <p className="font-mono font-bold text-green-700">{formatNumber(totals.totalCredit)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">عدد الحسابات</p>
                  <p className="font-mono font-bold text-gray-800">{rows.length}</p>
                </div>
              </div>
            </div>
          </Card>

          {/* Grouped tables per AccountType */}
          <div className="space-y-4">
            {ACCOUNT_TYPE_ORDER
              .filter((t) => grouped.has(t) && grouped.get(t)!.length > 0)
              .map((t) => {
                const list = grouped.get(t)!;
                const typeDebit = list.reduce((s, r) => s + r.totalDebit, 0);
                const typeCredit = list.reduce((s, r) => s + r.totalCredit, 0);
                return (
                  <Card key={t} className="overflow-x-auto p-0">
                    <div className="px-4 py-3 bg-gray-50 border-b border-gray-200 flex items-center justify-between">
                      <h3 className="text-sm font-bold text-gray-800">
                        {ACCOUNT_TYPE_LABELS[t]}{' '}
                        <span className="text-xs text-gray-500 font-normal">({list.length} حساب)</span>
                      </h3>
                      <div className="text-xs text-gray-600 font-mono">
                        مدين: <span className="font-bold text-blue-700">{formatNumber(typeDebit)}</span> •
                        دائن: <span className="font-bold text-green-700">{formatNumber(typeCredit)}</span>
                      </div>
                    </div>
                    <table className="w-full text-sm" dir="rtl">
                      <thead className="bg-white border-b border-gray-100">
                        <tr>
                          <th className="text-start px-4 py-2 font-semibold text-gray-600 text-xs">الكود</th>
                          <th className="text-start px-4 py-2 font-semibold text-gray-600 text-xs">اسم الحساب</th>
                          <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">إجمالي المدين</th>
                          <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">إجمالي الدائن</th>
                          <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">الرصيد</th>
                          <th className="text-center px-4 py-2 font-semibold text-gray-600 text-xs">طبيعة</th>
                        </tr>
                      </thead>
                      <tbody>
                        {list.map((r) => (
                          <tr
                            key={r.accountId}
                            className="border-b border-gray-100 hover:bg-blue-50/40 transition-colors cursor-pointer"
                            onClick={() => router.push(`/finance/reports/general-ledger?accountId=${r.accountId}`)}
                          >
                            <td className="px-4 py-2 font-mono text-xs text-blue-600">{r.accountCode}</td>
                            <td className="px-4 py-2 text-gray-800">{r.accountName}</td>
                            <td className="px-4 py-2 text-end font-mono text-blue-700">
                              {r.totalDebit > 0 ? formatNumber(r.totalDebit) : '—'}
                            </td>
                            <td className="px-4 py-2 text-end font-mono text-green-700">
                              {r.totalCredit > 0 ? formatNumber(r.totalCredit) : '—'}
                            </td>
                            <td
                              className={`px-4 py-2 text-end font-mono font-bold ${
                                r.balance > 0
                                  ? r.normalBalance === 'Debit'
                                    ? 'text-blue-700'
                                    : 'text-orange-700'
                                  : r.balance < 0
                                  ? r.normalBalance === 'Debit'
                                    ? 'text-orange-700'
                                    : 'text-blue-700'
                                  : 'text-gray-500'
                              }`}
                            >
                              {formatNumber(r.balance)}
                            </td>
                            <td className="px-4 py-2 text-center text-xs text-gray-500">
                              {r.normalBalance === 'Debit' ? 'مدين' : 'دائن'}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </Card>
                );
              })}
          </div>

          <p className="mt-3 text-xs text-gray-500">
            كما في تاريخ <span className="font-mono font-semibold">{asOf || 'اليوم'}</span> • الحسابات مرتبة حسب النوع
            ثم الكود
          </p>
        </>
      )}
    </div>
  );
}
