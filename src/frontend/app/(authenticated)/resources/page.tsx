'use client';

// صفحة الموارد (Resources) — Sprint 32 (DEC-112)
// الموارد هي العمالة / المعدات / المواد / الخدمات التي تُعيَّن على مهام المشاريع

import { useEffect, useState } from 'react';
import { Plus, Hammer, Loader2 } from 'lucide-react';
import { Card, Badge, PageHeader, Button, EmptyState, SkeletonTable } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { resourcesApi, RESOURCE_TYPES, Resource } from '@/lib/api';

const TYPE_COLORS: Record<number, string> = {
  1: 'bg-blue-100 text-blue-700',     // Labor
  2: 'bg-orange-100 text-orange-700', // Equipment
  3: 'bg-green-100 text-green-700',   // Material
  4: 'bg-purple-100 text-purple-700', // Service
};

export default function ResourcesPage() {
  const { loading: authLoading } = useAuth();
  const [resources, setResources] = useState<Resource[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState({
    code: '',
    name: '',
    type: 1,
    hourlyRate: 50,
  });

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await resourcesApi.listResources();
      setResources(data);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { detail?: string } } };
      setError(err?.response?.data?.detail || 'فشل التحميل');
    } finally {
      setLoading(false);
    }
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreating(true);
    setError(null);
    try {
      await resourcesApi.createResource({
        code: form.code,
        name: form.name,
        type: Number(form.type),
        hourlyRate: Number(form.hourlyRate),
        isActive: true,
      });
      setForm({ code: '', name: '', type: 1, hourlyRate: 50 });
      await load();
    } catch (e: unknown) {
      const err = e as { response?: { data?: { detail?: string } } };
      setError(err?.response?.data?.detail || 'فشل الإنشاء');
    } finally {
      setCreating(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="🔧 الموارد"
        description="العمالة / المعدات / المواد / الخدمات المُستخدمة في المشاريع"
      />

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      <Card className="mb-6">
        <h3 className="font-bold text-gray-800 mb-3">➕ إضافة مورد جديد</h3>
        <form onSubmit={submit} className="grid grid-cols-1 md:grid-cols-5 gap-3 items-end">
          <div>
            <label className="block text-xs text-gray-600 mb-1">الكود</label>
            <input
              required
              value={form.code}
              onChange={(e) => setForm({ ...form, code: e.target.value })}
              placeholder="RES-XXX"
              className="w-full px-3 py-2 border border-gray-300 rounded text-sm"
            />
          </div>
          <div className="md:col-span-2">
            <label className="block text-xs text-gray-600 mb-1">الاسم</label>
            <input
              required
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
              placeholder="عامل حفر / حفار / أسمنت..."
              className="w-full px-3 py-2 border border-gray-300 rounded text-sm"
            />
          </div>
          <div>
            <label className="block text-xs text-gray-600 mb-1">النوع</label>
            <select
              value={form.type}
              onChange={(e) => setForm({ ...form, type: Number(e.target.value) })}
              className="w-full px-3 py-2 border border-gray-300 rounded text-sm"
            >
              {Object.entries(RESOURCE_TYPES).map(([k, v]) => (
                <option key={k} value={k}>
                  {v}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs text-gray-600 mb-1">سعر الساعة (LYD)</label>
            <input
              required
              type="number"
              step="0.01"
              min="0"
              value={form.hourlyRate}
              onChange={(e) => setForm({ ...form, hourlyRate: Number(e.target.value) })}
              className="w-full px-3 py-2 border border-gray-300 rounded text-sm"
            />
          </div>
          <div className="md:col-span-5">
            <Button type="submit" variant="primary" disabled={creating} iconLeft={creating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}>
              {creating ? 'جاري الإنشاء...' : 'إضافة'}
            </Button>
          </div>
        </form>
      </Card>

      {loading ? (
        <SkeletonTable rows={5} cols={4} />
      ) : resources.length === 0 ? (
        <EmptyState
          icon={<Hammer className="h-12 w-12" />}
          title="لا توجد موارد"
          description="أضف العمالة / المعدات / المواد / الخدمات التي تستخدمها في مشاريعك."
        />
      ) : (
        <div className="space-y-2">
          {resources.map((r) => (
            <Card key={r.id} accent="blue">
              <div className="flex items-center justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-gray-500 font-mono">{r.code}</span>
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${TYPE_COLORS[r.type] || 'bg-gray-100 text-gray-700'}`}>
                      {RESOURCE_TYPES[r.type] || `نوع ${r.type}`}
                    </span>
                    {!r.isActive && (
                      <Badge variant="neutral">غير نشط</Badge>
                    )}
                  </div>
                  <h3 className="font-bold text-gray-800 mt-1">{r.name}</h3>
                </div>
                <div className="text-left">
                  <p className="text-xs text-gray-500">سعر الساعة</p>
                  <p className="text-lg font-bold text-orange-600">
                    {r.hourlyRate.toFixed(2)} <span className="text-xs text-gray-500">LYD</span>
                  </p>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
