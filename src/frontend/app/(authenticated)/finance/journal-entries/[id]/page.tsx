'use client';

// تفاصيل قيد محاسبي (Journal Entry Detail)
// Sprint 39 (DEC-125) L60: use financeApi.getJournalEntry / postJournalEntry (auto-JWT)

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Send } from 'lucide-react';
import { Button, Card, PageHeader, Badge, ConfirmDialog, useToast } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { financeApi, getErrorMessage, JournalEntryDetail, JournalEntryLine } from '@/lib/api';
import { formatNumber } from '@/lib/format';

const STATUS_BADGE: Record<number, { label: string; variant: 'neutral' | 'success' | 'warning' }> = {
  1: { label: 'مسودة', variant: 'warning' },
  2: { label: 'مُرحَّل', variant: 'success' },
  3: { label: 'معكوس', variant: 'neutral' },
};

export default function JournalEntryDetailPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  useAuth();
  const toast = useToast();
  const [entry, setEntry] = useState<JournalEntryDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [posting, setPosting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmPost, setConfirmPost] = useState(false);

  const load = async () => {
    if (!params.id) return;
    setLoading(true);
    try {
      const data = await financeApi.getJournalEntry(params.id);
      setEntry(data);
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'فشل التحميل');
      setError(msg);
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params.id]);

  const handlePost = async () => {
    if (!entry) return;
    setConfirmPost(false);
    setPosting(true);
    try {
      await financeApi.postJournalEntry(entry.id);
      toast.success(`تم ترحيل القيد ${entry.entryNumber} بنجاح`);
      await load();
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'فشل ترحيل القيد.');
      setError(msg);
      toast.error(msg);
    } finally {
      setPosting(false);
    }
  };

  if (loading) {
    return (
      <div>
        <PageHeader title="قيد محاسبي" />
        <Card className="max-w-4xl"><div className="text-center py-12 text-gray-500">جاري التحميل...</div></Card>
      </div>
    );
  }

  if (!entry) {
    return (
      <div>
        <PageHeader title="قيد محاسبي" />
        <Card className="max-w-4xl">
          <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg text-sm">{error || 'القيد غير موجود'}</div>
          <div className="mt-4"><Link href="/finance/journal-entries"><Button variant="ghost">رجوع</Button></Link></div>
        </Card>
      </div>
    );
  }

  const statusInfo = STATUS_BADGE[entry.status] || { label: `Status ${entry.status}`, variant: 'neutral' as const };
  const balanced = Math.abs(Number(entry.totalDebit) - Number(entry.totalCredit)) < 0.01;

  return (
    <div>
      <PageHeader
        title="📒 قيد محاسبي"
        description={entry.entryNumber}
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'القيود', href: '/finance/journal-entries' },
          { label: entry.entryNumber },
        ]}
        actions={
          <div className="flex items-center gap-2">
            {entry.status === 1 && (
              <Button variant="primary" onClick={() => setConfirmPost(true)} loading={posting} iconLeft={<Send className="h-4 w-4" />}>
                ترحيل القيد
              </Button>
            )}
            <Link href="/finance/journal-entries">
              <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>رجوع</Button>
            </Link>
          </div>
        }
      />

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm">{error}</div>
      )}

      <Card className="max-w-4xl">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
          <div>
            <p className="text-ink-500">رقم القيد</p>
            <p className="font-mono text-brand-600">{entry.entryNumber}</p>
          </div>
          <div>
            <p className="text-ink-500">التاريخ</p>
            <p className="font-mono">{new Date(entry.entryDate).toLocaleDateString('en-GB')}</p>
          </div>
          <div>
            <p className="text-ink-500">الحالة</p>
            <Badge variant={statusInfo.variant}>{statusInfo.label}</Badge>
          </div>
          <div>
            <p className="text-ink-500">التوازن</p>
            <Badge variant={balanced ? 'success' : 'danger'}>{balanced ? '✅ متوازن' : '❌ غير متوازن'}</Badge>
          </div>
          {entry.reference && (
            <div className="col-span-2">
              <p className="text-ink-500">المرجع</p>
              <p className="font-mono text-ink-800">{entry.reference}</p>
            </div>
          )}
          {entry.postedAt && (
            <div className="col-span-2">
              <p className="text-ink-500">تاريخ الترحيل</p>
              <p className="font-mono text-ink-800">{new Date(entry.postedAt).toLocaleString('en-GB')}</p>
            </div>
          )}
          <div className="col-span-4">
            <p className="text-ink-500">الوصف</p>
            <p className="font-bold text-ink-800">{entry.description}</p>
          </div>
        </div>
      </Card>

      {/* Lines table */}
      <Card className="mt-4 max-w-4xl">
        <h3 className="font-bold text-ink-800 mb-3">البنود ({entry.lines.length})</h3>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-start text-ink-600">
                <th className="py-2 px-2 text-start">#</th>
                <th className="py-2 px-2 text-start">الحساب</th>
                <th className="py-2 px-2 text-start">الوصف</th>
                <th className="py-2 px-2 text-end">مدين</th>
                <th className="py-2 px-2 text-end">دائن</th>
              </tr>
            </thead>
            <tbody>
              {entry.lines.map((l) => (
                <tr key={l.lineNumber} className="border-b">
                  <td className="py-2 px-2 font-mono text-ink-500">{l.lineNumber}</td>
                  <td className="py-2 px-2">
                    <span className="font-mono text-brand-600">{l.accountCode}</span>
                    <span className="text-ink-800 ms-2">{l.accountName}</span>
                  </td>
                  <td className="py-2 px-2 text-ink-600">{l.description || '-'}</td>
                  <td className="py-2 px-2 text-end font-mono">{formatNumber(l.debit)}</td>
                  <td className="py-2 px-2 text-end font-mono">{formatNumber(l.credit)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="font-bold border-t-2">
                <td colSpan={3} className="py-2 px-2 text-end">الإجمالي:</td>
                <td className="py-2 px-2 text-end font-mono text-brand-600">{formatNumber(entry.totalDebit)}</td>
                <td className="py-2 px-2 text-end font-mono text-warning-600">{formatNumber(entry.totalCredit)}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </Card>

      {entry.status === 1 && (
        <p className="max-w-4xl mt-3 text-xs text-ink-500">
          💡 القيد في حالة مسودة. اضغط زر ترحيل القيد لنشره في دفتر الأستاذ. لا يمكن تعديل بنوده بعد الترحيل.
        </p>
      )}
      {entry.status === 2 && (
        <p className="max-w-4xl mt-3 text-xs text-ink-500">
          💡 القيد مُرحَّل. التغييرات تتطلب قيد عكسي.
        </p>
      )}

      {/* Sprint 39 (DEC-125) ConfirmDialog replaces native confirm() */}
      <ConfirmDialog
        open={confirmPost}
        title="تأكيد ترحيل القيد"
        message="سيتم ترحيل القيد ولن تتمكن من تعديل بنوده بعد ذلك. هل تريد المتابعة؟"
        confirmLabel="ترحيل"
        cancelLabel="إلغاء"
        variant="primary"
        loading={posting}
        onConfirm={handlePost}
        onCancel={() => setConfirmPost(false)}
      />
    </div>
  );
}
