'use client';

// عرض تفاصيل حساب (Account)
// ملاحظة: Backend لا يدعم PUT لتعديل الحسابات،
// هذه الصفحة تعرض التفاصيل فقط مع خيار إلغاء التفعيل

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Trash2 } from 'lucide-react';
import { Button, Card, PageHeader, Badge } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { getErrorMessage } from '@/lib/api';

interface Account {
  id: string;
  tenantId: string;
  code: string;
  name: string;
  description?: string;
  type: number;
  normalBalance: number;
  parentAccountId?: string;
  isPostable: boolean;
  isActive: boolean;
}

const ACCOUNT_TYPES: Record<number, string> = {
  1: 'أصول',
  2: 'خصوم',
  3: 'حقوق ملكية',
  4: 'إيرادات',
  5: 'مصروفات',
};

export default function EditAccountPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  useAuth();
  const [item, setItem] = useState<Account | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionRunning, setActionRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const res = await fetch(`/api/finance/accounts/${params.id}`);
        if (!res.ok) throw new Error('فشل التحميل');
        const data = await res.json();
        setItem(data);
      } catch (e: unknown) {
        setError(getErrorMessage(e, 'فشل التحميل'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [params.id]);

  const handleDeactivate = async () => {
    if (!item) return;
    if (!confirm('هل أنت متأكد من إلغاء تفعيل هذا الحساب؟')) return;
    setActionRunning(true);
    try {
      const res = await fetch(`/api/finance/accounts/${item.id}`, { method: 'DELETE' });
      if (!res.ok && res.status !== 204) {
        const t = await res.text();
        throw new Error(t || 'فشل الإلغاء');
      }
      router.push('/finance/accounts');
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل إلغاء التفعيل.'));
      setActionRunning(false);
    }
  };

  if (loading) {
    return (
      <div>
        <PageHeader title="حساب" />
        <Card className="max-w-2xl"><div className="text-center py-12 text-gray-500">جاري التحميل...</div></Card>
      </div>
    );
  }

  if (!item) {
    return (
      <div>
        <PageHeader title="حساب" />
        <Card className="max-w-2xl">
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">{error || 'الحساب غير موجود'}</div>
          <div className="mt-4"><Link href="/finance/accounts"><Button variant="ghost">رجوع</Button></Link></div>
        </Card>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title="💰 حساب"
        description={`${item.code} - ${item.name}`}
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'دليل الحسابات', href: '/finance/accounts' },
          { label: item.code },
        ]}
        actions={
          <Link href="/finance/accounts">
            <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>رجوع</Button>
          </Link>
        }
      />

      <Card className="max-w-2xl">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">{error}</div>
        )}

        <div className="grid grid-cols-2 gap-4 text-sm">
          <div>
            <p className="text-gray-500">الكود</p>
            <p className="font-mono text-blue-600 text-lg">{item.code}</p>
          </div>
          <div>
            <p className="text-gray-500">النوع</p>
            <Badge variant="info">{ACCOUNT_TYPES[item.type] || item.type}</Badge>
          </div>
          <div className="col-span-2">
            <p className="text-gray-500">الاسم</p>
            <p className="font-bold text-gray-800">{item.name}</p>
          </div>
          {item.description && (
            <div className="col-span-2">
              <p className="text-gray-500">الوصف</p>
              <p className="text-gray-800">{item.description}</p>
            </div>
          )}
          <div>
            <p className="text-gray-500">الرصيد الطبيعي</p>
            <Badge variant="neutral">{item.normalBalance === 1 ? 'مدين' : 'دائن'}</Badge>
          </div>
          <div>
            <p className="text-gray-500">قابل للترحيل</p>
            <Badge variant={item.isPostable ? 'success' : 'neutral'}>
              {item.isPostable ? 'نعم' : 'لا (حساب رئيسي)'}
            </Badge>
          </div>
          <div>
            <p className="text-gray-500">الحالة</p>
            <Badge variant={item.isActive ? 'success' : 'neutral'}>
              {item.isActive ? 'فعّال' : 'غير فعّال'}
            </Badge>
          </div>
        </div>

        <div className="mt-6 pt-4 border-t">
          <p className="text-xs text-gray-500 mb-3">
            💡 ملاحظة: لا يدعم النظام تعديل الحسابات بعد الإنشاء (لأسباب محاسبية).
            يمكنك إلغاء التفعيل وإنشاء حساب جديد بدلاً من ذلك.
          </p>
          {item.isActive && (
            <Button
              variant="danger"
              onClick={handleDeactivate}
              loading={actionRunning}
              iconLeft={<Trash2 className="h-4 w-4" />}
            >
              إلغاء التفعيل
            </Button>
          )}
        </div>
      </Card>
    </div>
  );
}
