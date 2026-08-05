'use client';

// قائمة القيود المحاسبية (Journal Entries) — Sprint 39 (DEC-125) Design System + L60 API fix

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Plus, Eye, FileText } from 'lucide-react';
import {
  Card,
  Badge,
  PageHeader,
  Button,
  Input,
  EmptyState,
  SkeletonTable,
  useToast,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { financeApi, getErrorMessage, JournalEntry } from '@/lib/api';
import { formatNumber } from '@/lib/format';

const JE_STATUSES: Record<number, { label: string; variant: 'neutral' | 'success' | 'warning' }> = {
  1: { label: 'مسودة', variant: 'warning' },
  2: { label: 'مُرحَّل', variant: 'success' },
  3: { label: 'معكوس', variant: 'neutral' },
};

export default function JournalEntriesPage() {
  const { loading: authLoading } = useAuth();
  const toast = useToast();
  const [items, setItems] = useState<JournalEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  // Sprint 39 (DEC-125) L60 fix: use financeApi (auto-attaches JWT) instead of raw fetch
  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await financeApi.listJournalEntries();
      setItems(data);
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'فشل التحميل');
      setError(msg);
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  const filtered = items.filter(
    (j) => !filter || j.entryNumber?.includes(filter) || j.description?.includes(filter)
  );

  const totalDr = items.reduce((s, j) => s + Number(j.totalDebit || 0), 0);
  const totalCr = items.reduce((s, j) => s + Number(j.totalCredit || 0), 0);
  const balanced = Math.abs(totalDr - totalCr) < 0.01;

  return (
    <div>
      <PageHeader
        title="📒 القيود المحاسبية"
        description="القيود اليومية ودفتر الأستاذ"
        actions={
          <div className="flex items-center gap-2">
            <Input
              placeholder="🔍 رقم/وصف..."
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              containerClassName="w-64"
            />
            <Link href="/finance/journal-entries/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>قيد جديد</Button>
            </Link>
          </div>
        }
      />

      {/* Summary card */}
      {!loading && items.length > 0 && (
        <Card className="mb-4" accent={balanced ? 'green' : 'red'}>
          <div className="grid grid-cols-3 gap-4 text-sm">
            <div>
              <p className="text-ink-500">إجمالي المدين</p>
              <p className="font-bold text-xl text-ink-800 tabular-nums">{formatNumber(totalDr)}</p>
            </div>
            <div>
              <p className="text-ink-500">إجمالي الدائن</p>
              <p className="font-bold text-xl text-ink-800 tabular-nums">{formatNumber(totalCr)}</p>
            </div>
            <div>
              <p className="text-ink-500">التوازن</p>
              <Badge variant={balanced ? 'success' : 'danger'}>
                {balanced ? '✅ متوازن' : '❌ غير متوازن'}
              </Badge>
            </div>
          </div>
        </Card>
      )}

      {loading ? (
        <SkeletonTable rows={6} cols={4} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<FileText className="h-12 w-12" />}
          title="لا توجد قيود محاسبية"
          description="ابدأ بإنشاء قيد يومية جديد لتسجيل المعاملات المالية."
          action={
            <Link href="/finance/journal-entries/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>قيد جديد</Button>
            </Link>
          }
        />
      ) : (
        <div className="space-y-3">
          {filtered.map((j) => {
            const statusInfo = JE_STATUSES[j.status] || { label: `Status ${j.status}`, variant: 'neutral' as const };
            const jeBalanced = Math.abs(Number(j.totalDebit) - Number(j.totalCredit)) < 0.01;
            return (
              <Card key={j.id} accent="purple" interactive>
                <div className="flex items-start justify-between">
                  <div className="min-w-0 flex-1">
                    <p className="text-xs text-ink-500 font-mono">{j.entryNumber}</p>
                    <h3 className="font-bold text-ink-800 mt-1">{j.description}</h3>
                    <div className="flex items-center gap-3 mt-2 text-xs text-ink-500">
                      <span>📅 {new Date(j.entryDate).toLocaleDateString('en-GB')}</span>
                      {j.reference && <span>🔖 {j.reference}</span>}
                      <span className={jeBalanced ? 'text-success-600' : 'text-danger-600'}>
                        د. {formatNumber(j.totalDebit)} / ح. {formatNumber(j.totalCredit)}
                      </span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 flex-shrink-0">
                    <Link href={`/finance/journal-entries/${j.id}`}>
                      <Button variant="ghost" size="sm" iconLeft={<Eye className="h-3 w-3" />}>
                        عرض
                      </Button>
                    </Link>
                    <Badge variant={statusInfo.variant}>{statusInfo.label}</Badge>
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
