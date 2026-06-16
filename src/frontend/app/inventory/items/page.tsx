'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { authApi, inventoryApi, Item } from '@/lib/api';

export default function ItemsPage() {
  const router = useRouter();
  const [items, setItems] = useState<Item[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('');

  useEffect(() => {
    if (!authApi.isLoggedIn()) { router.push('/login'); return; }
    load();
  }, [router]);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const data = await inventoryApi.listItems();
      setItems(data);
    } catch (e: any) {
      setError(e?.response?.data?.detail || 'فشل التحميل');
    } finally { setLoading(false); }
  };

  const filtered = items.filter(i =>
    !filter || i.sku.includes(filter) || i.name.includes(filter)
  );

  return (
    <div className="min-h-screen" dir="rtl">
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Link href="/dashboard" className="text-gray-500 hover:text-gray-700">← رجوع</Link>
            <h1 className="text-2xl font-bold text-gray-800">📦 المنتجات (Items)</h1>
          </div>
          <input
            type="text"
            placeholder="🔍 SKU / اسم..."
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            className="border rounded-lg px-3 py-1.5 text-sm w-48"
          />
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-6 py-6">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
            {error}
          </div>
        )}

        {loading ? (
          <div className="text-center py-12 text-gray-500">جاري التحميل...</div>
        ) : filtered.length === 0 ? (
          <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
            لا توجد منتجات في هذا الـ tenant.
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {filtered.map((item) => (
              <div key={item.id} className="bg-white rounded-xl shadow-sm p-4 border-r-4 border-green-500">
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
              </div>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
