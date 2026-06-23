'use client';

// صفحة المنتجات (Items) — بطاقات

import { useEffect, useState } from 'react';
import { Input, Card, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { inventoryApi, Item } from '@/lib/api';

export default function ItemsPage() {
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<Item[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await inventoryApi.listItems();
      setItems(data);
    } catch (e: unknown) {
      const err = e as { response?: { data?: { detail?: string } } };
      setError(err?.response?.data?.detail || 'فشل التحميل');
    } finally {
      setLoading(false);
    }
  };

  const filtered = items.filter(
    (i) => !filter || i.sku.includes(filter) || i.name.includes(filter)
  );

  return (
    <div>
      <PageHeader
        title="📦 المنتجات (Items)"
        description="قائمة الأصناف في المخزون"
        actions={
          <Input
            placeholder="🔍 SKU / اسم..."
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            containerClassName="w-64"
          />
        }
      />

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {loading ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </div>
      ) : filtered.length === 0 ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          لا توجد منتجات في هذا الـ tenant.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map((item) => (
            <Card key={item.id} accent="green">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-xs text-gray-500 font-mono">{item.sku}</p>
                  <h3 className="font-bold text-gray-800 mt-1">{item.name}</h3>
                </div>
                <span className="bg-green-50 text-green-700 text-xs px-2 py-0.5 rounded">
                  {item.itemType}
                </span>
              </div>
              <p className="text-xs text-gray-500 mt-2">
                {item.description || 'لا يوجد وصف'}
              </p>
              <div className="mt-3 pt-3 border-t flex items-center justify-between text-sm">
                <span className="text-gray-500">التكلفة المتوسطة:</span>
                <span className="font-bold text-gray-800">
                  {item.averageCost?.toFixed(2) || '0.00'}
                </span>
              </div>
              <div className="mt-1 flex items-center justify-between text-sm">
                <span className="text-gray-500">إعادة الطلب عند:</span>
                <span className="font-mono">{item.reorderLevel}</span>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
