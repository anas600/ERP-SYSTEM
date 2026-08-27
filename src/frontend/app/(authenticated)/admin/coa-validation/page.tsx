'use client';

// Sprint 60 Wave 3B (DEC-191) — CoA Validation Page
// يعرض نتائج CoAValidationService (موجود في Wave 3A، DEC-190) —
// يفحص: journal_line orphans، trial balance mismatch، duplicate codes،
// invalid code format، legacy accounts.

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import {
  CheckCircle2, XCircle, AlertCircle, RefreshCw, Shield, ArrowLeft, FileText,
} from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { api, getErrorMessage } from '@/lib/api';

interface CoAValidationIssue {
  code: string;
  severity: 'Error' | 'Warning' | 'Info';
  message: string;
  accountCode?: string;
  accountId?: string;
  details?: string;
}

interface CoAValidationResult {
  isValid: boolean;
  issues: CoAValidationIssue[];
  warningCount: number;
  errorCount: number;
  companyId: string;
  validatedAt: string;
}

export default function CoAValidationPage() {
  const router = useRouter();
  const [result, setResult] = useState<CoAValidationResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const runValidation = async () => {
    setLoading(true);
    setError(null);
    try {
      const r = await api.get<CoAValidationResult>('/api/admin/coa/validate');
      setResult(r.data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تشغيل تحقق CoA. تأكد أن لديك صلاحيات Admin.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { runValidation(); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, []);

  return (
    <div>
      <Link href="/dashboard" className="inline-flex items-center gap-1 text-sm text-ink-500 hover:text-brand-600 mb-3 transition-colors">
        <ArrowLeft className="h-4 w-4" />
        العودة للوحة التحكم
      </Link>
      <PageHeader
        title="🛡️ تحقق من CoA"
        description="Sprint 60 Wave 3B (DEC-191): فحص سلامة دليل الحسابات — journal_lines orphans، trial balance، أكواد مكررة، تنسيق، حسابات قديمة."
        actions={
          <Button
            variant="primary"
            onClick={runValidation}
            disabled={loading}
            iconLeft={<RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />}
          >
            {loading ? 'جاري الفحص...' : 'إعادة التحقق'}
          </Button>
        }
      />

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm flex items-start gap-2">
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" /><span>{error}</span>
        </div>
      )}

      {loading && !result ? (
        <div className="text-center py-12 text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري فحص CoA...</p>
        </div>
      ) : !result ? null : (
        <>
          {/* ملخص النتيجة */}
          <Card className={`p-4 mb-4 border-r-4 ${result.isValid ? 'border-green-500 bg-green-50/40' : 'border-danger-500 bg-red-50/40'}`}>
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                {result.isValid ? <CheckCircle2 className="h-7 w-7 text-green-600" /> : <XCircle className="h-7 w-7 text-danger-600" />}
                <div>
                  <p className={`text-lg font-bold ${result.isValid ? 'text-green-800' : 'text-red-800'}`}>
                    {result.isValid ? 'دليل الحسابات سليم ✓' : 'دليل الحسابات فيه مشاكل ✗'}
                  </p>
                  <p className="text-xs text-gray-500">
                    {result.issues.length === 0
                      ? 'لا توجد مشاكل — CoA جاهز للإنتاج.'
                      : `${result.errorCount} خطأ، ${result.warningCount} تحذير`}
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-6 text-sm">
                <div>
                  <p className="text-xs text-gray-500">الأخطاء</p>
                  <p className={`font-mono font-bold ${result.errorCount > 0 ? 'text-red-700' : 'text-gray-400'}`}>
                    {result.errorCount}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">التحذيرات</p>
                  <p className={`font-mono font-bold ${result.warningCount > 0 ? 'text-yellow-700' : 'text-gray-400'}`}>
                    {result.warningCount}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">الشركة</p>
                  <p className="font-mono text-xs text-gray-600">{result.companyId ? `${result.companyId.slice(0, 8)}...` : '—'}</p>
                </div>
              </div>
            </div>
          </Card>

          {/* جدول المشاكل */}
          {result.issues.length > 0 && (
            <Card className="p-0 overflow-hidden">
              <div className="px-4 py-3 border-b border-gray-200 bg-gradient-to-l from-blue-50 to-white flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <FileText className="h-4 w-4 text-blue-700" />
                  <h3 className="text-sm font-bold text-blue-900">قائمة المشاكل ({result.issues.length})</h3>
                </div>
                <span className="text-xs text-gray-500">
                  {result.validatedAt ? new Date(result.validatedAt).toLocaleString('ar-LY') : '—'}
                </span>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm" dir="rtl">
                  <thead className="bg-white border-b border-gray-100">
                    <tr>
                      <th className="text-start px-3 py-2 text-xs font-semibold text-gray-600">الخطورة</th>
                      <th className="text-start px-3 py-2 text-xs font-semibold text-gray-600">الكود</th>
                      <th className="text-start px-3 py-2 text-xs font-semibold text-gray-600">الحساب</th>
                      <th className="text-start px-3 py-2 text-xs font-semibold text-gray-600">الرسالة</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.issues.map((issue, idx) => {
                      const isError = issue.severity === 'Error';
                      return (
                        <tr
                          key={`${issue.code}-${issue.accountCode ?? idx}`}
                          className={`border-b border-gray-100 last:border-b-0 ${
                            isError ? 'bg-red-50/40' : 'bg-yellow-50/40'
                          }`}
                        >
                          <td className="px-3 py-2">
                            <span
                              className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-semibold ${
                                isError
                                  ? 'bg-red-100 text-red-800'
                                  : 'bg-yellow-100 text-yellow-800'
                              }`}
                            >
                              {isError ? <XCircle className="h-3 w-3" /> : <AlertCircle className="h-3 w-3" />}
                              {issue.severity === 'Error' ? 'خطأ' : 'تحذير'}
                            </span>
                          </td>
                          <td className="px-3 py-2 font-mono text-xs text-blue-600">
                            {issue.accountCode ?? <span className="text-gray-400">—</span>}
                          </td>
                          <td className="px-3 py-2 text-xs text-gray-700">
                            {issue.accountId ? (
                              <button
                                onClick={() => router.push(`/finance/accounts/${issue.accountId}/edit`)}
                                className="text-blue-600 hover:underline"
                                type="button"
                              >
                                {issue.accountCode ?? issue.accountId.slice(0, 8)}
                              </button>
                            ) : (
                              <span className="text-gray-400">—</span>
                            )}
                          </td>
                          <td className="px-3 py-2 text-gray-800">
                            <div className="font-mono text-[10px] text-gray-400 mb-0.5">{issue.code}</div>
                            <div>{issue.message}</div>
                            {issue.details && (
                              <div className="text-xs text-gray-500 mt-1">{issue.details}</div>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </Card>
          )}

          {/* شرح الفحوصات (دائماً معروض) */}
          <Card className="mt-4 p-4 bg-gray-50/60">
            <h3 className="text-sm font-bold text-gray-700 mb-2 flex items-center gap-2">
              <Shield className="h-4 w-4" /> الفحوصات التي يقوم بها CoAValidationService
            </h3>
            <ul className="text-xs text-gray-600 space-y-1 list-disc list-inside">
              <li><b>Journal line integrity</b> — لا توجد journal_line تشير إلى حساب غير موجود.</li>
              <li><b>Trial balance</b> — Σ Debit = Σ Credit لكل شركة في القيود المرحلة.</li>
              <li><b>Unique codes</b> — لا يوجد كود حساب مكرر داخل نفس الشركة.</li>
              <li><b>Code format</b> — الكود يطابق النمط القانوني (X.X.XX.XXX) أو الـ 4-digit القديم.</li>
              <li><b>Legacy account audit</b> — الحسابات القديمة (is_canonical=FALSE) تُسجَّل كـ Warning، لا Error.</li>
              <li><b>Deprecated account usage</b> — لا توجد حركة على حساب deprecated.</li>
            </ul>
            <p className="text-xs text-gray-500 mt-3">
              راجع نتائج <Link href="/finance/accounts" className="text-blue-600 hover:underline">دليل الحسابات</Link> للتفاصيل.
            </p>
          </Card>
        </>
      )}
    </div>
  );
}
