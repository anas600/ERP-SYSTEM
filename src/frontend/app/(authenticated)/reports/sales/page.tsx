'use client';

import { useState } from 'react';
import { FileText, ArrowRight } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Button, Card } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';

export default function SalesReportsPage() {
  const { loading: authLoading } = useAuth();
  const [reportType, setReportType] = useState<'coming' | 'coming' | 'coming'>('coming');

  if (authLoading) {
    return <div className="text-center py-12 text-gray-500">جاري التحميل...</div>;
  }

  return (
    <div>
      <PageHeader
        title="💰 تقارير المبيعات"
        description="Sales Reports — قريباً"
        actions={
          <Link href="/reports">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
              العودة للتقارير
            </Button>
          </Link>
        }
      />

      <Card>
        <div className="text-center py-8">
          <FileText className="h-12 w-12 text-gray-400 mx-auto mb-3" />
          <h3 className="text-lg font-semibold text-gray-700 mb-2">قريباً</h3>
          <p className="text-sm text-gray-500 max-w-md mx-auto">
            تقارير المبيعات قيد التطوير. حالياً يمكنك استخدام <Link href="/finance/sales-invoices" className="text-blue-600 hover:underline">قائمة الفواتير</Link> للحصول على البيانات الأولية.
          </p>
          <p className="text-xs text-gray-400 mt-3">
            Backend endpoints: <code className="bg-gray-100 px-1 rounded">/api/finance/aging-ar</code>, <code className="bg-gray-100 px-1 rounded">/api/ar/customers</code>
          </p>
        </div>
      </Card>
    </div>
  );
}
