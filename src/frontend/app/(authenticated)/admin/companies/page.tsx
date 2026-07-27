'use client';

import { useEffect, useState } from 'react';
import { Plus, Pencil, Building2, Hash, MapPin } from 'lucide-react';
import { Card, Badge, PageHeader, Button, EmptyState, SkeletonTable } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { api, getErrorMessage } from '@/lib/api';
import { formatDate } from '@/lib/utils';

interface Company {
  id: string;
  code: string;
  name: string;
  legalName?: string;
  taxNumber?: string;
  currency: string;
  country?: string;
  isHolding: boolean;
  isActive: boolean;
  createdAt: string;
}

export default function CompaniesAdminPage() {
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<Company[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<'all' | 'active' | 'inactive' | 'holding'>('all');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const includeInactive = filter === 'all' || filter === 'inactive';
      const r = await api.get<Company[]>('/api/companies', { params: { includeInactive } });
      setItems(r.data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [filter, authLoading]);

  const filtered = items.filter((c) => {
    if (filter === 'active') return c.isActive;
    if (filter === 'inactive') return !c.isActive;
    if (filter === 'holding') return c.isHolding;
    return true;
  });

  const activeCount = items.filter((c) => c.isActive).length;
  const holdingCount = items.filter((c) => c.isHolding).length;

  return (
    <div>
      <PageHeader
        title="🏢 إدارة الشركات"
        description="Companies Admin — عرض وإنشاء وتعديل وتعطيل الشركات"
        actions={
          <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
            شركة جديدة
          </Button>
        }
      />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
        <div className="bg-white rounded-xl shadow-sm p-4">
          <div className="text-sm text-gray-500">إجمالي</div>
          <div className="text-2xl font-bold text-blue-600 mt-1">{items.length}</div>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4">
          <div className="text-sm text-gray-500">فعّالة</div>
          <div className="text-2xl font-bold text-green-600 mt-1">{activeCount}</div>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4">
          <div className="text-sm text-gray-500">قابضة</div>
          <div className="text-2xl font-bold text-purple-600 mt-1">{holdingCount}</div>
        </div>
        <div className="bg-white rounded-xl shadow-sm p-4">
          <div className="text-sm text-gray-500">معطّلة</div>
          <div className="text-2xl font-bold text-gray-400 mt-1">{items.length - activeCount}</div>
        </div>
      </div>

      {/* Filter Tabs */}
      <div className="bg-white rounded-xl shadow-sm p-2 mb-4 flex gap-1">
        {(['all', 'active', 'inactive', 'holding'] as const).map((f) => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={`px-4 py-2 rounded-lg text-sm font-medium ${
              filter === f
                ? 'bg-blue-500 text-white'
                : 'text-gray-600 hover:bg-gray-100'
            }`}
          >
            {f === 'all' && 'الكل'}
            {f === 'active' && 'فعّالة'}
            {f === 'inactive' && 'معطّلة'}
            {f === 'holding' && 'قابضة'}
          </button>
        ))}
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {loading ? (
        <SkeletonTable rows={5} cols={3} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<Building2 className="h-12 w-12" />}
          title="لا توجد شركات"
          description={items.length === 0 ? 'لم يتم تسجيل أي شركة بعد.' : 'لا توجد شركات تطابق الفلتر الحالي.'}
          action={
            items.length === 0 ? (
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                شركة جديدة
              </Button>
            ) : (
              <Button variant="secondary" onClick={() => setFilter('all')}>
                عرض كل الشركات
              </Button>
            )
          }
        />
      ) : (
        <div className="space-y-3">
          {filtered.map((c) => (
            <Card key={c.id} accent={c.isHolding ? 'purple' : 'blue'}>
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <Building2 className="h-4 w-4 text-gray-500" />
                    <span className="font-mono text-xs text-gray-500">{c.code}</span>
                    {c.isHolding && <Badge variant="info">قابضة</Badge>}
                    <Badge variant={c.isActive ? 'success' : 'neutral'}>
                      {c.isActive ? 'فعّال' : 'معطّل'}
                    </Badge>
                  </div>
                  <h3 className="font-bold text-gray-800 text-lg">{c.name}</h3>
                  {c.legalName && c.legalName !== c.name && (
                    <p className="text-sm text-gray-600 mt-1">{c.legalName}</p>
                  )}
                  <div className="mt-3 flex flex-wrap gap-3 text-sm text-gray-500">
                    <span className="flex items-center gap-1">
                      <Hash className="h-3 w-3" />
                      {c.currency}
                    </span>
                    {c.country && (
                      <span className="flex items-center gap-1">
                        <MapPin className="h-3 w-3" />
                        {c.country}
                      </span>
                    )}
                    {c.taxNumber && <span>ضريبة: {c.taxNumber}</span>}
                    <span>تاريخ الإنشاء: {formatDate(c.createdAt)}</span>
                  </div>
                </div>
                <div className="flex gap-1">
                  <Button variant="ghost" size="sm" iconLeft={<Pencil className="h-3 w-3" />}>
                    تعديل
                  </Button>
                  {c.isActive ? (
                    <Button variant="ghost" size="sm">
                      تعطيل
                    </Button>
                  ) : (
                    <Button variant="ghost" size="sm">
                      تفعيل
                    </Button>
                  )}
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
