'use client';

import { useEffect, useCallback, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, ArrowLeft, FileText, RefreshCw } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { api, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function InventoryItemsIdPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const id = params.id;

  const [item, setItem] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);


  const load = useCallback(async () => {
    setLoading(true); setError(null);
    try {
      const url = "/api/inventory/items/{id}".replace('{id}', encodeURIComponent(id || ''));
      const r = await api.get(url);
      setItem(r.data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل البيانات.'));
    } finally { setLoading(false); }  }, [id]);

  useEffect(() => { load(); }, [id, load]);

  if (loading) {
    return (
      <div className="text-center py-12 text-gray-500">
        <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
        <p className="mt-3 text-sm">جاري التحميل...</p>
      </div>
    );
  }

  return (
    <div>
      <div className="mb-3">
        <button
          onClick={() => router.back()}
          className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-blue-600 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" />
          العودة للقائمة
        </button>
      </div>

      <PageHeader
        title="بطاقة صنف"
        description="بيانات الصنف + المخزون + الحركات"
        actions={
          <Link href="/inventory/items">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>العودة إلى الأصناف</Button>
          </Link>
        }
      />

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {!item ? (
        <Card className="p-12 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          لم يتم العثور على السجل.
        </Card>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <Card className="p-6">
            <h3 className="text-lg font-bold text-gray-800 mb-4">المعلومات الأساسية</h3>
            <dl className="space-y-3">
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">sku</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.sku ?? item['skuName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">barcode</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.barcode ?? item['barcodeName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">name</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.name ?? item['nameName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">nameEn</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.nameEn ?? item['nameEnName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">categoryName</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.categoryName ?? item['categoryNameName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">unitOfMeasure</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.unitOfMeasure ?? item['unitOfMeasureName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">costPrice</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.costPrice ?? item['costPriceName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">sellingPrice</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.sellingPrice ?? item['sellingPriceName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">reorderLevel</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.reorderLevel ?? item['reorderLevelName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">isActive</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.isActive ?? item['isActiveName'] ?? '—')}
                </dd>
              </div>
            </dl>
          </Card>

          <Card className="p-6">
            <h3 className="text-lg font-bold text-gray-800 mb-4">الإجراءات</h3>
            <div className="space-y-2">
              <Button variant="primary" onClick={load} iconLeft={<RefreshCw className="h-4 w-4" />} className="w-full">
                إعادة تحميل
              </Button>
              <Link href="/inventory/items">
                <Button variant="secondary" className="w-full">العودة للقائمة</Button>
              </Link>
            </div>
          </Card>

          <Card className="p-4 lg:col-span-2">
            <h3 className="text-sm font-semibold text-gray-700 mb-2">البيانات الخام (JSON)</h3>
            <pre className="text-xs bg-gray-50 p-3 rounded overflow-auto max-h-96" dir="ltr">
              {JSON.stringify(item, null, 2)}
            </pre>
          </Card>
        </div>
      )}
    </div>
  );
}
